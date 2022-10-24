using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.Json;

namespace TextMateSharp.Internal.Parser.Json
{
    public class JSONPListParser<T>
    {

        private bool theme;
        private readonly JsonReaderOptions options;

        public JSONPListParser(bool theme)
        {
            this.theme = theme;

            this.options = new JsonReaderOptions();
            options.CommentHandling = JsonCommentHandling.Skip;
            options.AllowTrailingCommas = true;
        }

        public T Parse(Stream contents)
        {
            PList<T> pList = new PList<T>(theme);

            var buffer = new byte[1024];

            // Fill the buffer.
            // For this snippet, we're assuming the stream is open and has data.
            // If it might be closed or empty, check if the return value is 0.
            int size = contents.Read(buffer, 0, buffer.Length);

            var reader = new Utf8JsonReader(buffer.AsSpan(0, size), isFinalBlock: false, state: new JsonReaderState(options));

            while (true)
            {
                int bytesRead = -1;

                while (!reader.Read())
                {
                    bytesRead = GetMoreBytesFromStream(contents, ref buffer, ref reader);

                    if (bytesRead == 0)
                        break;
                }

                if (bytesRead == 0)
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

        private static int GetMoreBytesFromStream(Stream stream, ref byte[] buffer, ref Utf8JsonReader reader)
        {
            int bytesRead;
            if (reader.BytesConsumed < buffer.Length)
            {
                ReadOnlySpan<byte> leftover = buffer.AsSpan((int)reader.BytesConsumed);

                if (leftover.Length == buffer.Length)
                {
                    Array.Resize(ref buffer, buffer.Length * 2);
                }

                leftover.CopyTo(buffer);
                bytesRead = stream.Read(buffer, leftover.Length, buffer.Length - leftover.Length);
                reader = new Utf8JsonReader(buffer.AsSpan(0, bytesRead + leftover.Length), isFinalBlock: bytesRead == 0, reader.CurrentState);
            }
            else
            {
                bytesRead = stream.Read(buffer, 0, buffer.Length);
                reader = new Utf8JsonReader(buffer.AsSpan(0, bytesRead), isFinalBlock: bytesRead == 0, reader.CurrentState);
            }

            //reader = new Utf8JsonReader(buffer, isFinalBlock: bytesRead == 0, reader.CurrentState);
            return bytesRead;
        }
    }
}