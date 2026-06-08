# Showlist2026

> **Note:** This is a personal project built for a very specific use case by the author. It is part of a series of internal systems for managing media workflows and is tailored to the author's particular setup and preferences. While parts of it may be useful or adaptable for others, it is not designed as a general-purpose application and comes with no guarantees of suitability for other environments.

A personal TV show tracker built with Blazor Server, EF Core, and the TVMaze API. Track shows, monitor downloads, get notifications for new episodes, and manage your watchlist.

## Requirements

- .NET 10 SDK
- SQL Server (local or remote)

## Quick Start

```bash
# Clone and build
git clone <repo-url>
cd Showlist2026
dotnet restore
dotnet build

# Configure your connection string (see Configuration below)
cd src/Showlist2026.Web
dotnet user-secrets set "ConnectionStrings:Default" "Server=YOUR_SERVER;Database=showlist2026;User ID=YOUR_USER;Password=YOUR_PASS;TrustServerCertificate=True;"

# Run
dotnet run --project src/Showlist2026.Web
```

The database is created and migrations are applied automatically on startup.

## Configuration

All settings live in `src/Showlist2026.Web/appsettings.json`. For secrets (passwords, API keys), use [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) or environment variables instead of editing `appsettings.json` directly.

### User Secrets Setup

```bash
cd src/Showlist2026.Web
dotnet user-secrets init  # only needed once
```

Then set any config value:

```bash
dotnet user-secrets set "Key:SubKey" "value"
```

Or create/edit the secrets file directly at:
- **Windows:** `%APPDATA%\Microsoft\UserSecrets\<user-secrets-id>\secrets.json`
- **Linux/Mac:** `~/.microsoft/usersecrets/<user-secrets-id>/secrets.json`

---

### Connection String

The database connection. Required.

| Key | Description | Default |
|-----|-------------|---------|
| `ConnectionStrings:Default` | SQL Server connection string | `Server=localhost;Database=showlist2026;TrustServerCertificate=True;Integrated Security=True;` |

**User Secrets example:**

```bash
dotnet user-secrets set "ConnectionStrings:Default" "Server=10.0.3.253;Database=showlist2026;User ID=myuser;Password=mypassword;TrustServerCertificate=True;"
```

**Environment variable example:**

```bash
export ConnectionStrings__Default="Server=10.0.3.253;Database=showlist2026;User ID=myuser;Password=mypassword;TrustServerCertificate=True;"
```

---

### Showlist Options

Paths and URLs used by the application.

| Key | Description | Default |
|-----|-------------|---------|
| `Showlist:TvNameListPath` | Root folder where show name folders are scanned to match shows to folders | `C:\tvnamelist\` |
| `Showlist:ShowFolderBasePath` | Base path where new show folders are created when you select a show as wanted | `F:\tv_name_list\` |
| `Showlist:TvMazeBaseUrl` | TVMaze API base URL. Only change if using a proxy/mirror | `http://api.tvmaze.com` |
| `Showlist:NzbPlanetApiKey` | API key for NZBPlanet NZB search | *(empty)* |

**appsettings.json:**

```json
"Showlist": {
  "TvNameListPath": "C:\\tvnamelist\\",
  "ShowFolderBasePath": "F:\\tv_name_list\\",
  "TvMazeBaseUrl": "http://api.tvmaze.com",
  "NzbPlanetApiKey": ""
}
```

**User Secrets example:**

```bash
dotnet user-secrets set "Showlist:TvNameListPath" "/mnt/media/tvnamelist/"
dotnet user-secrets set "Showlist:ShowFolderBasePath" "/mnt/media/tv_name_list/"
dotnet user-secrets set "Showlist:NzbPlanetApiKey" "your-api-key"
```

---

### Notifications

Three notification channels are supported. Enable any combination. All are disabled by default.

#### Pushover

Push notifications to your phone via [Pushover](https://pushover.net/).

| Key | Description |
|-----|-------------|
| `Notifications:Pushover:Enabled` | `true` to enable |
| `Notifications:Pushover:ApiKey` | Your Pushover application API token |
| `Notifications:Pushover:UserKey` | Your Pushover user key |

```bash
dotnet user-secrets set "Notifications:Pushover:Enabled" "true"
dotnet user-secrets set "Notifications:Pushover:ApiKey" "your-app-token"
dotnet user-secrets set "Notifications:Pushover:UserKey" "your-user-key"
```

#### Discord

Post messages to a Discord channel via webhook.

| Key | Description |
|-----|-------------|
| `Notifications:Discord:Enabled` | `true` to enable |
| `Notifications:Discord:WebhookUrl` | Discord webhook URL (Server Settings > Integrations > Webhooks) |

```bash
dotnet user-secrets set "Notifications:Discord:Enabled" "true"
dotnet user-secrets set "Notifications:Discord:WebhookUrl" "https://discord.com/api/webhooks/123456/abcdef"
```

#### Email

Send notifications via SMTP.

| Key | Description |
|-----|-------------|
| `Notifications:Email:Enabled` | `true` to enable |
| `Notifications:Email:SmtpHost` | SMTP server hostname (e.g. `smtp.gmail.com`) |
| `Notifications:Email:SmtpPort` | SMTP port (default: `587`) |
| `Notifications:Email:From` | Sender email address |
| `Notifications:Email:To` | Recipient email address |
| `Notifications:Email:Username` | SMTP username |
| `Notifications:Email:Password` | SMTP password or app-specific password |

```bash
dotnet user-secrets set "Notifications:Email:Enabled" "true"
dotnet user-secrets set "Notifications:Email:SmtpHost" "smtp.gmail.com"
dotnet user-secrets set "Notifications:Email:SmtpPort" "587"
dotnet user-secrets set "Notifications:Email:From" "myemail@gmail.com"
dotnet user-secrets set "Notifications:Email:To" "myemail@gmail.com"
dotnet user-secrets set "Notifications:Email:Username" "myemail@gmail.com"
dotnet user-secrets set "Notifications:Email:Password" "your-app-password"
```

---

### Full appsettings.json Reference

```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Database=showlist2026;TrustServerCertificate=True;Integrated Security=True;"
  },
  "Showlist": {
    "TvNameListPath": "C:\\tvnamelist\\",
    "ShowFolderBasePath": "F:\\tv_name_list\\",
    "TvMazeBaseUrl": "http://api.tvmaze.com",
    "NzbPlanetApiKey": ""
  },
  "Notifications": {
    "Pushover": {
      "Enabled": false,
      "ApiKey": "",
      "UserKey": ""
    },
    "Discord": {
      "Enabled": false,
      "WebhookUrl": ""
    },
    "Email": {
      "Enabled": false,
      "SmtpHost": "",
      "SmtpPort": 587,
      "From": "",
      "To": "",
      "Username": "",
      "Password": ""
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

---

## Features

### Core
- **Show tracking** — search and add shows from TVMaze, mark as wanted/unwanted
- **Episode tracking** — mark episodes as watched, track watch history
- **Filters** — filter by show, genre, network, web network, country, language, type
- **Notifications** — get notified via Pushover, Discord, or Email when new episodes air or new seasons start
- **Folder aliases & continuation links** — map alternate folder names to a show, and link continuation shows (a "new" TVMaze listing that's really a later season of an earlier show) with a **season offset**. Files that arrive under the old show's name with continued season numbering (e.g. `Foo/Season 3/...` where the new show's S01 = Foo's S03) are matched to the correct show's episodes in place. Managed per-show on the show detail page.

### Views
- **Airing Now** — episodes airing around the current date for your selected shows
- **Calendar** — week/month grid view of upcoming episodes with date picker navigation
- **Missed** — episodes you may have missed
- **Undecided** — new shows to accept or reject in bulk
- **Unwatched** — next unwatched episode per show with progress bars
- **Coming Soon** — new shows premiering soon
- **Downloaded** — downloaded episode history from scanned directories
- **Watch History** — chronological log of watched episodes

### Analytics
- **Statistics** — watch time, genre breakdown, most-watched shows, episodes per month
- **Storage Dashboard** — disk usage per show folder, matched/unmatched folder detection, filtering and sorting
- **Download Progress** — per-show download completion with missing episode lists
- **Trending** — currently trending shows from TVMaze (respects your type and show filters)
- **Compare** — side-by-side show comparison

### Admin
- **Data refresh** — refresh networks, shows, episodes, and folder names from TVMaze
- **Export/Import** — JSON and CSV export of selections and watched history
- **Import from paths** — import a text file of video file paths to bulk-mark shows as wanted and episodes as watched
- **Duplicate detection** — find potential duplicate shows in the database
- **Notification testing** — test each notification channel individually
- **NZB site management** — configure search sites for NZB lookups
- **TV directory management** — configure directories to scan for downloads

---

## Pages

| Route | Description |
|-------|-------------|
| `/` | Home — stats overview, tonight's episodes, background job status |
| `/airing` | Episodes airing around now for your selected shows |
| `/calendar` | Week/month calendar view of upcoming episodes |
| `/missed` | Episodes you may have missed |
| `/missed-shows` | Undecided shows — bulk accept or reject |
| `/unwatched` | Next unwatched episode per show |
| `/coming-soon` | New shows premiering soon |
| `/downloaded` | Downloaded episode history |
| `/download-progress` | Per-show download completion with missing episode lists |
| `/no-folder` | Selected shows without a matched folder |
| `/watched-history` | Chronological watch history |
| `/stats` | Watch statistics — genres, trends, watch time |
| `/storage` | Storage dashboard — disk usage per show folder |
| `/search` | Advanced search (local + TVMaze API) |
| `/trending` | Trending shows from TVMaze |
| `/compare` | Side-by-side show comparison |
| `/showlist/show/{id}` | Show detail with episodes, NZB search, catch-up |
| `/filters/*` | Filter management (shows, genres, networks, countries, languages, types) |
| `/admin` | Admin operations, data export/import |
| `/admin/nzbsites` | NZB site configuration |
| `/admin/tvdirectories` | TV directory scan configuration |
| `/hangfire` | Hangfire background job dashboard |
| `/health` | Health check endpoint (DB + TVMaze API) |

## Background Jobs

Managed by Hangfire. Status is visible on the home page and at `/hangfire`.

| Job | Schedule | Description |
|-----|----------|-------------|
| RefreshShowDates | Every hour at :06 | Fetches show update timestamps from TVMaze |
| RefreshShows | Every hour at :02, :22, :42 | Updates show metadata and episodes for changed shows |
| PopulateShowFolderNames | Every 3 hours at :25 | Matches selected shows to folders on disk |
| ShowDownloadedJob | Every 10 minutes | Scans TV directories for new downloads, sends notifications |
| NewSeasonNotifications | Daily at 8:00 AM | Notifies when a new season starts for selected shows |

## Export / Import

From the Admin page (`/admin`):

- **Export (JSON/CSV):** Downloads your show selections (wanted/unwanted) and watched episode history. Uses TVMaze IDs so exports are portable across database instances.
- **Import (JSON):** Upload a previously exported JSON file. Idempotent — skips records that already exist. Useful for migrating to a new database or restoring from backup.
- **Import from paths:** Upload a text file of video file paths (one per line) to bulk-import watched shows. Matches folder names to shows in the database, marks them as wanted, and marks episodes as watched based on S01E01/1x01 patterns in filenames. Only matches shows premiering in 2015 or earlier, and for those shows marks all prior episodes as watched too.

## Existing Database Setup

If you have an existing database from a previous version, you need to mark the initial migration as applied:

```sql
INSERT INTO [__EFMigrationsHistory] (MigrationId, ProductVersion)
VALUES ('<migration-id>', '10.0.5');
```

Find the migration ID in `src/Showlist2026.Data/Migrations/` (the timestamp-prefixed filename).

## Project Structure

```
src/
  Showlist2026.Core/          Entities, models, service interfaces, config
  Showlist2026.Data/          EF Core DbContext and migrations
  Showlist2026.Web/           Blazor Server app
    Components/
      Layout/                 App shell and nav menu
      Pages/                  All page components
      Shared/                 Reusable components
    Health/                   Health check implementations
    Services/                 Service implementations
```

## Tech Stack

- .NET 10 / Blazor Server (Interactive Server rendering)
- Entity Framework Core 10 (SQL Server)
- Hangfire (background job scheduling)
- TVMaze API (show/episode data)
- Flurl.Http (HTTP client)
- Bootstrap 5.3 (UI framework)
- Pushover / Discord / Email (notifications)
- PWA support (manifest, service worker)
