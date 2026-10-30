using System.Collections.Generic;
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
        private const string PrefsJsonFolderKey = "TmpFontPipeline.JsonSearchPath";
        private const string PrefsOutputFolderKey = "TmpFontPipeline.OutputPath";

        private static readonly string[] TabLabels = { "Extract", "Apply", "Help" };

        private Vector2 _scroll;
        private int _selectedTab;

        private string _jsonSearchPath;
        private string _outputPath;
        private FontAtlasApplyProfile _profile;
        private SerializedObject _serializedProfile;
        private string _lastActionMessage = string.Empty;
        private MessageType _lastActionMessageType = MessageType.Info;

        [MenuItem("Tmp Font Pipeline/Open Window", priority = 0)]
        public static void ShowWindow()
        {
            GetWindow<TmpFontPipelineWindow>("TMP Font Pipeline");
        }

        private void OnEnable()
        {
            _jsonSearchPath = EditorPrefs.GetString(PrefsJsonFolderKey, string.Empty);
            _outputPath = EditorPrefs.GetString(PrefsOutputFolderKey, string.Empty);
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
            EditorGUI.BeginChangeCheck();
            _jsonSearchPath = EditorGUILayout.TextField("JSON Folder", _jsonSearchPath);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetString(PrefsJsonFolderKey, _jsonSearchPath ?? string.Empty);
            }

            DrawPathPingRow(_jsonSearchPath);

            EditorGUI.BeginChangeCheck();
            _outputPath = EditorGUILayout.TextField("Output Folder", _outputPath);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetString(PrefsOutputFolderKey, _outputPath ?? string.Empty);
            }

            DrawPathPingRow(_outputPath);

            JsonParserMode parserMode = (JsonParserMode)EditorGUILayout.EnumPopup(
                "JSON Parser",
                JsonParserSettings.Mode);
            if (parserMode != JsonParserSettings.Mode)
            {
                JsonParserSettings.Mode = parserMode;
            }

            EuropeanExtractMode europeanMode = (EuropeanExtractMode)EditorGUILayout.EnumPopup(
                "European Extract",
                EuropeanExtractSettings.Mode);
            if (europeanMode != EuropeanExtractSettings.Mode)
            {
                EuropeanExtractSettings.Mode = europeanMode;
            }

            DrawExtractPreview();

            bool pathsConfigured = !string.IsNullOrWhiteSpace(_jsonSearchPath)
                && !string.IsNullOrWhiteSpace(_outputPath);
            bool jsonFolderExists = DirectoryExistsAssetPath(_jsonSearchPath);
            bool hasStringJsonInFolder = jsonFolderExists && HasStringJsonFiles(_jsonSearchPath);
            bool canExtract = !EditorApplication.isPlaying
                && pathsConfigured
                && jsonFolderExists
                && hasStringJsonInFolder;
            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Exit Play Mode to extract characters.", MessageType.Warning);
            }
            else if (!pathsConfigured)
            {
                EditorGUILayout.HelpBox("Set JSON Folder and Output Folder (saved in EditorPrefs).", MessageType.Warning);
            }
            else if (!jsonFolderExists)
            {
                EditorGUILayout.HelpBox($"JSON folder not found: {_jsonSearchPath}", MessageType.Warning);
            }
            else if (!hasStringJsonInFolder)
            {
                EditorGUILayout.HelpBox(
                    $"No String*.json files in folder (top level only): {_jsonSearchPath}",
                    MessageType.Warning);
            }

            using (new EditorGUI.DisabledScope(!canExtract))
            {
                if (GUILayout.Button("Extract Unique Characters", GUILayout.Height(28f)))
                {
                    bool ok = StringJsonCharacterExtractor.Extract(_jsonSearchPath, _outputPath);
                    SetLastAction(
                        ok
                            ? "Extract completed. See Console for details."
                            : "Extract failed. See Console for errors.",
                        ok ? MessageType.Info : MessageType.Error);
                }
            }
        }

        private static bool HasStringJsonFiles(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath) || !DirectoryExistsAssetPath(assetPath))
            {
                return false;
            }

            string absoluteDir = AssetPathToAbsolute(assetPath);
            return Directory.GetFiles(absoluteDir, "String*.json", SearchOption.TopDirectoryOnly).Length > 0;
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
                using (new EditorGUI.DisabledScope(_profile == null))
                {
                    if (GUILayout.Button("Ping Profile"))
                    {
                        EditorGUIUtility.PingObject(_profile);
                    }
                }
            }

            if (_profile == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign a Font Atlas Apply Profile asset.",
                    MessageType.Warning);
            }
            else
            {
                EditorGUILayout.LabelField("Generated Folder", _profile.GeneratedFolder);
                if (string.IsNullOrWhiteSpace(_profile.GeneratedFolder))
                {
                    EditorGUILayout.HelpBox(
                        "Set Generated Folder on the Apply Profile asset (Inspector) before Apply.",
                        MessageType.Warning);
                }

                if (!PathsEqual(_outputPath, _profile.GeneratedFolder))
                {
                    EditorGUILayout.HelpBox(
                        $"Extract output ({_outputPath}) differs from profile Generated Folder ({_profile.GeneratedFolder}).",
                        MessageType.Warning);
                }

                DrawEntriesTable();
            }

            bool profileReady = _profile != null && !string.IsNullOrWhiteSpace(_profile.GeneratedFolder);
            bool canApply = !EditorApplication.isPlaying && profileReady;
            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Exit Play Mode to apply characters.", MessageType.Warning);
            }
            else if (_profile != null && string.IsNullOrWhiteSpace(_profile.GeneratedFolder))
            {
                EditorGUILayout.HelpBox("Generated Folder on the profile is empty.", MessageType.Warning);
            }

            using (new EditorGUI.DisabledScope(!canApply))
            {
                if (GUILayout.Button("Apply Generated Characters", GUILayout.Height(28f)))
                {
                    FlushProfileEdits();
                    bool ok = FontAtlasApplier.Apply(_profile);
                    SetLastAction(
                        ok
                            ? "Apply completed. See Console for details."
                            : "Apply failed or had failed entries. See Console for details.",
                        ok ? MessageType.Info : MessageType.Error);
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

            DrawDuplicateFontAssetWarnings();
        }

        private void DrawDuplicateFontAssetWarnings()
        {
            if (_profile == null)
            {
                return;
            }

            IReadOnlyList<string> warnings =
                FontAtlasApplyDuplicateWarnings.GetEnabledDuplicateFontAssetWarnings(_profile.Entries);
            if (warnings.Count == 0)
            {
                return;
            }

            var body = new StringBuilder();
            _ = body.AppendLine(
                "Enabled entries reference the same TMP_FontAsset more than once (Apply still runs; see Console):");
            for (int i = 0; i < warnings.Count; i++)
            {
                _ = body.AppendLine($"• {warnings[i]}");
            }

            EditorGUILayout.HelpBox(body.ToString(), MessageType.Warning);
        }

        private void DrawHelpSection()
        {
            EditorGUILayout.HelpBox(
                "1. Set JSON Folder and Output Folder on Extract (paths persist in EditorPrefs).\n" +
                "2. Extract String*.json → unique_chars_*.txt (European Extract: PerLanguage default, or MergedEuropean)\n" +
                "3. Assign Apply Profile, toggle Enabled entries, Apply to Static TMP fonts (atlas 2048)\n" +
                "MergedEuropean: use FontAtlasBucket.European on Apply entries (same txt → multiple fonts OK).\n" +
                "Duplicate Enabled entries pointing at the same TMP_FontAsset show a warning (Apply tab + Console).\n" +
                "Detail logs are written to the Console.",
                MessageType.None);
        }

        private void DrawLastAction()
        {
            if (string.IsNullOrEmpty(_lastActionMessage))
            {
                return;
            }

            EditorGUILayout.HelpBox(_lastActionMessage, _lastActionMessageType);
        }

        private void SetLastAction(string message, MessageType type)
        {
            _lastActionMessage = message;
            _lastActionMessageType = type;
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
