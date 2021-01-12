namespace Telegram.Attributes
{
    public class InlineQueryHandlerAttribute : HandlerAttribute
    {
        public InlineQueryHandlerAttribute(int group = 0) : base(group)
        {
        }

        public InlineQueryHandlerAttribute(string filterTag, int group = 0) : base(filterTag, group)
        {
        }
    }
}
