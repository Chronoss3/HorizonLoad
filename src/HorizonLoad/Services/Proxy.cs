
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using HorizonLoad.Inbound;

namespace HorizonLoad.Services
{
    public static class Proxy
    {
        public static async void PipeRequestFromService(Stream stream, Service service, HttpRequest request)
        {
            // check if needs authenticating.
            if (service.requiresAuth)
            {
                // authorize first.
                if (!IsAuthorized(request, service))
                {
                    StringBuilder headersBuilder = new();

                    headersBuilder.AppendLine("HTTP/1.1 403 Forbidden\r\n");
                    await stream.WriteAsync(Encoding.UTF8.GetBytes(headersBuilder.ToString()));

                    // Write the response content to the existing TcpClient's NetworkStream
                    await stream.WriteAsync(Encoding.UTF8.GetBytes("Not Permitted"));
                    await stream.FlushAsync();
                    stream.Close();
                    return;
                }
            }

            // get the server with the least requests
            Server targetServer = service.servers.OrderBy(server => server.currentRequests)
                    .FirstOrDefault();
            
            // if strip route is enabled
            // it removes the value of route:
            // from the URL.
            if (service.stripRoute)
            {
                request.Path = request.Path!.Replace(service.route!, "/").Replace("//", "/"); // strip the route out and replace double slashes
            }

            // Make HTTP request using HttpClient
            using HttpClient httpClient = new();

            // assign headers from the current request
            foreach (var header in request.Headers)
            {
                try{
                    httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }catch(Exception) {}
            }

            // create the URL ready for the request
            string url;
            if (targetServer.port != 0 && targetServer.port != null)
            {
                url = $"http://{targetServer.host}:{targetServer.port}{request.Path}";
            }
            else
            {
                url = $"http://{targetServer.host}{request.Path}";
            }
            
            // now lets fetch the page
            HttpResponseMessage response;

            try{
                response = request.Method!.ToUpper() switch
                {
                    "GET" => await httpClient.GetAsync(url),
                    "POST" => await httpClient.PostAsync(url, new StringContent(request.Body!)),
                    "PUT" => await httpClient.PutAsync(url, new StringContent(request.Body!)),
                    "PATCH" => await httpClient.PatchAsync(url, new StringContent(request.Body!)),
                    "DELETE" => await httpClient.DeleteAsync(url),
                    "HEAD" => await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, url)),
                    // Add cases for other HTTP methods if needed
                    _ => new HttpResponseMessage(HttpStatusCode.MethodNotAllowed)
                    {
                        Content = new StringContent("Method Not Allowed")
                    },// Handle unsupported methods or return an error response
                };
            }catch(Exception e)
            {
                // usually if the microservice is down, it wont
                Console.WriteLine(e.Message);

                // shift the request to another server
                service.servers.Remove(targetServer);

                if (service.servers.Count == 0)
                {
                    StringBuilder headersBuilder = new();

                    headersBuilder.AppendLine("HTTP/1.1 503 Service Unavailable\r\n");
                    await stream.WriteAsync(Encoding.UTF8.GetBytes(headersBuilder.ToString()));

                    // Write the response content to the existing TcpClient's NetworkStream
                    await stream.WriteAsync(Encoding.UTF8.GetBytes("There was an error with this applications configuration"));
                    await stream.FlushAsync();
                    stream.Close();
                    return;
                } else {
                    PipeRequestFromService(stream, service, request);
                }
                return;
            }
            if (IsChunked(response) && IsGzipEncoded(response))
            {
                
                await stream.WriteAsync(Encoding.UTF8.GetBytes(GetResponseHeaders(response)));
                using Stream responseStream = await response.Content.ReadAsStreamAsync();
                using var gzipStream = new GZipStream(responseStream, CompressionMode.Decompress);
                using StreamReader reader = new StreamReader(gzipStream);
                // Assuming chunked content
                string line = await reader.ReadToEndAsync();

                await stream.WriteAsync(Encoding.UTF8.GetBytes(line));

                try
                {
                    await stream.FlushAsync();
                    stream.Close();
                }
                catch (Exception)
                {
                    stream.Close();
                }
            }
            else
            {
                await stream.WriteAsync(Encoding.UTF8.GetBytes(GetResponseHeaders(response)));
                if (
                    response.Content.Headers.ContentType?.MediaType?.StartsWith("text/") != true
                )
                {
                    // Assuming the image is in binary format, you can directly write the binary data to the network stream
                    byte[] imageData = await response.Content.ReadAsByteArrayAsync();
                    await stream.WriteAsync(imageData);
                }
                else
                {
                    // Handle non-chunked or non-gzipped content here
                    await response.Content.CopyToAsync(stream);
                }
                try{
                    await stream.FlushAsync();
                    stream.Close();
                }catch(Exception)
                {
                    stream.Close();
                }
            }
        }
        private static string GetResponseHeaders(HttpResponseMessage response)
        {
            StringBuilder headersBuilder = new();

            headersBuilder.AppendLine($"HTTP/{response.Version} {(int)response.StatusCode} {response.ReasonPhrase}");

            foreach (var header in response.Headers)
            {
                if (header.Key == "Transfer-Encoding") {

                } else {
                    headersBuilder.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
                }
            }

            foreach (var header in response.Content.Headers)
            {
                if (header.Key == "Content-Encoding") {
                    continue;
                }
                headersBuilder.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
            }

            headersBuilder.AppendLine(); // Empty line to indicate the end of headers

            return headersBuilder.ToString();
        }
        private static bool IsChunked(HttpResponseMessage response)
        {
            return response.Headers.TransferEncodingChunked.HasValue &&
                response.Headers.TransferEncodingChunked.Value;
        }

        private static bool IsGzipEncoded(HttpResponseMessage response)
        {
            return response.Content.Headers.ContentEncoding.Contains("gzip");
        }

        private static bool IsAuthorized(HttpRequest request, Service service)
        {
            Authorizer authenticator = (Authorizer) service.authorizer!;

            try{
                string response = Executor.Execute(authenticator.host, authenticator.port, request);
            
                Dictionary<string,bool> authResponse = JsonSerializer.Deserialize<Dictionary<string,bool>>(response)!;
                return authResponse["authorised"];
            }catch(Exception)
            {
                Console.WriteLine($"Authorizer {authenticator.authorizerName} is not available: Connection Refused, are you sure its running?");
                return false;
            }
        }
    }
}