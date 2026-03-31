# Relationship Planner

This is the Blazor/MudBlazor version of the relationship planner.

## What it is

- A working Blazor Web App
- MudBlazor integrated for the UI
- SQLite-backed shared planner data
- Separate from the Node/SQLite app

## Run it

```bash
dotnet run
```

Or from the project directory:

```bash
dotnet watch
```

## Current features

- Dashboard with relationship summary
- Shared planner calendar and selected-day entries
- Finance management with per-user monthly budget lines
- Finance section management for custom budget categories
- Per-user profiles and accent colors
- Optional Steam ID64 per user for library checks
- "Sign in with Steam" flow in Settings to attach a Steam account to the active user
- Shared settings for relationship details
- Custom color settings for planner kinds and game statuses
- Style presets for Persona, RWBY, Pokémon, D&D, Cozy romance, and Retro arcade moods
- Dark mode with saved preference and system follow mode
- RAWG game search with optional Steam ownership checks
- A browsable Steam library table for the active user, when Steam is linked
- SQLite persistence in `App_Data/planner.db`

## Run it

```bash
dotnet run
```

The app runs locally on:

- `http://localhost:5132`

## Game search

- Enter the RAWG API key in `Settings` to enable game search.
- Enter the Steam web API key in `Settings` to enable ownership checks.
- Use `Sign in with Steam` in `Settings` to link the active user to a Steam account.
- You can still type a Steam ID64 manually on a user profile if you prefer.
- `Settings` also includes color pickers for date ideas, reminders, memories, countdowns, got together items, and the game status colors.
- `Settings` also includes a style preset selector that changes the overall app mood across the shell, cards, and controls.
- If a RAWG result exposes a Steam app ID, the Games page can check whether the active user owns it.
- The Games page also shows the current user's full Steam library in a collapsible table when Steam is linked and the web API key is saved.

## Finance tab

- The `Finance` tab is per-user and month-based.
- It uses a spreadsheet-style budget layout with income, fixed costs, flexible spending, savings, and cash buffer lines.
- Use `Load defaults` to seed the month from the editable default template for that user.
- The default template itself can be managed in the Finance page, so you can add, edit, or delete the starter lines.
- Each line tracks budgeted amount, actual amount, due date, notes, and whether it is shared.
- The Finance page also lets you add, rename, recolor, and delete custom sections.

## Steam login flow

- Choose the active user in `Settings`.
- Click `Sign in with Steam`.
- Steam returns a verified Steam ID64 to that active user.
- Ownership checks in `Games` use that linked Steam account automatically.

