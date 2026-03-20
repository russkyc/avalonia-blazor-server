using System;
using Android.App;
using Android.Runtime;
using Avalonia.Android;

namespace AvaloniaBlazorServer.Android;

[Application]
public class AndroidApp : AvaloniaAndroidApplication<App>
{
    protected AndroidApp(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
    {
    }
}