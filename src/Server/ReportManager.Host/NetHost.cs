#if NET

using CoreWCF.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using ReportManager.Server.Wcf;
using ReportManager.Shared;

namespace ReportManager.Host
{
    internal sealed class NetHost
    {
        public void Run(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddLocalization();

            builder.Services.Configure<RequestLocalizationOptions>(o =>
            {
                o.DefaultRequestCulture = new RequestCulture(Constants.DefaultLanguage);
                o.SupportedCultures = null;
                o.SupportedUICultures = null;

                o.RequestCultureProviders.Insert(0, new AcceptLanguageHeaderRequestCultureProvider());
            });

            // Ensure controllers from the ReportManager.Server assembly are discovered
            builder.Services.AddControllers()
                .AddApplicationPart(typeof(ReportManager.Server.Controllers.ReportDownloadController).Assembly);
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddTransient<ReportManager.Server.Services.ReportService>();
            builder.Services.AddTransient<ReportManager.Server.Services.ReportDownloadService>();
            //Enable CoreWCF Services, with metadata (WSDL) support
            builder.Services.AddServiceModelServices()
                .AddServiceModelMetadata();
            builder.WebHost.UseUrls(ServicesConfiguration.GetBaseUrl());
            builder.WebHost.ConfigureKestrel((context, options) =>
            {
                options.AllowSynchronousIO = true;
            });

            var app = builder.Build();

            app.UseRouting(); 
            app.UseRequestLocalization();
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