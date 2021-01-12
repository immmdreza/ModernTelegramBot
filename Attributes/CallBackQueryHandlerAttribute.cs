namespace Telegram.Attributes
{
    public class CallBackQueryHandlerAttribute : HandlerAttribute
    {
        public CallBackQueryHandlerAttribute(int group = 0) : base(group)
        {
        }

        public CallBackQueryHandlerAttribute(string filterTag, int group = 0) : base(filterTag, group)
        {
        }
    }
}
