using Telegram.Filters;

namespace Telegram.Attributes.Filters
{
    public class GroupFilterAttribute: FilterAttribute
    {
        public GroupFilterAttribute()
        {
            SetFilter(Filter.Group);
        }
    }
}
