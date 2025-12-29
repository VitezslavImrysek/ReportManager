#if NET

using CoreWCF.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ReportManager.Server;
using ReportManager.Server.Services;

namespace ReportManager.Host
{
    internal sealed class NetHost
    {
        public void Run(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            // Ensure controllers from the ReportManager.Server assembly are discovered
            builder.Services.AddControllers()
                .AddApplicationPart(typeof(ReportManager.Server.Controllers.ReportDownloadController).Assembly);
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            //Enable CoreWCF Services, with metadata (WSDL) support
            builder.Services.AddServiceModelServices()
                .AddServiceModelMetadata();
            builder.WebHost.UseUrls(ServicesConfiguration.GetBaseUrl());

            var app = builder.Build();

            app.UseRouting();
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
                var serviceMetadataBehavior = app.Services.GetRequiredService<CoreWCF.Description.ServiceMetadataBehavior>();
                serviceMetadataBehavior.HttpGetEnabled = true;
            });

            app.UseSwagger();
            app.UseSwaggerUI();
            app.MapControllers();

            app.Run();
        }
    }
}
#endif