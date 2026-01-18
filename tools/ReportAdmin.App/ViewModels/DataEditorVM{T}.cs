using ReportAdmin.App.Messages;
using ReportAdmin.Core;

namespace ReportAdmin.App.ViewModels
{
    public abstract class DataEditorVM<TData> : DataEditorVM<TData, object>
        where TData : class, new()
    { }

    public abstract class DataEditorVM<TData, TContext> : NotificationObject
        where TData : class, new()
        where TContext : class
    {
        public bool IsInitialized { get; private set => SetValue(ref field, value); } = false;

        public void New(TContext context)
        {
            IsInitialized = false;
            OnNew(context);
            IsInitialized = true;
        }

        public void SetData(TData data)
        {
            IsInitialized = false;
            OnSetData(data);
            IsInitialized = true;
        }

        public void GetData(TData data)
        {
            OnGetData(data);
        }

        protected virtual void OnNew(TContext context) { }
        protected abstract void OnSetData(TData data);
        protected abstract void OnGetData(TData data);

        protected void NotifyStatus(string status)
        {
            SendMessage(new StatusMessage { Text = status });
        }

        protected TMessage SendMessage<TMessage>() 
            where TMessage : class, new()
        {
            return SendMessage<TMessage>(new TMessage());
        }

        protected TMessage SendMessage<TMessage>(TMessage msg)
            where TMessage : class
        {
            Messenger.Instance.Send<TMessage>(msg);
            return msg;
        }

        protected void RegisterMesssage<TMessage>(Action<TMessage> handler) 
            where TMessage : class
        {
            Messenger.Instance.Register<TMessage>(handler);
        }
    }
}
