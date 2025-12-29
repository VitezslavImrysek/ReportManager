using ReportManager.Server;
using ReportManager.Server.Services;
using System.Configuration;



#if NET
using CoreWCF.Configuration;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
#else
using System.ServiceModel;
#endif

namespace ReportManager.Host
{
	internal static class Program
	{
#if NETFRAMEWORK
		static void Main(string[] args)
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
					var host = new ServiceHost(serviceType);
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
#else
        static void Main(string[] args)
		{
            IWebHost host = CreateWebHostBuilder(args).Build();
            host.Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
           WebHost.CreateDefaultBuilder(args)
            .UseUrls(ConfigurationManager.AppSettings["ApiBaseUrl"])
            .UseStartup<BasicHttpBindingStartup>();

        public class BasicHttpBindingStartup
        {
            public void ConfigureServices(IServiceCollection services)
            {
                //Enable CoreWCF Services, with metadata (WSDL) support
                services.AddServiceModelServices()
                    .AddServiceModelMetadata();
            }

            public void Configure(IApplicationBuilder app)
            {
                app.UseServiceModel(builder =>
                {
                    // Add the Calculator Service
                    builder.AddService<ReportService>(serviceOptions => { })
                    // Add BasicHttpBinding endpoint
                    .AddServiceEndpoint<ReportService, IReportService>(ServicesConfiguration.CreateReportServiceBinding(), "/ReportService");

                    // Add the Calculator Service
                    builder.AddService<ReportDownloadService>(serviceOptions => { })
                    // Add BasicHttpBinding endpoint
                    .AddServiceEndpoint<ReportDownloadService, IReportDownloadService>(ServicesConfiguration.CreateReportDownloadServiceBinding(), "/ReportDownloadService");

                    // Configure WSDL to be available
                    var serviceMetadataBehavior = app.ApplicationServices.GetRequiredService<CoreWCF.Description.ServiceMetadataBehavior>();
                    serviceMetadataBehavior.HttpGetEnabled = true;
                });
            }
        }
#endif
    }
}
