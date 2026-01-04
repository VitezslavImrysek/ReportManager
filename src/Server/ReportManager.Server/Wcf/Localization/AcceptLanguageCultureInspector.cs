#if NETFRAMEWORK

using ReportManager.Shared;
using System.Globalization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace ReportManager.Server.Wcf.Localization
{
    public sealed class AcceptLanguageCultureInspector : IDispatchMessageInspector
    {
        private readonly CultureInfo _defaultCulture;

        public AcceptLanguageCultureInspector()
        {
            _defaultCulture = CultureInfo.CurrentUICulture;
        }

        public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext)
        {
            var culture = ResolveCultureFromHttpHeader() ?? _defaultCulture;

            CultureInfo.CurrentUICulture = culture;

            return null;
        }

        public void BeforeSendReply(ref Message reply, object correlationState) 
        {
            // Restore default culture
            CultureInfo.CurrentUICulture = _defaultCulture;
        }

        private CultureInfo? ResolveCultureFromHttpHeader()
        {
            // HTTP header je v HttpRequestMessageProperty
            if (!OperationContext.Current.IncomingMessageProperties.TryGetValue(HttpRequestMessageProperty.Name, out var propObj))
                return null;

            var http = propObj as HttpRequestMessageProperty;
            var acceptLanguage = http?.Headers[Constants.LanguageHeaderName];
            if (string.IsNullOrWhiteSpace(acceptLanguage))
                return null;

            try
            {
                return CultureInfo.GetCultureInfo(acceptLanguage);
            }
            catch
            {
                return null;
            }
        }
    }
}

#endif