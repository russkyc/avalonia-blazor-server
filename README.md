## Avalonia Embedded Blazor Web App (Interactive Server)
This is a sample of a Blazor Web App fully embedded inside an avalonia cross platform app that runs on desktop and android. (ios is currently not wurking, but should be possible with more workarounds)

<img src="screenshot.png">

### Why?
We can't run a full hosted app on net-android, atleast officially. The goal of this project is to run both a ui and a web server on a portable device.
And to be ale to access the web server from the ui and in browser or other devices on the same network.

#### Why not use Blazor Hybrid?
Blazor-Maui hybrid's goal is to be able to use blazor components, web technology, and the razor syntax in the ui of a native app instead of using xaml.

This is different, we want to host a full blazor web app. We want it to act as a server and be able to access it from a browser or other devices on the same network, without a pc or dedicated server. The UI of the app is just a webview to access the blazor web app, but the web app is fully functional and can be accessed from other devices on the same network.

### Example use cases (mobile-first, no PC required):

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
Technically no, but it is a fun experiment and can be useful for some specific use cases. So we are doing it anyway.
### Overview of how it works

- `Microsoft.NET.Sdk.Web` cannot be used on android, so after creating a blazor web app, we set it to use `Microsoft.NET.Sdk.Razor`.
- The `_framework/blazor.web.js` cannot be produced, so we take a copy of it from a running web app or produced assets of a normal web app.
- The wwwroot content is embedded to not require copying the wwwroot folder to the android project.

### Special thanks

this is heavily inspired from [ASP.NET Core in Maui](https://github.com/JamesNK/aspnetcore-maui). Huge credit to JamesNK for discovering the workarounds
to run ASP.NET unofficially in unsupported platforms.