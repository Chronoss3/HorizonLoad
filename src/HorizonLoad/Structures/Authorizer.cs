
namespace HorizonLoad
{
    public struct Authorizer
    {
        public string authorizerName;
        public string host;
        public int? port;

        public override string ToString()
        {
            return $"   Authoriser(name='{authorizerName}', host='{host}', port='{port}')\n";
        }
    }
}