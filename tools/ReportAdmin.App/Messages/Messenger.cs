namespace ReportAdmin.App.Messages
{
    public sealed class Messenger
    {
        private readonly List<WeakReference> _recipients = [];

        public static Messenger Instance { get; } = new Messenger();

        public void Register<TMessage>(IMessageReceiver<TMessage> recipient)
            where TMessage : class
        {
            if (recipient == null)
            {
                throw new ArgumentNullException(nameof(recipient));
            }

            _recipients.Add(new WeakReference(recipient));
        }

        public TMessage Send<TMessage>()
            where TMessage : class, new()
        {
            return Send(new TMessage());
        }

        public TMessage Send<TMessage>(TMessage message)
            where TMessage : class
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            List<WeakReference>? deadReferences = null;
            foreach (var weakReference in _recipients)
            {
                if (weakReference.Target is IMessageReceiver<TMessage> recipient)
                {
                    recipient.Receive(message);
                }
                else if (!weakReference.IsAlive)
                {
                    deadReferences ??= [];
                    deadReferences.Add(weakReference);
                }
            }

            // Clean up dead references
            if (deadReferences != null)
            {
                foreach (var deadReference in deadReferences)
                {
                    _recipients.Remove(deadReference);
                }
            }

            return message;
        }
    }
}
