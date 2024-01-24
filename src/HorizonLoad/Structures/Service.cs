
namespace HorizonLoad
{
    public struct Service
    {
        public string? serviceName;
        public string? route;
        public bool requiresAuth = false;
        public bool stripRoute = false; // allows you to stripe the value of route from the path
        public Authorizer? authorizer;


        public List<Server> servers = new();

        public Service()
        {

        }
        public override string ToString()
        {
            string authStr = requiresAuth ? authorizer?.authorizerName! : "None";
            string output = $"   Service(name='{serviceName}', route='{route}', authorizer='{authStr}', servers='{servers.Count}', strip-route='{stripRoute}') (\n";
            servers.ForEach(server => {
                output += server.ToString();
            });
            output += "    )\n";
            return output;
        }
    }
}