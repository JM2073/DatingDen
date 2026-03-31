# Relationship Planner

A mobile-friendly relationship planning app starter for storing date ideas, reminders, countdowns, and memories in a local SQLite database that persists across restarts.

The current app uses a MudBlazor-inspired visual style, and the separate Blazor/MudBlazor rebuild starter lives in `../mudblazor-rebuild/`.
There is also a .NET MAUI mobile project in `../RelationshipPlanner.Mobile/`, ready for Android emulator testing.

## What It Includes

- Persistent local SQLite storage
- Responsive web UI that works on phones and desktops
- Wide-screen layout that uses the extra space on larger monitors
- MudBlazor-inspired surfaces, spacing, and color treatment
- Multiple user profiles with their own colors, themes, and reminder defaults
- Active user switching in the app shell
- Per-user game status tracking for shared game plans
- Status-only game records that do not add a dated planner event
- Create, edit, delete, and list entries
- Dialog-based add/edit flows for planner items, day memos, and delete confirmation
- Interactive month calendar with day selection and agenda view
- Day memos attached to calendar dates, including a touch-friendly sketch pad
- RAWG game search and game-plan entries
- Game plan cards and status dashboard that show RAWG banner art where available
- Light and dark mode toggle with saved preference
- A dedicated settings page for relationship details and common defaults
- Entry types for:
  - Date ideas
  - Reminders
  - Countdowns
  - Memories
  - Got together dates
  - Game plans
- A small HTTP API for future expansion

## How It Runs

The app has two parts, but they run together in one Node process:

- `server.js` starts the HTTP server
- The server serves the frontend from `public/`
- The same server also exposes the API under `/api/*`
- Data is saved to `data/relationship-planner.db`
- Run the commands in this README from the `node-app/` folder.

## Solution And Mobile Project

- The repo now includes `../RelationshipPlanner.slnx`, which contains both the Blazor rebuild and the MAUI mobile project.
- Open `../RelationshipPlanner.slnx` in Visual Studio to work with the .NET apps together.
- The mobile project is `../RelationshipPlanner.Mobile/`.
- In Visual Studio, set `../RelationshipPlanner.Mobile` as the startup project and use the Android emulator to test and debug it.

The database file stays on disk, so your saved items remain after stopping and restarting the app.

## Requirements

- Node.js 24 or newer
- A device on the same Wi-Fi network if you want to open it from a phone

No extra npm dependencies are currently required because the project uses Node's built-in modules.

For game search, set a RAWG API key on the server:

```bash
$env:RAWG_API_KEY="your-key-here"
```

You can also place the key in a local `.env` file in the `node-app/` folder. The app loads `.env` automatically on startup.

## Run It

Start the app:

```bash
npm run start
```

For development with automatic restart when `server.js` changes:

```bash
npm run dev
```

The default address is:

- `http://localhost:3000`

If you want a different host or port, set environment variables before starting:

- `PORT` defaults to `3000`
- `HOST` defaults to `0.0.0.0`

Examples:

```bash
$env:PORT=4000
$env:HOST="127.0.0.1"
npm run start
```

## Open On Mobile

To use the app from a mobile device on the same Wi-Fi network:

1. Start the app on your computer.
2. Find your computer's local IP address.
3. Open `http://YOUR-IP:3000` on the phone.

Example:

- `http://192.168.1.25:3000`

Because the server listens on `0.0.0.0` by default, it can accept connections from other devices on the same network.

## Data Storage

SQLite database file:

- `data/relationship-planner.db`

The app creates the `data/` folder automatically if it does not exist.

## Database Schema

The app stores data in several local SQLite tables:

### `entries`

Columns:

- `id` - auto-incrementing primary key
- `kind` - one of `date`, `reminder`, `countdown`, `memory`, `got_together`, `game_plan`, or `game_status`
- `title` - entry title
- `details` - freeform notes
- `event_date` - the main scheduled date/time in ISO format
- `reminder_at` - optional reminder date/time in ISO format
- `game_status` - legacy fallback mirror for the last saved game status
- `rawg_game_id` - RAWG game id
- `rawg_slug` - RAWG slug
- `rawg_background_image` - RAWG cover image URL
- `rawg_platforms` - JSON text containing RAWG platform data
- `rawg_metacritic` - RAWG Metacritic score
- `rawg_released` - RAWG release date text
- `created_by_user_id` - the user who created the entry
- `updated_by_user_id` - the user who last updated the entry
- `created_at` - creation timestamp
- `updated_at` - last update timestamp

### `users`

Columns:

- `id` - auto-incrementing primary key
- `name` - display name
- `created_at` - creation timestamp
- `updated_at` - last update timestamp

### `user_settings`

Key/value settings stored per user.

Columns:

- `user_id` - user id
- `key` - setting name
- `value` - stored value
- `updated_at` - last update timestamp

Current keys:

- `accent_color`
- `preferred_theme`
- `default_reminder_minutes`

### `entry_user_settings`

Key/value settings stored per user and per entry.

Columns:

- `entry_id` - entry id
- `user_id` - user id
- `key` - setting name
- `value` - stored value
- `updated_at` - last update timestamp

Current key:

- `game_status`
- `game_rating`

### `settings`

Columns:

- `key` - setting name
- `value` - stored setting value

Current settings keys:

- `couple_name`
- `relationship_started_at`

These tables are created and migrated automatically on startup so the app can keep existing data when the schema changes.

## Project Structure

- `server.js` - HTTP server, API, SQLite persistence
- `public/index.html` - app shell
- `public/styles.css` - responsive styling
- `public/app.js` - client-side UI logic, calendar rendering, and filters
- `data/relationship-planner.db` - local saved data
- `mudblazor-rebuild/` - separate Blazor/MudBlazor rebuild starter

## API

The following routes are available:

- `GET /health`
- `GET /api/users`
- `GET /api/users/:id`
- `POST /api/users`
- `PUT /api/users/:id`
- `GET /api/entries?userId=...`
- `GET /api/entries/:id?userId=...`
- `POST /api/entries`
- `PUT /api/entries/:id`
- `DELETE /api/entries/:id`

Entry payload fields:

- `kind` must be one of `date`, `reminder`, `countdown`, `memory`, `got_together`, `game_plan`, or `game_status`
- `title` is required
- `details` is optional text
- `eventDate` is required and must be an ISO date string
- `reminderAt` is optional and must be an ISO date string if provided
- `userId` selects which user is creating or editing the entry
- When `kind` is `game_plan` or `game_status`, the app can also store RAWG metadata such as the game id, slug, cover image, platforms, Metacritic score, and release date

The `got_together` kind is rendered as elapsed time since that date, so it works well as a relationship anniversary or "since we started dating" counter.

The `game_plan` kind is intended for planned play sessions. Use the RAWG search panel to find a game, then click `Use` to prefill the form with its data.
The `game_status` kind is for status-only game records. Use the `Status only` action in RAWG search or the selected game panel when you want to track a game without creating a dated planner event.
You can also quick-add a saved game status card back into the planner from the Games management panel when you want to turn status tracking into a dated plan.
Saved game records power the game status dashboard for the currently active user, which groups entries into `want_to_play`, `playing`, `played`, and `finished` buckets and shows a banner preview when one is available.
The Games section also includes a compact status management card so you can change a game's status directly without opening the planner form.
From the Games management card, you can also open a dialog to rate a game from 1 to 5 stars; that rating is stored per user on the existing game entry.

RAWG cover art is cached locally by the server in `public/rawg-cache/`, so the app can reload saved game banners without depending on the remote RAWG image URL each time.

## Settings

Open the `Settings` tab in the top bar to manage the common details that keep the planner personalized:

- Couple or project name
- Relationship start date
- A live time-together summary and the next anniversary date based on the relationship start date

Below that is the Users section, where each profile can set:

- Display name
- Accent color used to identify their items
- Preferred theme
- Default reminder lead time in minutes

Theme, color, and reminder defaults are stored per user in the SQLite database so they persist across restarts.

Use the active user switcher in the hero area to change whose personal preferences and game statuses are being edited.

## Calendar

- Use the `Prev`, `Today`, and `Next` buttons to move between months.
- Click any day to select it.
- The calendar shows how many saved items fall on each day.
- The agenda below the calendar lists the entries for the selected day.
- Day memos can be added from the selected-day panel, and each memo shows the user who created it.
- Add and edit actions open in dialogs, and delete actions use a confirmation dialog.
- Use `Use selected date` to copy the chosen day into the form's date field.

## Notes

- The database seeds a default `You` user the first time it is created so the app always has an active profile to work with.
- The server currently uses Node's built-in `node:sqlite` module, which is marked experimental in Node 24.
- If you want, we can swap to a more traditional SQLite package later for long-term stability.
- RAWG cover images are cached locally in `public/rawg-cache/` and served by the app.

## Next Upgrades

- Reminder notifications
- Authentication for private sharing
- Shared couple accounts across devices
- Search, tags, and favorites
- Calendar export and sync
