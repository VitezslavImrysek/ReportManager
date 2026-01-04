using ReportAdmin.Core;

namespace ReportAdmin.App.ViewModels
{
    public abstract class DataEditorVM<TData, TContext> : NotificationObject
        where TData : class
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

        public TData GetData()
        {
            return OnGetData();
        }

        protected abstract void OnNew(TContext context);
        protected abstract void OnSetData(TData data);
        protected abstract TData OnGetData();
    }
}
