using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using AvaloniaWebView;
using Russkyc.Messaging;
using ServerApp;
using ServerApp.Messages;

namespace AvaloniaBlazorServer;

public partial class App : Application
{
    private readonly CancellationTokenSource _serverTokenSource = new();
    private Task? _serverTask;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _serverTask = ServerAppHost.Start(_serverTokenSource.Token);
            desktop.MainWindow = new Window()
            {
                Content = new WebView()
                {
                    Url = new Uri($"localhost:{ServerAppHost.Port}"),
                }
            };
            desktop.ShutdownRequested += (_, _) =>
            {
                _serverTokenSource.CancelAsync().ContinueWith(_ =>
                {
                    _serverTask.Wait();
                    _serverTask = null;
                });
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            var webview = new WebView();
            webview.Loaded += (self, _) =>
            {
                var blazorWebview = (WebView)self!;

                // The server is started from a foreground service,
                // wait for the server to start before navigating to the URL
                WeakReferenceMessenger.Default.Register<ServerStartedEvent>(blazorWebview, (_, message) =>
                {
                    Console.WriteLine(message.HostUrl);
                    Dispatcher.UIThread.Invoke(() => blazorWebview.Url = new Uri(message.HostUrl));
                });
            };
            singleViewPlatform.MainView = webview;
        }

        base.OnFrameworkInitializationCompleted();
    }
}