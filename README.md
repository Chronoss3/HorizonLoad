# HorizonLoad

HorizonLoad is a powerful Reverse-Proxy/Load Balancer designed to enhance the security and scalability of your backend architecture. Developed in C#, HorizonLoad boasts blazing-fast performance, and its flexibility makes it compatible with any web-servable content. This tool allows you to obfuscate the intricacies of your backend infrastructure and provides support for an unlimited number of servers.

## Getting Started

1. **Clone the Repository:**
   ```bash
   git clone https://github.com/your-username/HorizonLoad.git
   cd HorizonLoad
   ```

2. **Setup Application Structure:**
   Define your application structure in the `orchestration.yaml` file. This file allows you to configure authorizers, service routes, and servers for seamless integration.

   ```yaml
   authorizers:
     frontend-auth:
       host: 127.0.0.1
       port: 4500

   service-map:
     customer-service-ui:
       route: /customers
       require-auth: frontend-auth
       servers:
         main:
           host: 127.0.0.1
           port: 3000
   ```

   Customize authorizers for different tasks, specify routes, and define servers according to your application's needs.

3. **SSL/HTTPS Configuration:**
   While SSL Certificate Providers are not bundled with HorizonLoad, you can easily implement your own. Future updates will include built-in providers. To use your custom certificate provider, add the following code before starting the runtime:

   ```C#
   ICertificateProvider myCertificateHandler = new CustomCertificateHandler();
   myRuntime.LoadCertificate(myCertificateHandler);
   ```
4. **Run In Docker (Optional)**
    ```shell
    docker build -t horizonload &&
    docker run -p 8080:8080 -it horizonload 
    ```
    swap ports out if necessary, dont forget to update the port in `Program.cs` file.

## Use Cases and Benefits

- **Enhanced Security:**
  HorizonLoad acts as a layer of defense by obfuscating the backend architecture, making it more challenging for potential attackers to understand the underlying infrastructure.

- **Scalability:**
  With support for an unlimited number of servers, HorizonLoad enables seamless scalability for your applications. Easily add or remove servers based on demand.

- **Flexible Authorization:**
  Configure different authorizers for various tasks, allowing you to implement authentication mechanisms such as API keys and Bearer Tokens. Fine-tune security based on your specific requirements.

- **Efficient Load Balancing:**
  Distribute incoming traffic across multiple servers to optimize performance and ensure high availability. HorizonLoad intelligently balances the load to prevent overloading individual servers.

- **Compatibility:**
  Designed to work with any web-servable content, HorizonLoad provides versatility and can be seamlessly integrated into a wide range of applications.

Feel free to explore and contribute to HorizonLoad, and stay tuned for upcoming updates, including bundled SSL Certificate Providers for added convenience.