using TanksKombat2.Shared;
using UnityEditor;
using UnityEngine;

namespace TanksKombat2.MapEditor
{
    // Read-only inspector for CustomMapConfig assets.
    [CustomEditor(typeof(CustomMapConfig))]
    public class CustomMapConfigInspector : Editor
    {
        private MapSizeConfig _mapSizesConfig;

        public override void OnInspectorGUI()
        {
            var config = (CustomMapConfig)target;

            if (_mapSizesConfig == null)
                _mapSizesConfig = AssetDatabase.LoadAssetAtPath<MapSizeConfig>(MapSizeConfig.DefaultAssetPath);

            if (_mapSizesConfig == null)
            {
                EditorGUILayout.HelpBox(
                    $"MapSizeConfig asset is missing (expected at '{MapSizeConfig.DefaultAssetPath}').",
                    MessageType.Error);
                return;
            }

            var expected = _mapSizesConfig.MapSizes[config.Size];
            var cells = config.Cells;

            EditorGUILayout.LabelField("Size", expected.x >= 0
                ? $"{expected.x} x {expected.y} ({config.Size})"
                : $"<invalid> ({config.Size})");

            EditorGUILayout.LabelField("Cells", cells == null ? "<null>" : $"{cells.Length}");

            if (cells == null || cells.Length != expected.x * expected.y)
                EditorGUILayout.HelpBox("Cell count does not match the declared map size.", MessageType.Error);

            EditorGUILayout.HelpBox("Double-click the asset or use Tools \u2192 Map Editor to edit it.",
                MessageType.Info);

            if (GUILayout.Button("Open Map Editor"))
                CustomMapEditorWindow.Open(config);
        }
    }
}