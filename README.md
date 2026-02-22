# AMAZE-CLONE

## Level Editor

### How to Open
- In Unity, open `Tools > Level Editor`.

### How to Use
- Set `Level ID`, `Width`, and `Height`, then click `Create / Resize`.
- `Level ID` should be sequential (1, 2, 3, ...) so each level has a consistent numeric order.
- Choose `Mode`:
  - `ToggleCells`: click cells to toggle Available/Blocked.
  - `SetStartNode`: click a cell to set the Start (it will be forced Available).
- Use grid tools:
  - `Fill All Available`, `Fill All Blocked`, `Invert`, `Auto Generate`.
- Configure auto-generation with `Wall Density` and `Random`.
- `Validate` checks the level; status appears in the header.
- `Save JSON` writes to `Assets/Levels/Level_{ID}.json`.
- `Load JSON` loads by `Level ID` from the same path.
- Materials:
  - Select `Ground`, `Wall`, and `Ground Paint` materials from `Assets/Art/Materials/EditorMaterials`.

## Game Settings Panel

### How to Open
- In Unity, open `Tools > Game Settings Panel`.

### How to Use
- Edit `Ball Speed`, `Roll Speed Multiplier`, and `Camera Y Offset`.
- Click `Save` to write `Assets/Settings/GameSettings.json`.
- Click `Load` to reload values from disk.

## Save Tools

### How to Open
- In Unity, open `Tools > Reset Save`.

### How to Use
- Confirm the dialog to delete the local save file at `Application.persistentDataPath/saves/save.save`.
- If the file does not exist, a log message indicates it was not found.

## Time Spent
- Approximately 13 hours
