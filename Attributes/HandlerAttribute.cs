using System;

namespace Telegram.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class HandlerAttribute : Attribute
    {
        /// <summary>
        /// Pass one of filter tag you created using AddCustomFilter
        /// </summary>
        /// <param name="filterTag"></param>
        public HandlerAttribute(string filterTag, int group = 0)
        {
            FilterTag = filterTag;
            Group = group;
        }

        public HandlerAttribute(int group = 0)
        {
            FilterTag = null;
            Group = group;
        }

        public string FilterTag { get; set; }

        public int Group { get; set; }
    }
}
