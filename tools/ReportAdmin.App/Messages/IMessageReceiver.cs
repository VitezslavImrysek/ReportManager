namespace ReportAdmin.App.Messages
{
    public interface IMessageReceiver<in T> 
        where T : class
    {
        void Receive(T message);
    }
}
