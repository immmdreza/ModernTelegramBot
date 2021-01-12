using Telegram.Filters;

namespace Telegram.Attributes.Filters
{
    public class PrivateFilterAttribute : FilterAttribute
    {
        public PrivateFilterAttribute()
        {
            SetFilter(Filter.Private);
        }
    }
}
