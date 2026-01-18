using System.Reflection;

namespace ReportAdmin.App.Messages
{
    public sealed class Messenger
    {
        private record MessageListener(WeakReference? Listener, MethodInfo Method);

        private readonly Dictionary<Type, List<MessageListener>> _recipients = [];

        public static Messenger Instance { get; } = new Messenger();

        public void Register<TMessage>(Action<TMessage> handler)
            where TMessage : class
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            var messageType = typeof(TMessage);
            if (!_recipients.TryGetValue(messageType, out var recipients))
            {
                _recipients[messageType] = recipients = [];
            }

            if (handler.Target == null)
            {
                // Static
                recipients.Add(new MessageListener(null, handler.GetMethodInfo()));
            }
            else
            {
                // Instance
                recipients.Add(new MessageListener(new WeakReference(handler.Target), handler.GetMethodInfo()));
            }
        }

        public TMessage Send<TMessage>(TMessage message)
            where TMessage : class
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var messageType = typeof(TMessage);
            if (!_recipients.TryGetValue(messageType, out var recipients))
            {
                return message;
            }

            List<MessageListener>? deadReferences = null;
            foreach (var recipient in recipients)
            {
                var weakReference = recipient.Listener;
                if (weakReference == null)
                {
                    // Static
                    recipient.Method.Invoke(null, [message]);
                }
                else
                {
                    // Instance
                    var target = weakReference.Target;
                    if (target != null)
                    {
                        recipient.Method.Invoke(target, [message]);
                    }
                    else
                    {
                        deadReferences ??= [];
                        deadReferences.Add(recipient);
                    }
                }
            }

            // Clean up dead references
            if (deadReferences != null)
            {
                foreach (var deadReference in deadReferences)
                {
                    recipients.Remove(deadReference);
                }
            }

            return message;
        }
    }
}
