using YamlDotNet.Serialization;

namespace HorizonLoad
{
    public struct Application
    {
        public List<Authorizer> Authorizers { get; set; }
        public List<Service> Services { get; set; }

        public override string ToString()
        {
            string dump = "(Application) {\n";
            dump += "  authorizers (" + Authorizers.Count + ") {\n";
            Authorizers.ForEach(authorizer => {
                dump += authorizer.ToString();
            });
            dump += "  }\n";
            dump += "  services (" + Services.Count + ") {\n";
            Services.ForEach(service => {
                dump += service.ToString();
            });
            dump += "  }\n";
            dump += "}\n";
            return dump;
        }
    }

    public static class ApplicationLoader
    {
        public static Application LoadFromYAML(string path)
        {
            var deserializer = new DeserializerBuilder().Build();
            var yamlContent = File.ReadAllText(path);

            var root = deserializer.Deserialize<Dictionary<string, dynamic>>(yamlContent);

            var application = new Application
            {
                Authorizers = ParseAuthorizers(root)
            };
            application.Services = ParseServices(root, application);

            return application;
        }

        private static List<Authorizer> ParseAuthorizers(Dictionary<string, dynamic> root)
        {
            if (!root.ContainsKey("authorizers") || !(root["authorizers"] is Dictionary<object, object> authorizers))
            {
                Console.WriteLine("No authorizers loaded as none were defined, define at root level with the key 'authorizers'");
                return new List<Authorizer>();
            }

            return authorizers.Select(entry =>
            {
                var authorizerDefinition = (Dictionary<object, object>)entry.Value;

                return new Authorizer
                {
                    authorizerName = entry.Key.ToString()!,
                    host = authorizerDefinition.ContainsKey("host") ? authorizerDefinition["host"].ToString()! : "127.0.0.1",
                    port = authorizerDefinition.ContainsKey("port") ? int.Parse(authorizerDefinition["port"].ToString()!) : 0
                };
            }).ToList();
        }

        private static List<Service> ParseServices(Dictionary<string, dynamic> root, Application application)
        {
            if (!root.ContainsKey("service-map") || !(root["service-map"] is Dictionary<object, object> serviceMap))
            {
                throw new Exception("No services inside the orchestration file, make sure the root level has the key `service-map`");
            }

            return serviceMap.Select(serviceEntry =>
            {
                var serviceDefinition = (Dictionary<object, object>)serviceEntry.Value;

                var service = new Service
                {
                    serviceName = serviceEntry.Key.ToString(),
                    route = serviceDefinition.ContainsKey("route") ? serviceDefinition["route"].ToString() : throw new Exception($"Service {serviceEntry.Key} doesn't have a route set"),
                    requiresAuth = serviceDefinition.ContainsKey("require-auth"),
                    servers = ParseServers(serviceDefinition),
                    stripRoute = serviceDefinition.ContainsKey("strip-route") ? bool.Parse(serviceDefinition["strip-route"].ToString()!) : false
                };

                if (service.requiresAuth)
                {
                    var requireAuth = serviceDefinition["require-auth"].ToString();
                    service.authorizer = application.Authorizers.Find(auth => auth.authorizerName.Equals(requireAuth));
                    if (service.authorizer == null)
                    {
                        throw new Exception($"Unknown authorizer {requireAuth}");
                    }
                }

                return service;
            }).ToList();
        }

        private static List<Server> ParseServers(Dictionary<object, object> serviceDefinition)
        {
            if (!serviceDefinition.ContainsKey("servers") || !(serviceDefinition["servers"] is Dictionary<object, object> servers))
            {
                throw new Exception($"No servers listed on service {serviceDefinition["serviceName"]}");
            }

            return servers.Select(serverEntry =>
            {
                var serverDefinition = (Dictionary<object, object>)serverEntry.Value;
                return new Server
                {
                    serverName = serverEntry.Key.ToString()!,
                    host = serverDefinition.ContainsKey("host") ? serverDefinition["host"].ToString()! : throw new Exception($"Server {serverEntry.Key} of service {serviceDefinition["serviceName"]} doesn't have a host key set"),
                    port = serverDefinition.ContainsKey("port") ? int.Parse(serverDefinition["port"].ToString()!) : -1
                };
            }).ToList();
        }
    }
}
