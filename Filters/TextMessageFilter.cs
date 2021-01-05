using System.Text.RegularExpressions;
using Telegram.Bot.Types;

namespace Telegram.Filters
{
    public class TextMessageFilter: Filter
    {
        public TextMessageFilter(Update update) : base(update)
        {
        }

        private readonly Regex _pattern;

        public TextMessageFilter(Regex pattern = null) : base()
        {
            _pattern = pattern;
        }

        public override FilterResult ShouldProcess()
        {
            switch (_update)
            {
                case { Message: { Text: { } text } }:
                    {
                        if(_pattern != null)
                        {
                            var m = _pattern.Matches(text);
                            if(m.Count > 0)
                            {
                                return FilterResult.True("regex", m);
                            }
                            else
                            {
                                return FilterResult.False;
                            }
                        }
                        else
                        {
                            return FilterResult.True();
                        }
                    }
                default: return FilterResult.False;
            }
        }
    }
}
