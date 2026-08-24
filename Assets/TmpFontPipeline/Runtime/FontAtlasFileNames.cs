using System;

namespace TmpFontPipeline
{
    public static class FontAtlasFileNames
    {
        public const string OutputPrefix = "unique_chars_";
        public const string DialogueOutputSuffix = "_StringDialogue";

        public static readonly string[] CjkLanguageFieldNames =
        {
            nameof(FontAtlasBucket.Korean),
            nameof(FontAtlasBucket.SimplifiedChinese),
            nameof(FontAtlasBucket.TraditionalChinese),
            nameof(FontAtlasBucket.Japanese),
        };

        public static readonly string[] EuropeanLanguageFieldNames =
        {
            nameof(FontAtlasBucket.English),
            nameof(FontAtlasBucket.French),
            nameof(FontAtlasBucket.German),
            nameof(FontAtlasBucket.Italian),
            nameof(FontAtlasBucket.Spanish),
        };

        public static readonly string[] AllLanguageFieldNames =
        {
            nameof(FontAtlasBucket.Korean),
            nameof(FontAtlasBucket.SimplifiedChinese),
            nameof(FontAtlasBucket.TraditionalChinese),
            nameof(FontAtlasBucket.Japanese),
            nameof(FontAtlasBucket.English),
            nameof(FontAtlasBucket.French),
            nameof(FontAtlasBucket.German),
            nameof(FontAtlasBucket.Italian),
            nameof(FontAtlasBucket.Spanish),
        };

        // Bucket+Role 조합으로 unique_chars_*.txt 파일명을 반환합니다.
        public static string ResolveFileName(FontAtlasBucket bucket, FontUsageRole role)
        {
            string suffix = role == FontUsageRole.Dialogue ? DialogueOutputSuffix : string.Empty;
            return $"{OutputPrefix}{bucket}{suffix}.txt";
        }

        // CJK 버킷 enum을 추출기 JSON field name으로 변환합니다.
        public static string ToCjkFieldName(FontAtlasBucket bucket)
        {
            if (bucket != FontAtlasBucket.Korean
                && bucket != FontAtlasBucket.SimplifiedChinese
                && bucket != FontAtlasBucket.TraditionalChinese
                && bucket != FontAtlasBucket.Japanese)
            {
                throw new ArgumentException($"{bucket} is not a CJK field name.", nameof(bucket));
            }

            return bucket.ToString();
        }

        public static bool IsDialogueFileNameSuffix(string fileNameSuffix)
        {
            return string.Equals(fileNameSuffix, DialogueOutputSuffix, StringComparison.Ordinal);
        }
    }
}
