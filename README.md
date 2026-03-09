## Avalonia Embedded Blazor Web App Host (Interactive Server)
This is a sample of a Blazor Web App Server running from an android device,
without the need of a PC, and accessible from the ui(webview), in browser, or other devices on the same network.
Using wifi or the device hotspot, you can have other devices connect to the server.

<img src="screenshot.png">

### Rationale
We can't run a full ASP.NET hosted app on net-android (see), atleast officially. It is not planned as noted on these issues:

- [Github Issue: Run ASP.Net Core on Android+iOs Fully as if its a normal PC](https://github.com/dotnet/aspnetcore/issues/3204)
- [Microsoft.Net.Sdk.Web on Android](https://github.com/dotnet/sdk/issues/29567)

What if we really want to? Technically we can, and this project is the proof of concept.
A cross-platform app capable of hosting a complete Blazor Web App, Accessible from the ui, in browser, or other devices on the same network.

#### Example use cases (mobile-first, no PC required):

- **Gaming LAN host (phone hotspot mode)**  
  One Android phone runs the app server and creates a hotspot. Nearby players connect from their own devices for match lobbies, scoreboards, tournament brackets, or shared game tools.

- **Pop-up office in low-connectivity areas**  
  A tablet hosts internal forms, task boards, and status pages for a small team during travel or temporary setups, with everyone connecting over local Wi-Fi/hotspot instead of cloud services.

- **Field operations and inspections**  
  Teams in construction, logistics, utilities, or events run workflows directly on an Android device in the field. Data entry, checklists, and logs continue in a truly portable self-hosted manner.

- **Portable team hub / command center**  
  A single mobile device acts as a local hub for schedules, alerts, check-ins, and live updates that other nearby devices can open in a browser on the same local network.

- **Classroom/training/demo environments**  
  In workshops or classrooms, one phone/tablet hosts the experience and participants join from their own devices without needing lab PCs or preconfigured infrastructure.

- **Kiosk + supervisor access**  
  Keep the UI visible on a mounted Android kiosk while managers connect from another phone/tablet on the same network for monitoring and control.

- **Emergency fallback mode**  
  When WAN/cloud access fails, the mobile-hosted server provides a continuity layer for critical local operations until normal connectivity returns.

#### Should we be doing this?
Probably not mainly because android and mobile devices are not meant to be always-running server devices. But for these examples, we could arugue that it could be acceptable. So we are doing it anyway.

### Overview

- `Microsoft.NET.Sdk.Web` cannot be used on android, so after creating a blazor web app, we set it to use `Microsoft.NET.Sdk.Razor`.
- The `_framework/blazor.web.js` cannot be produced, so we take a copy of it from a running web app or produced assets of a normal web app.
- The wwwroot content is embedded to not require copying the wwwroot folder to the android project.

### How this works and the changes made

Multiple workarounds are used in order for this to run on android. Let's focus on the actual changes to the projects and the code:

#### I. The Blazor Web App (ServerApp.csproj)
The blazor web app is a normal blazor web app, but the template used was from [Blazor Minimum Project Templates](https://github.com/jsakamoto/BlazorMinimumTemplates). You can use the normal blazor web app template, but you will need to make the same changes as below.

Note that this is for interactive server mode, if you are using wasm or blazor webassembly the names might be different.

##### Steps:
Run the web app initially and get a copy of the `blazor.web.js` file from the running app `http://<host>:<port>/_framework/blazor.web.js`. This is required because the build process cannot produce it when using `Microsoft.NET.Sdk.Razor`. Stop the app after copying the file. Save this to the `wwwroot/framework/` folder of the web app project update the path in the root compnent:
```razorhtmldialect
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8"/>
    <meta name="viewport" content="width=device-width, initial-scale=1.0"/>
    <title>ServerApp</title>
    <base href="/"/>
    <ResourcePreloader/>
    <ImportMap/>
    <link rel="stylesheet" href="@Assets["css/blazor-ui.css"]"/>
    <HeadOutlet @rendermode="@InteractiveServer"/>
</head>
<body>
<Routes @rendermode="@InteractiveServer"/>
@*Use the extracted blazor.web.js*@
<script src="@Assets["framework/blazor.web.js"]"></script>
</body>
</html>
```
Replace `Microsoft.NET.Sdk.Web` with `Microsoft.NET.Sdk.Razor`. And set the project to be a class library instead of an executable and update references in the project file.

```xml
<!-- Change to Sdk.Razor from Sdk.Web -->
<Project Sdk="Microsoft.NET.Sdk.Razor">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <!-- Set as class library insteaf of executable -->
    <OutputType>Library</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <!-- Include references to the AspNetCore dlls of the current framework, your exact version folder may vary -->
    <Reference
            Include="C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.1\*.dll"
            Exclude="C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.1\aspnetcorev2_inprocess.dll">
      <Private>False</Private>
      <CopyToOutputDirectory>Never</CopyToOutputDirectory>
    </Reference>
  </ItemGroup>
  <ItemGroup>
    <!-- Configure to embed the wwwroot folder -->
    <Content Remove="wwwroot\**" />
    <EmbeddedResource Include="wwwroot\**\*" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.FileProviders.Embedded" />
  </ItemGroup>
  <ItemGroup>
    <_ContentIncludedByDefault Remove="Properties\launchSettings.json" />
  </ItemGroup>
</Project>
```
- Reference the aspnetcore dll's
- Embed the wwwroot content as embedded resources
- Add the `Microsoft.Extensions.FileProviders.Embedded` package


Remove `Program.cs` and move the startup code to a separate class with a `Start` method that can be called from the avalonia app. Note that it doesn't need to be static
this is just for simplicity.

ServerAppHost.cs
```csharp
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using ServerApp.Components;

namespace ServerApp;

public static class ServerAppHost
{
    public static ICollection<string> Hosts { get; } = new List<string>();

    public static Task Start(CancellationToken serverTokenToken = default, int port = 5000, bool broadcast = true)
    {
        // Configure to properly resolve the static assets from the embedded resources
        var assemblyName = typeof(ServerAppHost).Assembly.GetName().Name;
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = assemblyName
        });

        // set what IP addresses Kestrel should listen on. If broadcast is true, listen on all IPs, otherwise only on localhost
        builder.WebHost.ConfigureKestrel((_, serverOptions) =>
            serverOptions.Listen(broadcast ? IPAddress.Any : IPAddress.Loopback, port));
        // Enable static web assets
        builder.WebHost.UseStaticWebAssets();

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        
        // Use an embedded file provider to serve static files from the assembly's wwwroot folder
        var embeddedProvider = new EmbeddedFileProvider(
            typeof(ServerAppHost).Assembly,
            $"{assemblyName}.wwwroot"
        );

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = embeddedProvider
        });

        app.UseAntiforgery();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        var server = app.Services.GetRequiredService<IServer>();
        var addressesFeature = server.Features.Get<IServerAddressesFeature>();
        
        // We could also use app.RunAsync() here, but StartAsync()
        // allows us to do additional work (like listing the URLs)
        app.StartAsync(serverTokenToken);

        // this is just used to list the actual URLs the server is listening on,
        // so we can display them in the UI and let the user know how to connect to the server from their mobile device.
        if (addressesFeature != null)
        {
            // The Addresses collection contains all configured URLs
            foreach (var url in addressesFeature.Addresses)
            {
                var uri = new Uri(url);
                // If it's the wildcard, find the actual network IPs
                if (url.Contains("0.0.0.0") || url.Contains("[::]"))
                {
                    // Iterate through all network interfaces (Wi-Fi, Hotspot, Ethernet)
                    var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                        .Where(i => i.OperationalStatus == OperationalStatus.Up);

                    foreach (var netInterface in interfaces)
                    {
                        var properties = netInterface.GetIPProperties();
                        var ipv4 = properties.UnicastAddresses
                            .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork);

                        if (ipv4 is null) continue;

                        var host = ipv4.Address.ToString() == "127.0.0.1" ? "localhost" : ipv4.Address.ToString();
                        var portSuffix = uri.Port == 80 ? "" : $":{uri.Port}";
                        Hosts?.Add($"{uri.Scheme}://{host}{portSuffix}");
                    }
                }
                else
                {
                    Hosts?.Add(url);
                }
            }
        }

        return app.WaitForShutdownAsync(token: serverTokenToken);
    }
}
```

#### II. The Main Avalonia Project (AvaloniaBlazorServer.csproj)
Reference the server project

```xml
<ItemGroup>
    <!-- Reference the server class library project -->
    <ProjectReference Include="..\ServerApp\ServerApp.csproj" />
</ItemGroup>
```

Call the `Start` method of the server from the avalonia app, for example in `App.axaml.cs`. We use a cancellation token to be able to stop the server when the app is closed.

```csharp
_server = ServerAppHost.Start(_serverTokenSource.Token);
```

#### III. The Android and Desktop projects

We need to add the references to the aspnetcore dll's in the android project `AvaloniaBlazorServer.Android.csproj`

```xml
<ItemGroup>
    <ProjectReference Include="..\AvaloniaBlazorServer\AvaloniaBlazorServer.csproj"/>
    <!-- Include references to the AspNetCore dlls of the current framework, your exact version folder may vary -->
    <Reference Include="C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.1\*.dll"
               Exclude="C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.1\aspnetcorev2_inprocess.dll">
        <Private>False</Private>
        <CopyToOutputDirectory>Never</CopyToOutputDirectory>
    </Reference>
</ItemGroup>
```

To make it actually run and not just crash, we need to configure the android project to not use AOT.

```xml
<PropertyGroup>
    <!-- Both needed to be set to false in order for the Asp.Net dll references to work on android -->
    <AndroidEnableProfiledAot>false</AndroidEnableProfiledAot>
    <RunAOTCompilation>false</RunAOTCompilation>
</PropertyGroup>
```

In the desktop project (`AvaloniaBlazorServer.Desktop.csproj`) we could just reference `Aspnetcore.App` since it is supported in the desktop platform.

```xml
<ItemGroup>
    <ProjectReference Include="..\AvaloniaBlazorServer\AvaloniaBlazorServer.csproj"/>
    <!-- Used instead of manuall dll reference since AspNetCore dll's can actually target desktop platforms -->
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
</ItemGroup>
```

Now you have a fully working blazor web app running on android and desktop, accessible from the ui (with a webview), in browser, or other devices on the same network.

### Thoughts and Next Steps

The current setup is hacky and has some downsides:
- We have to manually copy the `blazor.web.js` file and reference it, which is not ideal. It would be better if we could produce it as part of the build process.
- Css isolation is not working (since assets are not being produced as part of the build process), so we have to use global css for styling.
- Which also means that ui libraries that rely on css isolation won't work without modification.

##### Next steps:

- Programmaticaly produce the `blazor.web.js` file as part of the build process, preferrably fix the build process to produce it and other static assets properly.
- Find a way to better handle the AspNet Core dll references, maybe by using a nuget package or something similar instead of manually referencing the dll's from the framework folder.
- Look for ios workarounds, since it should be possible to run on ios with the right configuration, but I don't have access to a mac to test and develop those workarounds.

### Special thanks

this is heavily inspired from [ASP.NET Core in Maui](https://github.com/JamesNK/aspnetcore-maui). Huge credit to JamesNK for discovering the workarounds
to run ASP.NET unofficially in unsupported platforms.