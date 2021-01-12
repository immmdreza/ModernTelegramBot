using Telegram.Bot.Types.Enums;

namespace Telegram.Filters
{
    public class GroupFilter : Filter
    {
        private GroupFilter() { }

        private static GroupFilter _instance;

        private static readonly object _lock = new object();

        public override FilterResult ShouldProcess()
        {
            return _update switch
            {
                { Message: { } m } => m.Chat.Type == ChatType.Group || m.Chat.Type == ChatType.Supergroup 
                    ? FilterResult.True("group", m.Chat) 
                    : FilterResult.False,
                { CallbackQuery: { Message: { } m } } => m.Chat.Type == ChatType.Group || m.Chat.Type == ChatType.Supergroup 
                    ? FilterResult.True("group", m.Chat) 
                    : FilterResult.False,
                _ => FilterResult.False
            };
        }

        public static GroupFilter GetInstance()
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new GroupFilter();
                    }
                }
            }
            return _instance;
        }
    }
}
