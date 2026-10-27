### Map editor

Custom maps are created and edited with the built-in **Map Editor** (`Tools → Map Editor`).

**Create a new map:**

1. Open the editor via **Tools → Map Editor**, or double-click any `CustomMapConfig` asset
   in the Project window to open it in the editor.
2. Click **New** and pick a map size (**Small 15×10 / Large 19×13 / ExtraLarge 38×26** —
   the size is fixed after creation and cannot be changed).
3. Choose where to save the asset (by default `Assets/Maps/`).
4. Paint tiles on the grid: pick one of the six tile types
   (`Background1..3`, `Wall1..3`) in the header, **LMB** paints, **RMB** erases.
   A whole stroke (press → drag → release) is a single **Undo** step; the theme
   dropdown changes the preview only, it is not saved into the map.
5. **Save** (`Ctrl+S`) writes the asset; closing the window or loading another map with
   unsaved changes prompts *Save changes?* (Save / Don't Save / Cancel).

**Using a custom map in a match:** assign the `CustomMapConfig` asset to an entry in
`CustomMapsAuthoring` (the `CustomMaps` object in `Assets/Scenes/SubScene/GameEntityScene.unity`),
then select it via the map settings in the main menu.

Map dimensions (`Assets/Configs/MapSizesConfig.asset`) and the tileset atlas layout
(`Assets/Configs/TileSetConfig.asset`) are shared data assets — the editor, the authoring
components and the runtime systems all read the same values.