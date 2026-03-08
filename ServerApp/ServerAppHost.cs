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
        var assemblyName = typeof(ServerAppHost).Assembly.GetName().Name;
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = assemblyName
        });

        builder.WebHost.ConfigureKestrel((_, serverOptions) =>
            serverOptions.Listen(broadcast ? IPAddress.Any : IPAddress.Loopback, port));
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

        app.StartAsync(serverTokenToken);

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

        return app.WaitForShutdownAsync();
    }
}