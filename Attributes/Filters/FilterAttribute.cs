using System;
using Telegram.Filters;

namespace Telegram.Attributes.Filters
{
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class FilterAttribute : Attribute
    {
        private Filter resultFilter;

        public bool Reverse { get; set; }

        public Filter ResultFilter { get => Reverse ? ~resultFilter : resultFilter; }

        protected void SetFilter(Filter filter) => resultFilter = filter;
    }
}
