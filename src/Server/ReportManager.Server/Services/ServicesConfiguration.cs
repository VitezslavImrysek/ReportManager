#if Server && NET
using CoreWCF;
using CoreWCF.Channels;
using ReportManager;
using System.Configuration;
#else
using ReportManager;
using System.Configuration;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
#endif

#if Server
namespace ReportManager.Server.Services
#else
namespace ReportManager.Proxy.Services
#endif
{
    public static class ServicesConfiguration
    {
        public static BasicHttpBinding CreateReportServiceBinding()
		{
			var binding = new BasicHttpBinding
			{
				MaxReceivedMessageSize = 10 * 1024 * 1024, // 10 MB
				MaxBufferSize = 10 * 1024 * 1024 // 10 MB
			};
			return binding;
        }

		public static BasicHttpBinding CreateReportDownloadServiceBinding()
		{
			var binding = new BasicHttpBinding
			{
				TransferMode = TransferMode.StreamedResponse,
				MaxReceivedMessageSize = int.MaxValue,
				MaxBufferSize = 65536,
#if NETFRAMEWORK
				MaxBufferPoolSize = int.MaxValue,
#endif
				ReaderQuotas = new System.Xml.XmlDictionaryReaderQuotas
				{
					MaxDepth = 32,
					MaxStringContentLength = int.MaxValue,
					MaxArrayLength = int.MaxValue,
					MaxBytesPerRead = 4096,
					MaxNameTableCharCount = int.MaxValue
				}
			};

			return binding;
        }

#if Proxy

		public static ChannelFactory<T> CreateChannelFactory<T>()
		{
			switch (typeof(T))
			{
				case Type t when t == typeof(IReportService):
					{
						var baseUrl = GetBaseUrl();
						var endpointAddress = CreateReportServiceEndpointAddress(baseUrl);
						var binding = CreateReportServiceBinding();
						return new ChannelFactory<T>(binding, endpointAddress);
					}
				case Type t when t == typeof(IReportDownloadService):
					{
						var baseUrl = GetBaseUrl();
						var endpointAddress = CreateReportDownloadServiceEndpointAddress(baseUrl);
						var binding = CreateReportDownloadServiceBinding();
						return new ChannelFactory<T>(binding, endpointAddress);
					}
				default:
                    throw new NotSupportedException(typeof(T).ToString());
            }
        }
#elif Server && NETFRAMEWORK
        public static ServiceHost CreateServiceHost(Type type)
        {
            var baseUrl = GetBaseUrl();
            switch (type)
            {
                case Type t when t == typeof(ReportService):
                    {
                        
                        var endpointAddress = CreateReportServiceEndpointAddress(baseUrl);
                        var binding = CreateReportServiceBinding();
                        var host = new ServiceHost(type, endpointAddress.Uri);
						host.Description.Endpoints.Add(new ServiceEndpoint(ContractDescription.GetContract(typeof(IReportService)), binding, endpointAddress));
                        return host;
                    }
                case Type t when t == typeof(ReportDownloadService):
                    {
                        var endpointAddress = CreateReportDownloadServiceEndpointAddress(baseUrl);
                        var binding = CreateReportDownloadServiceBinding();
                        var host = new ServiceHost(type, endpointAddress.Uri);
                        host.Description.Endpoints.Add(new ServiceEndpoint(ContractDescription.GetContract(typeof(IReportDownloadService)), binding, endpointAddress));
                        return host;
                    }
                default:
                    throw new NotSupportedException(type.ToString());
            }
        }
#endif

        public static string GetBaseUrl()
		{
			return ConfigurationManager.AppSettings["ApiBaseUrl"];
        }

		public static EndpointAddress CreateReportServiceEndpointAddress(string baseUrl)
		{
			return new EndpointAddress($"{baseUrl}/ReportService");
        }

		public static EndpointAddress CreateReportDownloadServiceEndpointAddress(string baseUrl)
		{
			return new EndpointAddress($"{baseUrl}/ReportDownloadService");
        }
    }
}
