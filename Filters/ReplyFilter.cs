namespace Telegram.Filters
{
    class ReplyFilter : Filter
    {
        private ReplyFilter() { }

        private static ReplyFilter _instance;

        private static readonly object _lock = new object();

        public override FilterResult ShouldProcess()
        {
            return _update switch
            {
                { Message: { ReplyToMessage: { } rm } } => FilterResult.True("reply", rm),
                _ => FilterResult.False
            };
        }

        public static ReplyFilter GetInstance()
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new ReplyFilter();
                    }
                }
            }
            return _instance;
        }
    }
}
