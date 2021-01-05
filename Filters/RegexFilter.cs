using System.Text.RegularExpressions;

namespace Telegram.Filters
{
    public class RegexFilter : Filter
    {
        private readonly Regex _pattern;
        public RegexFilter(Regex pattern)
        {
            _pattern = pattern;
        }

        public override FilterResult ShouldProcess()
        {
            switch (_update)
            {
                case { Message: { Text: { } txt } }:
                    {
                        var m = _pattern.Matches(txt);
                        return m.Count > 0 ? FilterResult.True("regex", m) : FilterResult.False;
                    }
                case { Message: { Caption: { } c } }:
                    {
                        var m = _pattern.Matches(c);
                        
                        return m.Count > 0 ? FilterResult.True("regex", m) : FilterResult.False;
                    }
                case { CallbackQuery: { Data: { } d } }:
                    {
                        var m = _pattern.Matches(d);
                        return m.Count > 0 ? FilterResult.True("regex", m) : FilterResult.False;
                    }
                case { EditedMessage: { Text: { } txt } }:
                    {
                        var m = _pattern.Matches(txt);
                        return m.Count > 0 ? FilterResult.True("regex", m) : FilterResult.False;
                    }
                case { EditedMessage: { Caption: { } c } }:
                    {
                        var m = _pattern.Matches(c);
                        return m.Count > 0 ? FilterResult.True("regex", m) : FilterResult.False;
                    }
                case { InlineQuery: { Query: { } q } }:
                    {
                        var m = _pattern.Matches(q);
                        return m.Count > 0 ? FilterResult.True("regex", m) : FilterResult.False;
                    }
                case { EditedChannelPost: { Text: { } txt } }:
                    {
                        var m = _pattern.Matches(txt);
                        return m.Count > 0 ? FilterResult.True("regex", m) : FilterResult.False;
                    }
                case { EditedChannelPost: { Caption: { } c } }:
                    {
                        var m = _pattern.Matches(c);
                        return m.Count > 0 ? FilterResult.True("regex", m) : FilterResult.False;
                    }
                case { ChannelPost: { Text: { } txt } }:
                    {
                        var m = _pattern.Matches(txt);
                        return m.Count > 0 ? FilterResult.True("regex", m) : FilterResult.False;
                    }
                case { ChannelPost: { Caption: { } c } }:
                    {
                        var m = _pattern.Matches(c);
                        return m.Count > 0 ? FilterResult.True("regex", m) : FilterResult.False;
                    }

                default: return FilterResult.False;
            }
        }
    }
}
