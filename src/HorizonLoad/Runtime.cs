using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

using HorizonLoad.Inbound;
using HorizonLoad.SSL;

namespace HorizonLoad
{
    public class Runtime
    {
        private readonly int port;
        private readonly int? securePort;
        private TcpListener? listener;
        private TcpListener? secureListener;
        private Application application;

        private X509Certificate2? sslCertificate;
        private RSA? privateKey;

        public Runtime(int port, int? securePort = null)
        {
            this.port = port;
            this.securePort = securePort;

            if (!File.Exists("orchestration.yaml"))
            {
                throw new Exception("No Orchestration file found");
            }

            application = ApplicationLoader.LoadFromYAML("orchestration.yaml");
        }

        public void LoadCertificate(ICertificateProvider certificateProvider)
        {
            sslCertificate = certificateProvider.GetCertificate();
            privateKey = certificateProvider.GetPrivateRsaKey();
        }

        public async void Start()
        {
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            if (securePort != null)
            {
                await Task.Run(() => StartSSL());
            }


            while (true)
            {
                // Accept a client connection
                TcpClient client = listener.AcceptTcpClient();

                // Create a separate thread to handle the client request
                Thread clientThread = new(() => {
                    HandleClient(client);
                });
                clientThread.Start();
            }
        }

        private void HandleClient(TcpClient client)
        {
            // Get the client's stream
            NetworkStream networkStream = client.GetStream();

            // Read the request
            byte[] buffer = new byte[4096];
            int bytesRead = networkStream.Read(buffer, 0, buffer.Length);
            string requestSource = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            HttpRequest request = new(requestSource);

            foreach(Service service in application.Services)
            {
                if (request.Path!.StartsWith(service.route!)) {

                    // goes to this service.
                    Services.Proxy.PipeRequestFromService(networkStream, service, request);
                    break;
                }
            }
        }
        
        private async void StartSSL()
        {
            if (securePort == null)
            {
                return;
            }
            if (sslCertificate == null)
            {
                return;
            }
            if (privateKey == null)
            {
                return;
            }
            secureListener = new TcpListener(IPAddress.Any, securePort!.Value);
            secureListener.Start();

            Console.WriteLine($"HTTPS Server started on port {securePort!.Value}");

            while (true)
            {
                // Accept a client connection for HTTPS
                TcpClient client = await secureListener.AcceptTcpClientAsync();

                // Create a separate thread to handle the client request over HTTPS
                Thread clientThread = new(() => {

                    using (SslStream sslStream = new(client.GetStream(), false))
                    {
                        try
                        {
                            
                            // Create a certificate with the private key
                            sslCertificate = sslCertificate!.CopyWithPrivateKey(privateKey);
                        

                            // Authenticate with the certificate
                            sslStream.AuthenticateAsServer(sslCertificate, false, SslProtocols.Tls13, true);

                            // Read the request
                            byte[] buffer = new byte[4096];
                            int bytesRead = client.GetStream().Read(buffer, 0, buffer.Length);
                            string requestSource = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                            HttpRequest request = new(requestSource);

                            foreach(Service service in application.Services)
                            {
                                if (request.Path!.StartsWith(service.route!)) {

                                    // goes to this service.
                                    Services.Proxy.PipeRequestFromService(sslStream, service, request);
                                    break;
                                }
                            }

                        } catch (Exception) {}
                    }

                });
                clientThread.Start();
            }
        }
    }
}