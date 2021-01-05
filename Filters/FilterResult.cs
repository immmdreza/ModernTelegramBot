namespace Telegram.Filters
{
    public class FilterResult
    {
        public bool Result { get; set; }

        public dynamic Data { get; set; }

        public string Name { get; set; }


        public static FilterResult True(string name, dynamic data)
        {
            return new FilterResult { Data = data, Name = name, Result = true };
        }

        public static FilterResult True()
        {
            return new FilterResult { Data = null, Name = null, Result = true };
        }

        public static FilterResult False => new FilterResult { Data = null, Name = null, Result = false };

        public static bool operator true(FilterResult result)
        {
            return result.Result;
        }

        public static bool operator false(FilterResult result)
        {
            return result.Result;
        }

        public static bool operator !(FilterResult result)
        {
            return !result.Result;
        }
    }
}
