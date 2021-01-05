namespace Telegram.Filters
{
    class PrivateFilter : Filter
    {
        private PrivateFilter() { }

        private static PrivateFilter _instance;

        private static readonly object _lock = new object();

        public override FilterResult ShouldProcess()
        {
            return _update switch
            {
                { Message: { } m } => m.Chat.Type == Bot.Types.Enums.ChatType.Private
                    ? FilterResult.True("private", m.Chat)
                    : FilterResult.False,
                { CallbackQuery: { Message: { } m } } => m.Chat.Type == Bot.Types.Enums.ChatType.Private
                    ? FilterResult.True("private", m.Chat)
                    : FilterResult.False
                ,
                _ => FilterResult.False
            };
        }

        public static PrivateFilter GetInstance()
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new PrivateFilter();
                    }
                }
            }
            return _instance;
        }
    }
}
