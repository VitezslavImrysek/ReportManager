using ReportManager.Lib.Wpf.Messages;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;
using System.Windows;

namespace ReportManager.Lib.Wpf
{
    public abstract class DataEditorVM<TData> : DataEditorVM<TData, object?>
        where TData : class, new()
    { }

    public abstract class DataEditorVM<TData, TContext> : NotificationObject, IDataValidation
        where TData : class, new()
        where TContext : class?
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

        protected void RegisterMessage<TMessage>(Action<TMessage> handler) 
            where TMessage : class
        {
            Messenger.Instance.Register<TMessage>(handler);
        }

        protected bool Validate()
        {
            var sb = new StringBuilder();

            var isOK = Validate(sb);
            if (!isOK) 
            {
                MessageBox.Show(sb.ToString());
            }

            return isOK;
        }

        protected bool Validate(StringBuilder log)
        {
            var isOK = true;

            var properties = GetType().GetProperties();
            foreach (var property in properties)
            {
                var isRequired = Attribute.IsDefined(property, typeof(RequiredAttribute));
                if (isRequired)
                {
                    var value = property.GetValue(this);
                    if (value == null)
                    {
                        isOK = false;
                        log.AppendLine($"Property '{GetPropertyName(property)}' must be set.");
                    }
                    else if (property.PropertyType == typeof(string))
                    {
                        var str = (string)value;
                        if (string.IsNullOrEmpty(str))
                        {
                            var requiredAttribute = (RequiredAttribute)property.GetCustomAttribute(typeof(RequiredAttribute))!;
                            if (!requiredAttribute.AllowEmptyStrings)
                            {
                                isOK = false;
                                log.AppendLine($"Property '{GetPropertyName(property)}' must be set.");
                            }
                        }
                    }
                }

                var isValidationVM = typeof(IDataValidation).IsAssignableFrom(property.PropertyType);
                if (isValidationVM)
                {
                    // Automatically validate child VMs
                    var value = property.GetValue(this) as IDataValidation;
                    if (value != null)
                    {
                        isOK &= value.Validate(log);
                    }
                }
            }

            isOK &= OnValidate(log);
            return isOK;
        }

        protected virtual bool OnValidate(StringBuilder log) => true;

        #region Private Methods

        private static string GetPropertyName(PropertyInfo property)
        {
            var description = property.GetCustomAttribute(typeof(DescriptionAttribute)) as DescriptionAttribute;
            if (description != null)
            {
                return description.Description;
            }

            return property.Name;
        }

        #endregion

        #region IDataValidation

        bool IDataValidation.Validate(StringBuilder log)
            => Validate(log);

        #endregion
    }
}
