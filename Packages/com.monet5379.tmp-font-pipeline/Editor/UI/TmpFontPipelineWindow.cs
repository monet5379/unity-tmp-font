using System.Collections.Generic;
using System.IO;
using System.Text;
using TmpFontPipeline;
using UnityEditor;
using UnityEngine;

namespace TmpFontPipeline.Editor
{
    public sealed class TmpFontPipelineWindow : EditorWindow
    {
        private static readonly string[] TabLabels = { "추출", "적용", "도움말" };

        private Vector2 _scroll;
        private int _selectedTab;

        private string _jsonSearchPath;
        private string _outputPath;
        private FontAtlasApplyProfile _profile;
        private SerializedObject _serializedProfile;
        private string _lastActionMessage = string.Empty;
        private MessageType _lastActionMessageType = MessageType.Info;

        [MenuItem("TMP 폰트 파이프라인/창 열기", priority = 0)]
        public static void ShowWindow()
        {
            GetWindow<TmpFontPipelineWindow>("TMP 폰트 파이프라인");
        }

        private void OnEnable()
        {
            _jsonSearchPath = ExtractPathSettings.JsonSearchPath;
            _outputPath = ExtractPathSettings.OutputPath;
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
            _jsonSearchPath = EditorGUILayout.TextField("JSON 폴더", _jsonSearchPath);
            if (EditorGUI.EndChangeCheck())
            {
                ExtractPathSettings.JsonSearchPath = _jsonSearchPath;
            }

            DrawPathPingRow(_jsonSearchPath);

            EditorGUI.BeginChangeCheck();
            _outputPath = EditorGUILayout.TextField("출력 폴더", _outputPath);
            if (EditorGUI.EndChangeCheck())
            {
                ExtractPathSettings.OutputPath = _outputPath;
            }

            DrawPathPingRow(_outputPath);

            JsonParserMode parserMode = (JsonParserMode)EditorGUILayout.EnumPopup(
                "JSON 파서",
                JsonParserSettings.Mode);
            if (parserMode != JsonParserSettings.Mode)
            {
                JsonParserSettings.Mode = parserMode;
            }

            EuropeanExtractMode europeanMode = (EuropeanExtractMode)EditorGUILayout.EnumPopup(
                "유럽어 추출",
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
                EditorGUILayout.HelpBox("추출하려면 Play 모드를 종료하세요.", MessageType.Warning);
            }
            else if (!pathsConfigured)
            {
                EditorGUILayout.HelpBox("JSON 폴더와 출력 폴더를 지정하세요 (EditorPrefs에 저장됩니다).", MessageType.Warning);
            }
            else if (!jsonFolderExists)
            {
                EditorGUILayout.HelpBox($"JSON 폴더를 찾을 수 없습니다: {_jsonSearchPath}", MessageType.Warning);
            }
            else if (!hasStringJsonInFolder)
            {
                EditorGUILayout.HelpBox(
                    $"폴더에 String*.json 파일이 없습니다 (최상위만 검색): {_jsonSearchPath}",
                    MessageType.Warning);
            }

            using (new EditorGUI.DisabledScope(!canExtract))
            {
                if (GUILayout.Button("고유 문자 추출", GUILayout.Height(28f)))
                {
                    bool ok = StringJsonCharacterExtractor.Extract(_jsonSearchPath, _outputPath);
                    SetLastAction(
                        ok
                            ? "추출이 완료되었습니다. 자세한 내용은 Console을 확인하세요."
                            : "추출에 실패했습니다. Console의 오류를 확인하세요.",
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
                $"String*.json {jsonFiles.Length}개, 대화용 {dialogueCount}개를 찾았습니다.",
                MessageType.Info);
        }

        private void DrawApplySection()
        {
            FontAtlasApplyProfile nextProfile = (FontAtlasApplyProfile)EditorGUILayout.ObjectField(
                "Apply 프로필",
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
                    if (GUILayout.Button("프로필 찾기"))
                    {
                        EditorGUIUtility.PingObject(_profile);
                    }
                }
            }

            if (_profile == null)
            {
                EditorGUILayout.HelpBox(
                    "폰트 아틀라스 Apply 프로필 에셋을 지정하세요.",
                    MessageType.Warning);
            }
            else
            {
                EditorGUILayout.LabelField("Generated 폴더", _profile.GeneratedFolder);
                if (string.IsNullOrWhiteSpace(_profile.GeneratedFolder))
                {
                    EditorGUILayout.HelpBox(
                        "Apply 전에 Apply 프로필(Inspector)에서 Generated 폴더를 설정하세요.",
                        MessageType.Warning);
                }

                if (!PathsEqual(_outputPath, _profile.GeneratedFolder))
                {
                    EditorGUILayout.HelpBox(
                        $"추출 출력 폴더({_outputPath})와 프로필 Generated 폴더({_profile.GeneratedFolder})가 다릅니다.",
                        MessageType.Warning);
                }

                DrawEntriesTable();
            }

            bool profileReady = _profile != null && !string.IsNullOrWhiteSpace(_profile.GeneratedFolder);
            bool canApply = !EditorApplication.isPlaying && profileReady;
            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("적용하려면 Play 모드를 종료하세요.", MessageType.Warning);
            }
            else if (_profile != null && string.IsNullOrWhiteSpace(_profile.GeneratedFolder))
            {
                EditorGUILayout.HelpBox("프로필의 Generated 폴더가 비어 있습니다.", MessageType.Warning);
            }

            using (new EditorGUI.DisabledScope(!canApply))
            {
                if (GUILayout.Button("Generated 문자 적용", GUILayout.Height(28f)))
                {
                    FlushProfileEdits();
                    bool ok = FontAtlasApplier.Apply(_profile);
                    SetLastAction(
                        ok
                            ? "적용이 완료되었습니다. 자세한 내용은 Console을 확인하세요."
                            : "적용에 실패했거나 일부 항목이 실패했습니다. Console을 확인하세요.",
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
                EditorGUILayout.HelpBox("프로필 엔트리 속성을 찾을 수 없습니다.", MessageType.Error);
                return;
            }

            EditorGUILayout.LabelField("엔트리", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("전체 켜기"))
                {
                    SetAllEntriesEnabled(entriesProp, true);
                }

                if (GUILayout.Button("전체 끄기"))
                {
                    SetAllEntriesEnabled(entriesProp, false);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("켜", GUILayout.Width(28f));
                EditorGUILayout.LabelField("버킷", GUILayout.Width(120f));
                EditorGUILayout.LabelField("역할", GUILayout.Width(72f));
                EditorGUILayout.LabelField("폰트");
                EditorGUILayout.LabelField("Txt", GUILayout.Width(36f));
                EditorGUILayout.LabelField("글자", GUILayout.Width(52f));
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
                "켜진 엔트리가 같은 TMP_FontAsset을 두 번 이상 참조합니다 (Apply는 계속 실행됩니다. Console 참고):");
            for (int i = 0; i < warnings.Count; i++)
            {
                _ = body.AppendLine($"• {warnings[i]}");
            }

            EditorGUILayout.HelpBox(body.ToString(), MessageType.Warning);
        }

        private void DrawHelpSection()
        {
            EditorGUILayout.HelpBox(
                "1. 추출 탭에서 JSON 폴더·출력 폴더를 지정하세요 (경로는 EditorPrefs에 저장).\n" +
                "2. String*.json에서 고유 문자를 추출해 unique_chars_*.txt를 만듭니다 (유럽어 추출: 기본 언어별, 공용 SDF는 유럽어 통합).\n" +
                "3. Apply 프로필을 지정하고 엔트리를 켠 뒤 Static TMP 폰트에 적용합니다 (아틀라스 2048).\n" +
                "유럽어 통합: Apply 엔트리에 FontAtlasBucket.European을 쓰세요 (같은 txt → 여러 폰트 OK).\n" +
                "같은 TMP_FontAsset을 가리키는 중복 Enabled 엔트리는 경고합니다 (Apply 탭 + Console).\n" +
                "상세 로그는 Console에 출력됩니다.",
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
                    if (GUILayout.Button("폴더 찾기", GUILayout.Width(100f)))
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
