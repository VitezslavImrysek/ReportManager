#if NETFRAMEWORK

using ReportManager.Server.Wcf;
using System.ServiceModel;

namespace ReportManager.Host
{
    internal sealed class NetFxHost
    {
        public void Run(string[] args)
        {
            var serviceTypes = new List<Type>
            {
                typeof(ReportService),
                typeof(ReportDownloadService)
            };

            var hosts = new List<ServiceHost>();

            foreach (var serviceType in serviceTypes)
            {
                try
                {
                    var host = ServicesConfiguration.CreateServiceHost(serviceType);
                    host.Open();
                    hosts.Add(host);

                    foreach (var endpoint in host.Description.Endpoints)
                    {
                        Console.WriteLine($"{serviceType.Name} running at: {endpoint.Address.Uri} (Binding: {endpoint.Binding.Name})");
                    }
                    Console.WriteLine($"WSDL: http://localhost:8733/{serviceType.Name}?wsdl");
                    Console.WriteLine();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            Console.WriteLine("Press ENTER to stop...");
            Console.ReadLine();

            foreach (var host in hosts)
            {
                try
                {
                    host.Close();
                }
                catch (Exception)
                {

                }
            }
        }
    }
}
#endif