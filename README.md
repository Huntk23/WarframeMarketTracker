# Warframe Market Tracker

---

A lightweight desktop app that watches personally selected [warframe.market](https://warframe.market) item prices in the background and pings you when a deal hits your target — so you don't have to constantly refresh the sellers tab or wait on a seller to click on the buyer tab; because we all know buying a deal is better than waiting for a deal.

## Preview

![App Preview](Documentation/AppPreview.png)

![Windows 11 Notification Example](Documentation/ExampleWin11Notification.png)

## What it does

- **Track items by name** — search from the full [warframe.market](https://warframe.market) item catalog with autocomplete
  - **Persistence** — the app will save and restore the items you are tracking automatically during app session and on close
- **Set your price** — pick a platinum threshold per item, optionally filter by mod rank
- **Get notified** — cross-platform native notification when an in-game seller lists at or below your target
  - **Sales link** — open the tracked items sale page
  - **Copy the whisper** — one click on the notification copies the `/w` trade message to your clipboard, ready to paste in-game, all while using the same [warframe.market](https://warframe.market) standard
  - **Ignore offers** — dismiss a specific seller's order so you stop hearing about it during app session
- **Lives in your tray** — closing the window hides it to the system tray, the poller keeps running no matter what
  - Should be compatible with any OS, including Windows, macOS, and Linux. Since Warframe is not Mac compatabile, we'll be ready.

## How it works

Every 15 seconds, the app checks the Warframe Market API for the lowest sell order on each item you're tracking. It only looks at sellers who are currently in-game (so you're not chasing offline ghosts). If the price is at or below your target, you get a toast notification. It won't spam you — it only re-notifies when the price drops *lower* than what it already told you about.

## OS Notification Settings

- **Windows 11** 
  - Settings > Accessibility > [Visual effects](ms-settings:easeofaccess-visualeffects) > Dismiss notifications after this amount of time {drop down}
  - Settings > [Notifications](ms-settings:notifications) > Turn off Do Not Disturb settings to allow the pop-up to overlay the game.
- **Linux**
  - **KDE Plasma**: System settings > Personalization > Notifications > Hide popup after {drop down}

## Building from source

Requires [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```
git clone https://github.com/Huntk23/WarframeMarketTracker.git
cd WarframeMarketTracker
dotnet run
```

## License

[MIT](LICENSE)
