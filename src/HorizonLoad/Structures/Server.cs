
namespace HorizonLoad
{
    public struct Server
    {
        public string? serverName;
        public string? host;
        public int? port;

        public int currentRequests = 0;

        public Server()
        {

        }

        public override string ToString()
        {
            return $"     Server(name='{serverName}', host='{host}', port='{port}')\n";
        }
    }
}