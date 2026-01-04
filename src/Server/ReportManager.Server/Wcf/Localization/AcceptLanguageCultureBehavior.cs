#if NETFRAMEWORK

using System.Collections.ObjectModel;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace ReportManager.Server.Wcf.Localization
{
    public sealed class AcceptLanguageCultureBehavior : IServiceBehavior
    {
        private readonly IDispatchMessageInspector _inspector;

        public AcceptLanguageCultureBehavior(IDispatchMessageInspector inspector) => _inspector = inspector;

        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
            foreach (ChannelDispatcher cd in serviceHostBase.ChannelDispatchers)
            {
                foreach (EndpointDispatcher ed in cd.Endpoints)
                    ed.DispatchRuntime.MessageInspectors.Add(_inspector);
            }
        }

        public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase) { }
        public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters) { }
    }
}

#endif