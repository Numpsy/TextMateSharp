using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace TextMateSharp.Internal.Parser.Json
{
    public class JSONPListParser<T>
    {

        private bool theme;

        public JSONPListParser(bool theme)
        {
            this.theme = theme;
        }

        public T Parse(Stream contents)
        {
            PList<T> pList = new PList<T>(theme);

            byte[] hmm = new byte[contents.Length];
            contents.Read(hmm, 0, hmm.Length);

            JsonReaderOptions options = new JsonReaderOptions();
            options.CommentHandling = JsonCommentHandling.Skip;
            options.AllowTrailingCommas = true;
            var reader = new Utf8JsonReader(hmm, options);

            while (true)
            {
                if (!reader.Read())
                    break;

                JsonTokenType nextToken = reader.TokenType;
                switch (nextToken)
                {
                    case JsonTokenType.StartArray:
                        pList.StartElement("array");
                        break;
                    case JsonTokenType.EndArray:
                        pList.EndElement("array");
                        break;
                    case JsonTokenType.StartObject:
                        pList.StartElement("dict");
                        break;
                    case JsonTokenType.EndObject:
                        pList.EndElement("dict");
                        break;
                    case JsonTokenType.PropertyName:
                        pList.StartElement("key");
                        pList.AddString((string)reader.GetString());
                        pList.EndElement("key");
                        break;
                    case JsonTokenType.String:
                        pList.StartElement("string");
                        pList.AddString(reader.GetString());
                        pList.EndElement("string");
                        break;
                    case JsonTokenType.Null:
                    case JsonTokenType.Number:
                    case JsonTokenType.False:
                    case JsonTokenType.True:
                        break;
                }
            }
            return pList.GetResult();
        }
    }
}