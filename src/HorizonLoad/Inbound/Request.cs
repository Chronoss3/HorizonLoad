using System.Collections.Generic;
using System.Text;
using YamlDotNet.Core.Tokens;

namespace HorizonLoad.Inbound {
    public class HttpRequest
    {
        public string? Method { get; set; }
        public string? Path { get; set; }
        public Dictionary<string, string> Headers { get; set; } = new();
        public string? Body { get; set; }

        public HttpRequest(string requestString)
        {
            ParseRequest(requestString);
        }
        public HttpRequest()
        {

        }
        public HttpRequest Clone()
        {
            HttpRequest clone = new() {
                Method = this.Method,
                Path = this.Path,
                Headers = this.Headers,
                Body = this.Body
            };

            return clone;
        }

        private void ParseRequest(string requestString)
        {
            string[] lines = requestString.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length == 0)
            {
                Console.WriteLine(requestString);
                throw new Exception("Lines empty");
            }
            
            // Parse the first line to get the method and path
            string[] firstLineParts = lines[0].Split(' ');
            Method = firstLineParts[0];
            Path = firstLineParts[1];

            // Parse headers
            Headers = new Dictionary<string, string>();
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    break;  // Headers end

                string[] headerParts = lines[i].Split(':');
                string headerName = headerParts[0].Trim();
                string headerValue = headerParts.Length > 1 ? headerParts[1].Trim() : string.Empty;
                Headers[headerName] = headerValue;
            }

            // Parse body
            int bodyStartIndex = Array.IndexOf(lines, string.Empty) + 1;
            Body = bodyStartIndex < lines.Length ? string.Join("\r\n", lines.Skip(bodyStartIndex)) : string.Empty;
        }

        public override string ToString()
        {
            StringBuilder requestBuilder = new();

            // Append the first line with method and path
            requestBuilder.Append($"{Method} {Path} HTTP/1.1\r\n");

            // Append headers
            foreach (var header in Headers)
            {
                requestBuilder.Append($"{header.Key}: {header.Value}\r\n");
            }

            // Append an empty line to separate headers and body
            requestBuilder.Append("\r\n");

            // Append body
            requestBuilder.Append(Body);

            return requestBuilder.ToString();
        }
    }
}