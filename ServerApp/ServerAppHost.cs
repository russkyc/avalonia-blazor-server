using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using BlazorBlueprint.Components;
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
        
        builder.Services.AddBlazorBlueprintComponents();

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