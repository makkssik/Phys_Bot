# WeatherBot

## Project Summary
WeatherBot is a C# Telegram bot providing weather updates, subscription features, and emergency alerts.

## Features
- Get current weather for any city.
- Subscribe to daily or emergency weather notifications.
- All user and subscription data is automatically cached in `users.json` (persisted across bot restarts).

## Setup
1. Clone this repo.
2. Install .NET 9 and restore dependencies.
3. Add your Telegram bot token in `WeatherBot/token.txt`.
4. Build and run:
   ```bash
   dotnet run --project Phys_Bot.csproj
   ```

## Developer Notes
- **User Data:**
  - `users.json` is generated automatically and stores user subscriptions.
  - **Do NOT commit user data.**
  - Example `.gitignore` entry:
    ```
    users.json
    ```

- If `users.json` is missing, the bot will create a new empty cache on startup.
- For privacy, do not share or upload `users.json`.

---
Maintained by Kenny & contributors.
