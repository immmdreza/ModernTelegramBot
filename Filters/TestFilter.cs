using Telegram.Bot.Types;

namespace Telegram.Filters
{
    public class TestFilter : Filter
    {
        private readonly string _text;
        public TestFilter(Update update, string text) : base(update)
        {
            _text = text;
        }

        public override FilterResult ShouldProcess()
        {
            return _text.ToLower() == "ok" ? FilterResult.True() : FilterResult.False;
        }
    }
}
