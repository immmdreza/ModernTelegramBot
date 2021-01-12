namespace Telegram.Attributes
{
    public class MessageHandlerAttribute : HandlerAttribute
    {
        public MessageHandlerAttribute(int group = 0) : base(group)
        {
        }

        public MessageHandlerAttribute(string filterTag, int group = 0) : base(filterTag, group)
        {
        }
    }
}
