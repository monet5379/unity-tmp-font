using System.Globalization;
using System.Text.RegularExpressions;

namespace TmpFontPipeline.Editor
{
    // TMP 태그·placeholder·토큰을 제거해 아틀라스 추출용 문자열만 남깁니다.
    public static class StringTextSanitizer
    {
        private static readonly Regex StyleTagRegex =
            new(@"<style=[^>]*>(.*?)<\/style>", RegexOptions.Compiled);

        private static readonly Regex ColorTagRegex =
            new(@"<color=.*?>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex ColorCloseTagRegex =
            new(@"<\/color>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex BoldTagRegex =
            new(@"<b>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex BoldCloseTagRegex =
            new(@"<\/b>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex ItalicTagRegex =
            new(@"<i>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex ItalicCloseTagRegex =
            new(@"<\/i>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex SizeTagRegex =
            new(@"<size=.*?>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex SizeCloseTagRegex =
            new(@"<\/size>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex SpriteTagRegex =
            new(@"<sprite[^>]*>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex FormatPlaceholderRegex =
            new(@"\{[0-9]+\}", RegexOptions.Compiled);

        private static readonly Regex DataTokenRegex =
            new(@"\[[^\]]+\]", RegexOptions.Compiled);

        public static string SanitizeForFont(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            string result = value;
            result = StyleTagRegex.Replace(result, "$1");
            result = ColorTagRegex.Replace(result, string.Empty);
            result = ColorCloseTagRegex.Replace(result, string.Empty);
            result = BoldTagRegex.Replace(result, string.Empty);
            result = BoldCloseTagRegex.Replace(result, string.Empty);
            result = ItalicTagRegex.Replace(result, string.Empty);
            result = ItalicCloseTagRegex.Replace(result, string.Empty);
            result = SizeTagRegex.Replace(result, string.Empty);
            result = SizeCloseTagRegex.Replace(result, string.Empty);
            result = SpriteTagRegex.Replace(result, string.Empty);
            result = FormatPlaceholderRegex.Replace(result, string.Empty);
            result = DataTokenRegex.Replace(result, string.Empty);

            return result;
        }

        // 폰트 아틀라스에 포함할 코드포인트인지 판별합니다.
        public static bool IsRenderableCodePoint(int codePoint)
        {
            switch (codePoint)
            {
                case '\n': // line feed
                case '\r': // carriage return
                case '\t': // tab
                case '\u00AD': // soft hyphen
                case '\u2028': // line separator
                case '\u2029': // paragraph separator
                case '\uFEFF': // BOM / ZWNBSP
                    return false;
            }

            return !IsControlCodePoint(codePoint);
        }

        private static bool IsControlCodePoint(int codePoint)
        {
            if (codePoint <= 0xFFFF) // BMP (Basic Multilingual Plane)
            {
                return char.IsControl((char)codePoint);
            }

            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(char.ConvertFromUtf32(codePoint), 0);
            return category == UnicodeCategory.Control
                || category == UnicodeCategory.Format;
        }

        public static void AddCodePoints(System.Collections.Generic.HashSet<int> set, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            for (int i = 0; i < value.Length; i++)
            {
                int codePoint = char.ConvertToUtf32(value, i);
                if (char.IsHighSurrogate(value[i]))
                {
                    i++;
                }

                if (!IsRenderableCodePoint(codePoint))
                {
                    continue;
                }

                _ = set.Add(codePoint);
            }
        }
    }
}
