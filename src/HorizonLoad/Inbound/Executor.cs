
using System.Text;

namespace HorizonLoad.Inbound
{
    public static class Executor
    {
        public static string Execute(string host, int? port, HttpRequest httpRequest)
        {
            using HttpClient httpClient = new();
            

            string url = $"http://{host}";
            
            if (port != null){
                url += $":{port}";
            }
            url += $"{httpRequest.Path}";

            using var requestMessage = new HttpRequestMessage(new HttpMethod(httpRequest.Method!), url)
            {
                Content = new StringContent(httpRequest.Body!)
            };


            foreach (var header in httpRequest.Headers)
            {
                try{
                    requestMessage.Headers.Add(header.Key, header.Value);
                }catch(FormatException)
                {
                    // nothing major.
                }
            }

            using var response = httpClient.Send(requestMessage);
            return ReadStreamAsString(response.Content.ReadAsStream());
        }

        static string ReadStreamAsString(Stream stream)
        {
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }
    }
}