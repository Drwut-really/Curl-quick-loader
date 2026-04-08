# Curl Quick Loader

A Windows desktop application for creating and managing named preset curl commands — complete with headers, HTTP method, URL, request body, and extra flags. Run presets instantly from the GUI, copy the full curl command to clipboard, or trigger them headlessly from scripts and Windows Task Scheduler.

---

## Table of Contents

- [Installation](#installation)
- [GUI Usage](#gui-usage)
- [CLI Usage](#cli-usage)
- [Creating a Preset](#creating-a-preset)
- [Example Presets](#example-presets)
  - [Pushover — Push Notifications](#pushover--push-notifications)
  - [Discord — Webhook Message](#discord--webhook-message)
  - [Home Assistant — REST API](#home-assistant--rest-api)
- [Windows Task Scheduler](#windows-task-scheduler)
- [Import & Export](#import--export)
- [Building from Source](#building-from-source)

---

## Installation

### Option 1 — Self-contained (no .NET required)

Download and extract **[CurlQuickLoader-v0.0.1-win-x64-selfcontained.zip](releases/v0.0.1/CurlQuickLoader-v0.0.1-win-x64-selfcontained.zip)**, then run `CurlQuickLoader.exe`.

### Option 2 — Framework-dependent (smaller download)

1. Install the [.NET 10 Windows Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)
2. Download and extract **[CurlQuickLoader-v0.0.1-win-x64.zip](releases/v0.0.1/CurlQuickLoader-v0.0.1-win-x64.zip)**, then run `CurlQuickLoader.exe`

> **Note:** curl is required to run presets. Windows 10 (1803+) and Windows 11 include curl in `System32` — no extra install needed.

---

## GUI Usage

When you launch the app you'll see two panels:

**Left panel — Preset list**
- Lists all your saved presets with their name, method, and URL
- Use the search box to filter by name, URL, or method
- **New / Edit / Delete / Duplicate** buttons in the toolbar
- Double-click a preset or press **Enter** to edit; **Delete** key removes it

**Right panel — Command preview & actions**
- Shows the fully built curl command for the selected preset (updates live)
- **Copy Command** — copies the command to clipboard for use anywhere
- **Run Now** — executes curl directly and shows output in the terminal panel below

---

## CLI Usage

The same `CurlQuickLoader.exe` supports headless operation — no window is shown.

| Command | Description |
|---|---|
| `CurlQuickLoader.exe --list` | Print all preset names |
| `CurlQuickLoader.exe --run "PresetName"` | Execute a preset's curl command |
| `CurlQuickLoader.exe --export "PresetName"` | Print the curl command string to stdout |
| `CurlQuickLoader.exe --help` | Show usage information |

**Exit codes** mirror curl's own exit codes, so scripts can detect failures reliably.

```bat
CurlQuickLoader.exe --run "DailyHealthCheck"
if %errorlevel% neq 0 (
    echo Curl command failed with error %errorlevel%
    exit /b 1
)
```

```powershell
# Export a command string into a variable
$cmd = & "C:\Tools\CurlQuickLoader.exe" --export "MyPreset"
Write-Host $cmd
```

---

## Creating a Preset

1. Click **New** (or press the toolbar button)
2. Fill in the fields:
   - **Name** — a unique label used for CLI `--run` lookups
   - **Method** — GET, POST, PUT, PATCH, DELETE, HEAD, or OPTIONS
   - **URL** — the full endpoint URL
   - **Headers** — click **+ Add Header** to add key/value pairs; remove with **Remove Selected**
   - **Body** — raw request body (JSON, form data, etc.)
   - **Extra Flags** — any additional curl flags appended verbatim (e.g. `--insecure -v --max-time 30`)
3. The **Command Preview** at the bottom updates live as you type
4. Click **Save**

> Presets are stored in `presets\presets.json` in the same folder as the executable — copy the folder to move your presets between machines.

---

## Example Presets

### Pushover — Push Notifications

[Pushover](https://pushover.net) lets you send push notifications to your phone or desktop. After registering at pushover.net, you'll have an **Application Token** (from creating an app) and a **User Key** (from your account dashboard).

**Preset settings:**

| Field | Value |
|---|---|
| Name | `Pushover - Alert` |
| Method | `POST` |
| URL | `https://api.pushover.net/1/messages.json` |
| Headers | *(none required — Pushover uses form data)* |
| Body | *(leave empty)* |
| Extra Flags | `--form-string "token=YOUR_APP_TOKEN" --form-string "user=YOUR_USER_KEY" --form-string "title=Alert" --form-string "message=Hello from Curl Quick Loader!"` |

**Generated command:**
```bat
curl -X POST "https://api.pushover.net/1/messages.json" ^
  --form-string "token=YOUR_APP_TOKEN" ^
  --form-string "user=YOUR_USER_KEY" ^
  --form-string "title=Alert" ^
  --form-string "message=Hello from Curl Quick Loader!"
```

**Optional extra flags:**

| Flag | Purpose |
|---|---|
| `--form-string "priority=1"` | High priority (bypasses quiet hours) |
| `--form-string "priority=2" --form-string "retry=60" --form-string "expire=3600"` | Emergency priority — retries every 60s for up to 1 hour |
| `--form-string "sound=siren"` | Custom notification sound |
| `--form-string "device=my_iphone"` | Send to a specific registered device only |
| `--form-string "html=1"` | Enable HTML formatting in the message body |

---

### Discord — Webhook Message

Discord webhooks let you post messages to a channel without a bot account. Create one under **Channel Settings → Integrations → Webhooks**, then copy the webhook URL.

**Preset settings:**

| Field | Value |
|---|---|
| Name | `Discord - Deploy Notification` |
| Method | `POST` |
| URL | `https://discord.com/api/webhooks/YOUR_WEBHOOK_ID/YOUR_WEBHOOK_TOKEN` |
| Headers | `Content-Type: application/json` |
| Body | `{"content": "Deployment complete!", "username": "Deploy Bot"}` |
| Extra Flags | *(leave empty)* |

**Generated command:**
```bat
curl -X POST "https://discord.com/api/webhooks/YOUR_WEBHOOK_ID/YOUR_WEBHOOK_TOKEN" ^
  -H "Content-Type: application/json" ^
  --data-binary "{\"content\": \"Deployment complete!\", \"username\": \"Deploy Bot\"}"
```

**Rich embed example** (paste into the Body field):
```json
{
  "username": "Monitor Bot",
  "embeds": [{
    "title": "Server Status",
    "description": "All systems operational",
    "color": 3066993
  }]
}
```

---

### Home Assistant — REST API

Home Assistant exposes a REST API for triggering automations, calling services, and controlling devices. Generate a **Long-Lived Access Token** under your profile page at `http://homeassistant.local:8123/profile`.

#### Trigger an Automation

| Field | Value |
|---|---|
| Name | `HA - Trigger Automation` |
| Method | `POST` |
| URL | `http://homeassistant.local:8123/api/services/automation/trigger` |
| Header 1 | `Authorization: Bearer YOUR_LONG_LIVED_TOKEN` |
| Header 2 | `Content-Type: application/json` |
| Body | `{"entity_id": "automation.your_automation_name"}` |

#### Send a Notification to Your Phone

| Field | Value |
|---|---|
| Name | `HA - Phone Notification` |
| Method | `POST` |
| URL | `http://homeassistant.local:8123/api/services/notify/mobile_app_YOUR_PHONE` |
| Header 1 | `Authorization: Bearer YOUR_LONG_LIVED_TOKEN` |
| Header 2 | `Content-Type: application/json` |
| Body | `{"message": "Hello from Curl Quick Loader!", "title": "Alert"}` |

#### Turn a Switch On

| Field | Value |
|---|---|
| Name | `HA - Turn On Porch Light` |
| Method | `POST` |
| URL | `http://homeassistant.local:8123/api/services/switch/turn_on` |
| Header 1 | `Authorization: Bearer YOUR_LONG_LIVED_TOKEN` |
| Header 2 | `Content-Type: application/json` |
| Body | `{"entity_id": "switch.porch_light"}` |

**Generated command:**
```bat
curl -X POST "http://homeassistant.local:8123/api/services/switch/turn_on" ^
  -H "Authorization: Bearer YOUR_LONG_LIVED_TOKEN" ^
  -H "Content-Type: application/json" ^
  --data-binary "{\"entity_id\": \"switch.porch_light\"}"
```

> Replace `homeassistant.local:8123` with your instance's IP or external URL. For remote access use `https://` with your Nabu Casa or custom domain.

---

## Windows Task Scheduler

Use `--run` to fire a preset on a schedule with no user interaction.

1. Open **Task Scheduler** → **Create Basic Task**
2. Set your trigger (daily, on login, on event, on startup, etc.)
3. Under **Action**, choose **Start a program** and set:
   - **Program/script:** `C:\Tools\CurlQuickLoader.exe`
   - **Add arguments:** `--run "DailyHealthCheck"`
4. Under **Settings**, optionally enable *"If the task fails, restart every"* for reliability

The task runs silently — no window opens. The exit code is forwarded so Task Scheduler can detect and log failures automatically.

**Example: send a Pushover alert every morning at 8 AM**

Create a preset named `Morning Alert`, then set the Task Scheduler trigger to *Daily at 8:00 AM* with arguments `--run "Morning Alert"`.

---

## Import & Export

**Export:** Go to **File → Export Presets** to save all presets to a `.json` file — useful for backup or sharing a preset library with others.

**Import:** Go to **File → Import Presets** to load presets from a `.json` file. Presets whose name already exists are skipped (no overwrite without editing the name first).

You can also edit `presets\presets.json` directly in any text editor — it's plain, human-readable JSON.

---

## Changelog

### v0.0.1 (prerelease)
- **Fix:** Application now correctly detects its own directory when deployed as a single-file exe — presets folder is created next to the `.exe`, not in a temp extraction folder
- **Fix:** `SplitterDistance` is now set after the form has finished layout, preventing a silent startup crash on certain Windows 11 configurations
- **Fix:** `CheckCurlAvailable()` is now called in the `Load` event (after the window is visible) rather than in the constructor, eliminating a startup hang if the system is slow to respond
- **Fix:** `Application.SetHighDpiMode(SystemAware)` added to improve rendering on high-DPI displays
- **New:** Logging system — all startup events, errors, and preset operations are written to `logs\app-YYYY-MM-DD.log` next to the exe. Check this file if the app fails to start

---

## Building from Source

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```bat
git clone https://github.com/Drwut-really/Curl-quick-loader.git
cd Curl-quick-loader

REM Run directly (development)
dotnet run --project src\CurlQuickLoader\CurlQuickLoader.csproj

REM Publish — self-contained single .exe (no .NET install needed on target machine)
dotnet publish src\CurlQuickLoader\CurlQuickLoader.csproj ^
  -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

REM Publish — framework-dependent (smaller, requires .NET 10 Desktop Runtime)
dotnet publish src\CurlQuickLoader\CurlQuickLoader.csproj ^
  -c Release -r win-x64 --self-contained false
```

Output: `src\CurlQuickLoader\bin\Release\net10.0-windows\win-x64\publish\CurlQuickLoader.exe`
