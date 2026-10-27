using System;
using TanksKombat2.Client;
using TanksKombat2.Shared;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace TanksKombat2.MapEditor
{
    // Map editor window for CustomMapConfig assets.
    // Edits an in-memory working copy (Instantiate of the asset) — the asset itself is only
    // touched on Save (CopySerialized + SaveAssets), so "Don't Save" simply drops the copy and
    // can never leak edits to disk via unrelated AssetDatabase.SaveAssets() calls.
    public class CustomMapEditorWindow : EditorWindow
    {
        private const string DefaultMapsFolder = "Assets/Maps";
        private const string WindowTitle = "Map Editor";

        private CustomMapConfig _config; // the original asset (never modified directly)
        private CustomMapConfig _workingCopy; // in-memory copy all edits go to
        private MapSizeConfig _mapSizesConfig;
        private TileSetConfig _tileSetConfig;
        private MapTile _brush = MapTile.Wall1;
        private MapTheme _theme = MapTheme.Hyrule;

        private bool _painting;
        private int _undoGroup = -1;

        [MenuItem("Tools/Map Editor")]
        public static void Open()
        {
            GetWindow<CustomMapEditorWindow>();
        }

        // Entry point for double-click opens; the window unload guard asks about
        // unsaved changes before the new config replaces the working copy.
        public static void Open(CustomMapConfig config)
        {
            var window = GetWindow<CustomMapEditorWindow>();
            window.TryLoadConfig(config);
            window.Focus();
        }

        // Opens the map editor when a CustomMapConfig asset is double-clicked in the
        // Project window (instead of Unity's default select-in-inspector behavior).
        [OnOpenAsset]
        private static bool OnOpenAsset(EntityId instanceId, int line)
        {
            var config = EditorUtility.EntityIdToObject(instanceId) as CustomMapConfig;
            if (config == null)
                return false;

            Open(config);
            return true;
        }

        private void TryLoadConfig(CustomMapConfig config)
        {
            if (config == null)
                return;

            // With hasUnsavedChanges set, Unity's window unload guard would prompt on close;
            // for an in-window load we ask explicitly and only proceed when allowed.
            if (hasUnsavedChanges && !ConfirmDiscardChanges())
                return;

            LoadConfig(config);
        }

        private void OnEnable()
        {
            titleContent = new GUIContent(TitleText);
            minSize = new Vector2(720, 480);
            wantsMouseMove = true;

            saveChangesMessage = "Save changes?";
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        private string TitleText => WindowTitle + (hasUnsavedChanges ? " *" : string.Empty);

        public override void SaveChanges()
        {
            SaveAsset();
            base.SaveChanges();
        }

        public override void DiscardChanges()
        {
            DropWorkingCopy();
            hasUnsavedChanges = false;
            base.DiscardChanges();
        }

        private bool ConfirmDiscardChanges()
        {
            var answer = EditorUtility.DisplayDialogComplex(
                WindowTitle,
                saveChangesMessage,
                "Save",
                "Cancel",
                "Don't Save");

            switch (answer)
            {
                case 0: // Save
                    SaveAsset();
                    return true;

                case 1: // Cancel
                    return false;

                default: // Don't Save
                    DiscardChanges();
                    return true;
            }
        }

        private void OnGUI()
        {
            HandleSaveShortcut();

            DrawToolbar();

            if (_mapSizesConfig == null)
                _mapSizesConfig = AssetDatabase.LoadAssetAtPath<MapSizeConfig>(MapSizeConfig.DefaultAssetPath);

            if (_tileSetConfig == null)
                _tileSetConfig = AssetDatabase.LoadAssetAtPath<TileSetConfig>(TileSetConfig.DefaultAssetPath);

            if (_config == null || _workingCopy == null)
            {
                EditorGUILayout.HelpBox(
                    "Create a new map ('New') or double-click a CustomMapConfig asset in the Project window.",
                    MessageType.Info);
                return;
            }

            DrawHeader();
            DrawGrid();
            DrawStatusBar();
        }

        // Ctrl+S inside the window saves the map.
        private void HandleSaveShortcut()
        {
            var e = Event.current;
            if (e.type == EventType.ValidateCommand && e.commandName == "Save")
                e.Use();
            else if (e.type == EventType.ExecuteCommand && e.commandName == "Save")
            {
                SaveAsset();
                e.Use();
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("New", EditorStyles.toolbarButton))
                OnNewClicked();

            if (GUILayout.Button("Open", EditorStyles.toolbarButton))
                OnOpenClicked();

            using (new EditorGUI.DisabledScope(_config == null))
            {
                if (GUILayout.Button("Save", EditorStyles.toolbarButton))
                    SaveAsset();
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button($"Theme: {_theme.ToDisplayString()}", EditorStyles.toolbarPopup))
                ShowThemeMenu();

            EditorGUILayout.EndHorizontal();
        }

        private void ShowThemeMenu()
        {
            var menu = new GenericMenu();
            foreach (MapTheme theme in Enum.GetValues(typeof(MapTheme)))
            {
                if (theme == MapTheme.Random)
                    continue;

                menu.AddItem(new GUIContent(theme.ToDisplayString()), theme == _theme, () =>
                {
                    _theme = theme;
                    Repaint();
                });
            }

            menu.ShowAsContext();
        }

        private void OnNewClicked()
        {
            var menu = new GenericMenu();
            foreach (MapSizePreset size in Enum.GetValues(typeof(MapSizePreset)))
            {
                var dimensions = _mapSizesConfig.MapSizes[size];
                menu.AddItem(new GUIContent($"{size.ToDisplayString()} ({dimensions.x}x{dimensions.y})"), false,
                    () => CreateNewMap(size));
            }

            menu.ShowAsContext();
        }

        private void CreateNewMap(MapSizePreset size)
        {
            if (hasUnsavedChanges && !ConfirmDiscardChanges())
                return;

            var path = EditorUtility.SaveFilePanelInProject("Save Custom Map", "CustomMap", "asset",
                "Choose where to save the new map config.", DefaultMapsFolder);

            if (string.IsNullOrEmpty(path))
                return;

            var config = CustomMapConfig.Create(size, _mapSizesConfig.MapSizes[size]);
            AssetDatabase.CreateAsset(config, path);

            LoadConfig(config);
        }

        private void OnOpenClicked()
        {
            var path = EditorUtility.OpenFilePanel("Open Custom Map", DefaultMapsFolder, "asset");
            if (string.IsNullOrEmpty(path))
                return;

            var relative = FileUtil.GetProjectRelativePath(path);
            if (string.IsNullOrEmpty(relative))
                relative = path.Replace('\\', '/');

            var config = AssetDatabase.LoadAssetAtPath<CustomMapConfig>(relative);
            if (config == null)
            {
                EditorUtility.DisplayDialog(WindowTitle,
                    $"Selected file is not a valid CustomMapConfig asset:\n{relative}", "OK");
                return;
            }

            if (hasUnsavedChanges && !ConfirmDiscardChanges())
                return;

            LoadConfig(config);
        }

        // Binds the window to an asset through an in-memory working copy; the asset
        // itself stays untouched until Save.
        private void LoadConfig(CustomMapConfig config)
        {
            DropWorkingCopy();

            _config = config;
            _workingCopy = Instantiate(config);
            _workingCopy.name = config.name; // strip the "(Clone)" suffix

            hasUnsavedChanges = false;
            Repaint();
        }

        private void SaveAsset()
        {
            if (_config == null || _workingCopy == null)
                return;

            // Flush the copy into the asset, then clear the copy's undo stack so undo
            // cannot reach across the save boundary and resurrect pre-save states.
            EditorUtility.CopySerialized(_workingCopy, _config);
            EditorUtility.SetDirty(_config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Undo.ClearUndo(_workingCopy);
            hasUnsavedChanges = false;
        }

        private void DrawHeader()
        {
            var dimensions = _mapSizesConfig.MapSizes[_workingCopy.Size];

            EditorGUILayout.BeginHorizontal();

            // Size is immutable after creation; display it read-only.
            EditorGUILayout.LabelField($"Size: {dimensions.x} x {dimensions.y} ({_workingCopy.Size.ToDisplayString()})",
                EditorStyles.boldLabel, GUILayout.Width(220f));

            GUILayout.FlexibleSpace();

            EditorGUILayout.LabelField("Tile:", GUILayout.Width(36f));
            foreach (MapTile tile in Enum.GetValues(typeof(MapTile)))
            {
                if (GUILayout.Toggle(_brush == tile, tile.ToDisplayString(), EditorStyles.toolbarButton,
                        GUILayout.Width(90f)) && _brush != tile)
                    _brush = tile;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawGrid()
        {
            var dimensions = _mapSizesConfig.MapSizes[_workingCopy.Size];

            var area = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            var cellSize = Mathf.Floor(Mathf.Min(area.width / dimensions.x, area.height / dimensions.y));
            if (cellSize < 4f)
                return;

            var gridSize = new Vector2(cellSize * dimensions.x, cellSize * dimensions.y);
            var origin = new Vector2(area.x + (area.width - gridSize.x) * 0.5f,
                area.y + (area.height - gridSize.y) * 0.5f);
            var gridRect = new Rect(origin, gridSize);

            var e = Event.current;
            var mouseCell = GetCellAt(e.mousePosition, gridRect, dimensions, cellSize);

            HandleGridInput(e, gridRect, dimensions, cellSize, mouseCell);

            if (e.type == EventType.Repaint)
                DrawGridContents(gridRect, dimensions, cellSize, mouseCell);
        }

        private void HandleGridInput(Event e, Rect gridRect, int2 dimensions, float cellSize, int2 mouseCell)
        {
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0 || e.button == 1)
                    {
                        var cell = GetCellAt(e.mousePosition, gridRect, dimensions, cellSize);
                        if (cell.x >= 0)
                        {
                            BeginStroke();
                            PaintCell(cell, erase: e.button == 1);
                            e.Use();
                        }
                    }

                    break;

                case EventType.MouseDrag:
                    if (_painting && (e.button == 0 || e.button == 1))
                    {
                        // No grid containment check: keep painting while dragging back in.
                        var cell = GetCellAt(e.mousePosition, gridRect, dimensions, cellSize);
                        if (cell.x >= 0)
                        {
                            PaintCell(cell, erase: e.button == 1);
                            e.Use();
                        }
                    }

                    break;

                case EventType.MouseUp:
                    if (_painting)
                    {
                        EndStroke();
                        if (mouseCell.x >= 0)
                            e.Use();
                    }

                    break;

                case EventType.MouseLeaveWindow:
                    if (_painting)
                        EndStroke();
                    break;

                case EventType.MouseMove:
                    Repaint();
                    break;
            }
        }

        private void BeginStroke()
        {
            _painting = true;

            Undo.IncrementCurrentGroup();
            _undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Paint Map Tiles");
        }

        // Undo/redo from the main menu (Ctrl+Z) does not raise any GUI events for
        // this window; repaint explicitly so the grid reflects the restored state.
        private void OnUndoRedoPerformed()
        {
            Repaint();
        }

        private void EndStroke()
        {
            _painting = false;

            if (_undoGroup >= 0)
                Undo.CollapseUndoOperations(_undoGroup);

            _undoGroup = -1;
        }

        private void PaintCell(int2 cell, bool erase)
        {
            var dimensions = _mapSizesConfig.MapSizes[_workingCopy.Size];
            var index = cell.y * dimensions.x + cell.x;

            if (index < 0 || index >= _workingCopy.Cells.Length)
                return;

            var tile = erase ? MapTile.Background1 : _brush;
            if (_workingCopy.Cells[index] == tile)
                return;

            // Record before every modification (including each MouseDrag event);
            // CollapseUndoOperations on MouseUp merges all deltas into one step.
            // Only the working copy is touched — the asset stays clean on disk.
            Undo.RecordObject(_workingCopy, "Paint Map Tiles");

            _workingCopy.Cells[index] = tile;
            hasUnsavedChanges = true;
        }

        private void DrawGridContents(Rect gridRect, int2 dimensions, float cellSize, int2 mouseCell)
        {
            var themeRow = math.clamp((int)_theme - 1, 0, _tileSetConfig.Rows - 1);

            EditorGUI.DrawRect(
                new Rect(gridRect.x - 1f, gridRect.y - 1f, gridRect.width + 2f, gridRect.height + 2f),
                new Color(0.1f, 0.1f, 0.1f, 1f));

            for (var y = 0; y < dimensions.y; y++)
            {
                // GUI y grows downward, level data y grows upward (world Y) — flip it.
                var guiY = dimensions.y - 1 - y;
                for (var x = 0; x < dimensions.x; x++)
                {
                    var cellRect = new Rect(gridRect.x + x * cellSize, gridRect.y + guiY * cellSize, cellSize,
                        cellSize);
                    DrawTile(cellRect, _workingCopy.Cells[y * dimensions.x + x], themeRow);
                }
            }

            DrawGridLines(gridRect, dimensions, cellSize);

            if (mouseCell.x >= 0)
            {
                var hoverGuiY = dimensions.y - 1 - mouseCell.y;
                EditorGUI.DrawRect(
                    new Rect(gridRect.x + mouseCell.x * cellSize, gridRect.y + hoverGuiY * cellSize, cellSize,
                        cellSize),
                    new Color(1f, 1f, 1f, 0.2f));
            }
        }

        private static void DrawGridLines(Rect gridRect, int2 dimensions, float cellSize)
        {
            var lineColor = new Color(0f, 0f, 0f, 0.25f);

            for (var x = 1; x < dimensions.x; x++)
                EditorGUI.DrawRect(new Rect(gridRect.x + x * cellSize - 0.5f, gridRect.y, 1f, gridRect.height),
                    lineColor);

            for (var y = 1; y < dimensions.y; y++)
                EditorGUI.DrawRect(new Rect(gridRect.x, gridRect.y + y * cellSize - 0.5f, gridRect.width, 1f),
                    lineColor);
        }

        private void DrawTile(Rect rect, MapTile tile, int themeRow)
        {
            if (_tileSetConfig.Atlas == null)
            {
                EditorGUI.DrawRect(rect, GetFallbackColor(tile));
                return;
            }

            // Same atlas layout as the runtime presentation: row = theme (Random = 0
            // is skipped, Hyrule = row 0), columns 0..2 = Background1..3, 3..5 = Wall1..3.
            var columns = _tileSetConfig.Columns;
            var rows = _tileSetConfig.Rows;

            var column = (int)tile;
            var rowBottom = rows - 1 - themeRow;

            var u0 = column / (float)columns;
            var u1 = (column + 1) / (float)columns;
            var v0 = rowBottom / (float)rows;
            var v1 = (rowBottom + 1) / (float)rows;

            GUI.DrawTextureWithTexCoords(rect, _tileSetConfig.Atlas, new Rect(u0, v0, u1 - u0, v1 - v0), true);
        }

        private static Color GetFallbackColor(MapTile tile)
        {
            return tile switch
            {
                MapTile.Background1 => new Color32(255, 255, 255, 255),
                MapTile.Background2 => new Color32(192, 192, 192, 255),
                MapTile.Background3 => new Color32(128, 128, 128, 255),
                MapTile.Wall1 => new Color32(0, 0, 255, 255),
                MapTile.Wall2 => new Color32(0, 0, 192, 255),
                MapTile.Wall3 => new Color32(0, 0, 128, 255),
                _ => Color.magenta
            };
        }

        private void DrawStatusBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            EditorGUILayout.LabelField(
                $"Brush: {_brush.ToDisplayString()}   |   LMB paint, RMB erase ({MapTile.Background1})",
                EditorStyles.miniLabel, GUILayout.Width(300f));

            GUILayout.FlexibleSpace();

            EditorGUILayout.LabelField(_config.name, EditorStyles.miniLabel);

            EditorGUILayout.EndHorizontal();
        }

        private static int2 GetCellAt(Vector2 position, Rect gridRect, int2 dimensions, float cellSize)
        {
            if (!gridRect.Contains(position))
                return new int2(-1, -1);

            var x = (int)((position.x - gridRect.x) / cellSize);

            // GUI y grows downward, level data y grows upward (world Y) — flip it:
            // the grid's top edge (gridRect.yMax) is the last data row.
            var y = (int)((gridRect.yMax - position.y) / cellSize);

            if (x < 0 || y < 0 || x >= dimensions.x || y >= dimensions.y)
                return new int2(-1, -1);

            return new int2(x, y);
        }

        private void DropWorkingCopy()
        {
            if (_workingCopy != null)
                Undo.ClearUndo(_workingCopy);

            _workingCopy = null;
            _config = null;
        }
    }
}