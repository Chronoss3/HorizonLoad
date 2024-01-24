# HorizonLoad

A Reverse-Proxy/Load Balancer that can be used to obfuscate your backend architecture. Supply an unlimited amount of
servers (theoretically,nothing is infinite). Built on C# for blazing fast performance. There will be room for improvement.
However this works with any web servable content. 

Setup your applications structurre in `orchestration.yaml`

```yaml
authorizers: # you can setup different authorizers for different tasks
             # this endpoint needs to return a response of {"authorised": true|false}
             # the headers, and POST data of the current request is forwarded on
             # to make it possible to supply API keys and also Bearer Tokens etc..
  frontend-auth:
    host: 127.0.0.1
    port: 4500

service-map:
    customer-service-ui: # can chain to a react app.
        route: /customers
        require-auth: frontend-auth
        servers:
            main:
                host: 127.0.0.1
                port: 3000
```

### SSL/HTTPS
Currently there is no SSL Certificate Providers bundled, this will be coming in upcoming updates. Feel free to implement your own.
Before starting the Runtime::Start, call:
```C#
ICertificateProvider myCertificateHandler = new CustomCertificateHandler();
myRuntime.LoadCertificate(myCertificateHandler);
```