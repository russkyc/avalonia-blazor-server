using System;
using System.Runtime.Versioning;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Avalonia;
using Avalonia.Android;
using Avalonia.WebView.Android;

namespace AvaloniaBlazorServer.Android;

[Activity(
    Label = "AvaloniaBlazorServer.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont()
            .UseAndroidWebView();
    }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        new Handler(Looper.MainLooper!).Post(StartHostService);
    }

    [SupportedOSPlatform("android33.0")]
    void CheckAndRequestNotificationPermission()
    {
        if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.PostNotifications) != (int)Permission.Granted)
        {
            ActivityCompat.RequestPermissions(this, [Manifest.Permission.PostNotifications], 0);
        }
    }

    private void StartHostService()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            CheckAndRequestNotificationPermission();
        }
        
        var intent = new Intent(this, typeof(HostService));
        
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            StartForegroundService(intent);
        }
        else
        {
            StartService(intent);
        }
    }
}