using System.Threading;
using System.Threading.Tasks;
using _Microsoft.Android.Resource.Designer;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using ServerApp;

namespace AvaloniaBlazorServer.Android;

[Service(Enabled = true, Exported = false, ForegroundServiceType = ForegroundService.TypeSpecialUse)]
public class HostService : Service
{
    // Configuration
    private const int NotificationId = 1;
    private const string NotificationChannelId = "com.russkyc.avaloniablazorserver";
    private const string NotificationChannelName = "Blazor Web Server Channel";
    private const string NotificationTitle = "Blazor Web Server";
    private const string NotificationText = "Blazor web server is running in the background";
    
    private CancellationTokenSource _tokenSource = new();
    private PowerManager.WakeLock? _wakeLock;
    private Task? _serverTask;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        if (_serverTask is { IsCompleted: false })
        {
            _tokenSource.Cancel();
            _serverTask.Wait();
            if (_wakeLock is { IsHeld: true })
            {
                _wakeLock.Release();
                _wakeLock = null;
            }
        }

        _tokenSource.Dispose();
        _tokenSource = new CancellationTokenSource();
        
        StartForegroundService();
        AcquireWakeLock();
        _serverTask = ServerAppHost.Start(_tokenSource.Token);
        return StartCommandResult.Sticky;
    }

    public override void OnDestroy()
    {
        _tokenSource.Cancel();
        _serverTask?.Wait();
        if (_wakeLock is { IsHeld: true })
        {
            _wakeLock.Release();
            _wakeLock = null;
        }
        base.OnDestroy();
    }

    public override IBinder? OnBind(Intent? intent)
    {
        return null;
    }
    
    public override void OnTaskRemoved(Intent? rootIntent)
    {
        // Stop the service and clean up when app is removed from recents
        _tokenSource.Cancel();
        _serverTask?.Wait();
        if (_wakeLock is { IsHeld: true })
        {
            _wakeLock.Release();
            _wakeLock = null;
        }

        StopSelf();
        base.OnTaskRemoved(rootIntent);
    }

    private void StartForegroundService()
    {
        if (GetSystemService(NotificationService) is not NotificationManager notificationManager)
        {
            return;
        }

        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            CreateNotificationChannel(notificationManager);
        }

        var builder = new NotificationCompat.Builder(this, NotificationChannelId);

        builder.SetCategory(NotificationCompat.CategoryService);
        builder.SetPriority(NotificationCompat.PriorityDefault);
        builder.SetSmallIcon(ResourceConstant.Drawable.Icon);
        builder.SetContentTitle(NotificationTitle);
        builder.SetContentText(NotificationText);
        builder.SetOngoing(true);

        var notification = builder.Build();
        
        StartForeground(NotificationId, notification);
    }

    private void CreateNotificationChannel(NotificationManager notificationMnaManager)
    {
        var channel = new NotificationChannel(NotificationChannelId, NotificationChannelName, NotificationImportance.Default);
        notificationMnaManager.CreateNotificationChannel(channel);
    }

    private void AcquireWakeLock()
    {
        if (_wakeLock is { IsHeld: true })
        {
            return;
        }
        var powerManager = (PowerManager)GetSystemService(PowerService)!;
        _wakeLock = powerManager.NewWakeLock(WakeLockFlags.Partial, "BlazorWebServer:WakeLock");
        _wakeLock!.Acquire();
    }

}