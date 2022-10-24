using System.Collections.Generic;
using System.IO;

using TextMateSharp.Internal.Parser.Json;
using TextMateSharp.Registry;
using TextMateSharp.Themes;

namespace TextMateSharp.Internal.Themes.Reader
{
    public class ThemeReader
    {
        public static IRawTheme ReadThemeSync(Stream reader)
        {
            JSONPListParser<IRawTheme> parser = new JSONPListParser<IRawTheme>(true);
            return parser.Parse(reader);
        }
    }
}
