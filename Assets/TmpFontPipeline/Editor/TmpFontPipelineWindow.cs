using System.IO;
using System.Text;
using TmpFontPipeline;
using UnityEditor;
using UnityEngine;

namespace TmpFontPipeline.Editor
{
    // Extract + Apply를 한 창에서 설정·실행합니다.
    public sealed class TmpFontPipelineWindow : EditorWindow
    {
        private static readonly string[] TabLabels = { "Extract", "Apply", "Help" };

        private Vector2 _scroll;
        private int _selectedTab;

        private string _jsonSearchPath;
        private string _outputPath;
        private FontAtlasApplyProfile _profile;
        private SerializedObject _serializedProfile;
        private string _lastActionMessage = string.Empty;

        [MenuItem("Tmp Font Pipeline/Open Window", priority = 0)]
        public static void ShowWindow()
        {
            GetWindow<TmpFontPipelineWindow>("TMP Font Pipeline");
        }

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(_jsonSearchPath))
            {
                _jsonSearchPath = StringJsonCharacterExtractor.DefaultJsonSearchPath;
            }

            if (string.IsNullOrEmpty(_outputPath))
            {
                _outputPath = StringJsonCharacterExtractor.DefaultOutputPath;
            }

            BindProfile(FontAtlasApplySettings.LoadActiveProfile());
        }

        private void OnGUI()
        {
            _selectedTab = GUILayout.Toolbar(_selectedTab, TabLabels);
            EditorGUILayout.Space(6f);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            switch (_selectedTab)
            {
                case 0:
                    DrawExtractSection();
                    break;
                case 1:
                    DrawApplySection();
                    break;
                default:
                    DrawHelpSection();
                    break;
            }

            EditorGUILayout.Space(8f);
            DrawLastAction();
            EditorGUILayout.EndScrollView();
        }

        private void DrawExtractSection()
        {
            _jsonSearchPath = EditorGUILayout.TextField("JSON Folder", _jsonSearchPath);
            DrawPathPingRow(_jsonSearchPath);

            _outputPath = EditorGUILayout.TextField("Output Folder", _outputPath);
            DrawPathPingRow(_outputPath);

            JsonParserMode parserMode = (JsonParserMode)EditorGUILayout.EnumPopup(
                "JSON Parser",
                JsonParserSettings.Mode);
            if (parserMode != JsonParserSettings.Mode)
            {
                JsonParserSettings.Mode = parserMode;
            }

            DrawExtractPreview();

            bool canExtract = !EditorApplication.isPlaying && DirectoryExistsAssetPath(_jsonSearchPath);
            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Exit Play Mode to extract characters.", MessageType.Warning);
            }
            else if (!DirectoryExistsAssetPath(_jsonSearchPath))
            {
                EditorGUILayout.HelpBox($"JSON folder not found: {_jsonSearchPath}", MessageType.Warning);
            }

            using (new EditorGUI.DisabledScope(!canExtract))
            {
                if (GUILayout.Button("Extract Unique Characters", GUILayout.Height(28f)))
                {
                    StringJsonCharacterExtractor.Extract(_jsonSearchPath, _outputPath);
                    _lastActionMessage = "Extract completed. See Console for details.";
                }
            }
        }

        private void DrawExtractPreview()
        {
            if (!DirectoryExistsAssetPath(_jsonSearchPath))
            {
                return;
            }

            string absoluteDir = AssetPathToAbsolute(_jsonSearchPath);
            string[] jsonFiles = Directory.GetFiles(absoluteDir, "String*.json", SearchOption.TopDirectoryOnly);
            int dialogueCount = 0;
            for (int i = 0; i < jsonFiles.Length; i++)
            {
                string name = Path.GetFileNameWithoutExtension(jsonFiles[i]);
                if (name.StartsWith("StringDialogue", System.StringComparison.OrdinalIgnoreCase))
                {
                    dialogueCount++;
                }
            }

            EditorGUILayout.HelpBox(
                $"Found {jsonFiles.Length} String*.json file(s), {dialogueCount} dialogue.",
                MessageType.Info);
        }

        private void DrawApplySection()
        {
            FontAtlasApplyProfile nextProfile = (FontAtlasApplyProfile)EditorGUILayout.ObjectField(
                "Apply Profile",
                _profile,
                typeof(FontAtlasApplyProfile),
                false);
            if (nextProfile != _profile)
            {
                BindProfile(nextProfile);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Use Demo Profile"))
                {
                    FontAtlasApplySettings.ActiveProfilePath = FontAtlasApplySettings.DemoProfilePath;
                    BindProfile(FontAtlasApplySettings.LoadActiveProfile());
                }

                using (new EditorGUI.DisabledScope(_profile == null))
                {
                    if (GUILayout.Button("Ping"))
                    {
                        EditorGUIUtility.PingObject(_profile);
                    }
                }
            }

            if (_profile == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign a Font Atlas Apply Profile, or use Create Demo Assets / Use Demo Profile.",
                    MessageType.Warning);
            }
            else
            {
                EditorGUILayout.LabelField("Generated Folder", _profile.GeneratedFolder);
                if (!PathsEqual(_outputPath, _profile.GeneratedFolder))
                {
                    EditorGUILayout.HelpBox(
                        $"Extract output ({_outputPath}) differs from profile Generated Folder ({_profile.GeneratedFolder}).",
                        MessageType.Warning);
                }

                DrawEntriesTable();
            }

            bool canApply = !EditorApplication.isPlaying && _profile != null;
            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Exit Play Mode to apply characters.", MessageType.Warning);
            }

            using (new EditorGUI.DisabledScope(!canApply))
            {
                if (GUILayout.Button("Apply Generated Characters", GUILayout.Height(28f)))
                {
                    FlushProfileEdits();
                    FontAtlasApplier.Apply(_profile);
                    _lastActionMessage = "Apply completed. See Console for details.";
                }
            }
        }

        private void DrawEntriesTable()
        {
            if (_serializedProfile == null || _profile == null)
            {
                return;
            }

            _serializedProfile.Update();
            SerializedProperty entriesProp = _serializedProfile.FindProperty("_entries");
            if (entriesProp == null || !entriesProp.isArray)
            {
                EditorGUILayout.HelpBox("Profile entries property not found.", MessageType.Error);
                return;
            }

            EditorGUILayout.LabelField("Entries", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Enable All"))
                {
                    SetAllEntriesEnabled(entriesProp, true);
                }

                if (GUILayout.Button("Disable All"))
                {
                    SetAllEntriesEnabled(entriesProp, false);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("On", GUILayout.Width(28f));
                EditorGUILayout.LabelField("Bucket", GUILayout.Width(120f));
                EditorGUILayout.LabelField("Role", GUILayout.Width(72f));
                EditorGUILayout.LabelField("Font");
                EditorGUILayout.LabelField("Txt", GUILayout.Width(36f));
                EditorGUILayout.LabelField("Chars", GUILayout.Width(52f));
            }

            for (int i = 0; i < entriesProp.arraySize; i++)
            {
                SerializedProperty entryProp = entriesProp.GetArrayElementAtIndex(i);
                SerializedProperty enabledProp = entryProp.FindPropertyRelative("Enabled");
                SerializedProperty bucketProp = entryProp.FindPropertyRelative("Bucket");
                SerializedProperty roleProp = entryProp.FindPropertyRelative("Role");
                SerializedProperty fontProp = entryProp.FindPropertyRelative("FontAsset");

                FontAtlasApplyEntry runtimeEntry = i < _profile.Entries.Count ? _profile.Entries[i] : null;
                string txtPath = runtimeEntry != null ? _profile.GetResolvedPath(runtimeEntry) : string.Empty;
                bool txtExists = !string.IsNullOrEmpty(txtPath) && File.Exists(AssetPathToAbsolute(txtPath));
                int charCount = txtExists ? CountCharsInGeneratedFile(txtPath) : 0;

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(enabledProp, GUIContent.none, GUILayout.Width(28f));
                    EditorGUILayout.LabelField(bucketProp.enumDisplayNames[bucketProp.enumValueIndex], GUILayout.Width(120f));
                    EditorGUILayout.LabelField(roleProp.enumDisplayNames[roleProp.enumValueIndex], GUILayout.Width(72f));
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.ObjectField(fontProp.objectReferenceValue, typeof(Object), false);
                    }

                    EditorGUILayout.LabelField(txtExists ? "✓" : "✗", GUILayout.Width(36f));
                    EditorGUILayout.LabelField(txtExists ? charCount.ToString() : "-", GUILayout.Width(52f));
                }
            }

            if (_serializedProfile.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(_profile);
            }
        }

        private void DrawHelpSection()
        {
            EditorGUILayout.HelpBox(
                "1. Extract String*.json → unique_chars_*.txt\n" +
                "2. Toggle Enabled entries on the Apply Profile\n" +
                "3. Apply to Static TMP font assets (atlas 2048)\n" +
                "Detail logs are written to the Console.",
                MessageType.None);

            bool demoProfileExists = AssetDatabase.LoadAssetAtPath<FontAtlasApplyProfile>(
                FontAtlasApplySettings.DemoProfilePath) != null;

            if (demoProfileExists)
            {
                EditorGUILayout.HelpBox(
                    "Demo profile already exists at Assets/Demo/FontAtlasApplyProfile.asset.\n" +
                    "Resync Demo Assets will overwrite Apply Profile and Font Role Catalog from demo bindings.\n" +
                    "Manual edits (Enabled, font refs, entry count) will be reset — Enabled becomes all true.",
                    MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Demo profile is missing.\n" +
                    "Create Demo Assets will create FontAtlasApplyProfile and FontRoleCatalog under Assets/Demo,\n" +
                    "seed language × Ui/Dialogue entries, and set the demo profile as active.",
                    MessageType.Info);
            }

            string buttonLabel = demoProfileExists ? "Resync Demo Assets" : "Create Demo Assets";
            if (GUILayout.Button(buttonLabel))
            {
                DemoFontProfileBootstrap.CreateDemoAssets();
                FontAtlasApplySettings.ActiveProfilePath = FontAtlasApplySettings.DemoProfilePath;
                BindProfile(FontAtlasApplySettings.LoadActiveProfile());
                _lastActionMessage = demoProfileExists
                    ? "Demo assets resynced. Manual edits were overwritten."
                    : "Demo assets created.";
            }
        }

        private void DrawLastAction()
        {
            if (string.IsNullOrEmpty(_lastActionMessage))
            {
                return;
            }

            EditorGUILayout.HelpBox(_lastActionMessage, MessageType.Info);
        }

        private void BindProfile(FontAtlasApplyProfile profile)
        {
            _profile = profile;
            if (_profile != null)
            {
                string path = AssetDatabase.GetAssetPath(_profile);
                if (!string.IsNullOrEmpty(path))
                {
                    FontAtlasApplySettings.ActiveProfilePath = path;
                }

                _serializedProfile = new SerializedObject(_profile);
            }
            else
            {
                _serializedProfile = null;
            }
        }

        private void FlushProfileEdits()
        {
            if (_serializedProfile == null || _profile == null)
            {
                return;
            }

            if (_serializedProfile.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(_profile);
            }
        }

        // Apply Profile 엔트리 Enabled를 일괄 설정합니다.
        private void SetAllEntriesEnabled(SerializedProperty entriesProp, bool enabled)
        {
            for (int i = 0; i < entriesProp.arraySize; i++)
            {
                SerializedProperty enabledProp = entriesProp.GetArrayElementAtIndex(i).FindPropertyRelative("Enabled");
                if (enabledProp != null)
                {
                    enabledProp.boolValue = enabled;
                }
            }

            if (_serializedProfile.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(_profile);
            }
        }

        private static void DrawPathPingRow(string assetPath)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(assetPath)))
                {
                    if (GUILayout.Button("Ping Folder", GUILayout.Width(100f)))
                    {
                        Object folder = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                        if (folder != null)
                        {
                            EditorGUIUtility.PingObject(folder);
                        }
                    }
                }
            }
        }

        private static bool DirectoryExistsAssetPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            return Directory.Exists(AssetPathToAbsolute(assetPath));
        }

        private static string AssetPathToAbsolute(string assetPath)
        {
            string normalized = assetPath.Replace('\\', '/').TrimEnd('/');
            if (normalized.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("Assets", System.StringComparison.OrdinalIgnoreCase))
            {
                string projectRoot = Path.GetDirectoryName(Application.dataPath);
                return Path.GetFullPath(Path.Combine(projectRoot ?? string.Empty, normalized));
            }

            return Path.GetFullPath(normalized);
        }

        private static bool PathsEqual(string left, string right)
        {
            if (string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right))
            {
                return false;
            }

            return string.Equals(
                left.Replace('\\', '/').TrimEnd('/'),
                right.Replace('\\', '/').TrimEnd('/'),
                System.StringComparison.OrdinalIgnoreCase);
        }

        private static int CountCharsInGeneratedFile(string assetPath)
        {
            try
            {
                string content = File.ReadAllText(AssetPathToAbsolute(assetPath), Encoding.UTF8);
                return content.Length;
            }
            catch
            {
                return 0;
            }
        }
    }
}
