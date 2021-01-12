using Telegram.Filters;

namespace Telegram.Attributes.Filters
{
    public class ReplyFilterAttribute : FilterAttribute
    {
        public ReplyFilterAttribute()
        {
            SetFilter(Filter.Reply);
        }
    }
}
