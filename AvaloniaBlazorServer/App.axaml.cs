using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ServerApp;

namespace AvaloniaBlazorServer;

public class App : Application
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
                Content = new NativeWebView()
                {
                    Source = new Uri($"localhost:{ServerAppHost.Port}"),
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
            var webview = new NativeWebView();
            webview.Loaded += (self, _) =>
            {
                var blazorWebview = (NativeWebView)self!;

                // The server is started from a foreground service,
                // wait for the server to start before navigating to the URL
                ServerAppHost.OnServerStarted = hostUrl =>
                {
                    Dispatcher.UIThread.Invoke(() => blazorWebview.Source = new Uri(hostUrl));
                };
            };
            singleViewPlatform.MainView = webview;
        }

        base.OnFrameworkInitializationCompleted();
    }
}