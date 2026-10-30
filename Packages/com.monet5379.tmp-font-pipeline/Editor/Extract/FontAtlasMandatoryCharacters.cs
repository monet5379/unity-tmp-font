using System.Text;

namespace TmpFontPipeline.Editor
{
    public static class FontAtlasMandatoryCharacters
    {
        public const string AtlasCharacters = "_";

        public static string MergeIntoCharacterString(string chars)
        {
            if (string.IsNullOrEmpty(AtlasCharacters))
            {
                return chars ?? string.Empty;
            }

            if (string.IsNullOrEmpty(chars))
            {
                return AtlasCharacters;
            }

            var merged = new StringBuilder(chars.Length + AtlasCharacters.Length);
            _ = merged.Append(chars);

            for (int i = 0; i < AtlasCharacters.Length; i++)
            {
                char required = AtlasCharacters[i];
                if (chars.IndexOf(required) < 0)
                {
                    _ = merged.Append(required);
                }
            }

            return merged.ToString();
        }
    }
}
