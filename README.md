# Warframe Market Tracker

---

A lightweight desktop app that watches personally selected [warframe.market](https://warframe.market)  item prices in the background and pings you when a deal hits your target — so you don't have to constantly refresh the sellers tab or wait on a seller to click on the buyer tab; because we all know buying a deal is better than waiting for a deal.

## Why this exists

If you've ever left [warframe.market](https://warframe.market) open in a browser tab while playing, you've probably noticed your fans kick up due to CPU pressure reconnecting to the site's virtual tables? It is a very feature-rich website, but when watching multiple items can be CPU intensive through your browser.

Chromium is and can be hungry; especially when refreshing a market page every few seconds with so many assets to snipe a deal doesn't help. I built this to solve that problem for myself: a small, native app that polls the Warframe Market API quietly in the background, uses barely any resources, and only bothers you when there's actually something worth buying.

Don't get me wrong; I have personally run into a few sellers trying to create a ripple in the market and undercutting users by a good 20% in plat. Well, you can ignore this fake offer after attempting to send a whisper and seeing they are not online. The app will get back to looking for the deal you are targeting for.

## What it does

- **Track items by name** — search from the full [warframe.market](https://warframe.market) item catalog with autocomplete
  - **Persistence** — the app will save and restore the items you are tracking automatically
- **Set your price** — pick a platinum threshold per item, optionally filter by mod rank
- **Get notified** — Cross-platform native notification when an in-game seller lists at or below your target
  - **Copy the whisper** — one click on the notification copies the `/w` trade message to your clipboard, ready to paste in-game, all while using the same [warframe.market](https://warframe.market) standard
  - **Ignore offers** — dismiss a specific seller's order so you stop hearing about it during app session
- **Lives in your tray** — closing the window hides it to the system tray, the poller keeps running no matter what
  - Should be compatible with any OS, including Windows, macOS, and Linux. Since Warframe is not Mac compatabile, we'll be ready.

## How it works

Every 15 seconds, the app checks the Warframe Market API for the lowest sell order on each item you're tracking. It only looks at sellers who are currently in-game (so you're not chasing offline ghosts). If the price is at or below your target, you get a toast notification. It won't spam you — it only re-notifies when the price drops *lower* than what it already told you about.

## Building from source

Requires [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```
git clone https://github.com/Huntk23/WarframeMarketTracker.git
cd WarframeMarketTracker
dotnet run
```

To publish a self-contained executable:

```
dotnet publish -c Release -r win-x64 --self-contained
```

## Tech stack

- [Avalonia UI](https://avaloniaui.net/) with [ShadUI](https://github.com/anthropics/ShadUI) theming
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/) for MVVM plumbing
- [Microsoft.Extensions.Hosting](https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host) for DI and background services
- [Serilog](https://serilog.net/) for structured logging
- [Avalonia.Labs.Notifications](https://github.com/AvaloniaUI/Avalonia.Labs) for native Windows toast notifications
- C# 14 / .NET 10

## License

[MIT](LICENSE)

## TODO

Further refine documentation as more users adopt the tool.