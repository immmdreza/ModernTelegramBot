using System.Collections.Generic;
using System.Linq;

namespace Telegram.Filters
{
    public class CommandFilter : Filter
    {
        private readonly List<string> _commands;
        private readonly List<char> _prefixs;
        private readonly bool _cc;


        public CommandFilter(string command, char prefix = '/', bool captureCaption = false)
        {
            _cc = captureCaption;
            _commands = new List<string> { command.ToLower() };
            _prefixs = new List<char> { prefix };
        }

        public CommandFilter(string[] commands, char prefix = '/', bool captureCaption = false)
        {
            _cc = captureCaption;
            _commands = commands.Select(x => x.ToLower()).ToList();
            _prefixs = new List<char> { prefix };
        }

        public CommandFilter(string command, char[] prefixs = null, bool captureCaption = false)
        {
            _cc = captureCaption;
            _commands = new List<string> { command.ToLower() };
            if (prefixs is null)
                _prefixs = new List<char> { '/' };
            else
                _prefixs = prefixs.ToList();
        }

        public CommandFilter(string[] commands, char[] prefixs = null, bool captureCaption = false)
        {
            _cc = captureCaption;
            _commands = commands.Select(x=> x.ToLower()).ToList();

            if (prefixs is null)
                _prefixs = new List<char> { '/' };
            else
                _prefixs = prefixs.ToList();
        }

        public override FilterResult ShouldProcess()
        {
            switch (_update)
            {
                case { Message: { Text: { } t } }:
                    {
                        if (!string.IsNullOrEmpty(t))
                        {
                            if(_prefixs.Any(x=> t[0] == x))
                            {
                                var args = t[1..].ToLower().Split(' ');
                                var cmd = args[0].Replace('@' + BotInfo.Username.ToLower(), "");

                                if(_commands.Any(x=> x == cmd))
                                {
                                    return FilterResult.True("args", args);
                                }
                            }
                        }
                        return FilterResult.False;
                    }
                case { Message: { Caption: { } c } }:
                    {
                        if (_cc)
                        {
                            if (!string.IsNullOrEmpty(c))
                            {
                                if (_prefixs.Any(x => c[0] == x))
                                {
                                    var args = c[1..].ToLower().Split(' ');
                                    var cmd = args[0].Replace('@' + BotInfo.Username, "");

                                    if (_commands.Any(x => x == cmd))
                                    {
                                        return FilterResult.True("command", args);
                                    }
                                }
                            }
                        }
                        return FilterResult.False;
                    }

                default: return FilterResult.False;
            }
        }
    }
}
