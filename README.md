## Avalonia Embedded Blazor Web App (Interactive Server)
This is a sample of a Blazor Web App fully embedded inside an avalonia cross platform app that runs on desktop and android. (ios is currently not wurking, but should be possible with more workarounds)

<img src="screenshot.png">

### Why?
To run both a ui and a web server without the need to run separate apps. And to be ale to access the web server from the ui and in browser or other devices on the same network.

#### Should we be doing this?
Technically no, but it is a fun experiment and can be useful for some specific use cases. So we are doing it anyway.
### Overview of how it works

- `Microsoft.NET.Sdk.Web` cannot be used on android, so after creating a blazor web app, we set it to use `Microsoft.NET.Sdk.Razor`.
- The `_framework/blazor.web.js` cannot be produced, so we take a copy of it from a running web app or produced assets of a normal web app.
- The wwwroot content is embedded to not require copying the wwwroot folder to the android project.

### Special thanks

this is heavily inspired from [ASP.NET Core in Maui](https://github.com/JamesNK/aspnetcore-maui). Huge credit to JamesNK for discovering the workarounds
to run ASP.NET unofficially in unsupported platforms.