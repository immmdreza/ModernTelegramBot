using Telegram.Filters;

namespace Telegram.Attributes.Filters
{
    public class CommandFilterAttribute : FilterAttribute
    {
        public CommandFilterAttribute(string command)
        {
            SetFilter(Filter.Command(command));
        }

        public CommandFilterAttribute(string command, char prefix = '/', bool captureCaption = false)
        {
            SetFilter(new CommandFilter(command, prefix, captureCaption));
        }

        public CommandFilterAttribute(string[] commands, char prefix = '/', bool captureCaption = false)
        {
            SetFilter(new CommandFilter(commands, prefix, captureCaption));
        }

        public CommandFilterAttribute(string command, char[] prefixs = null, bool captureCaption = false)
        {
            SetFilter(new CommandFilter(command, prefixs, captureCaption));
        }

        public CommandFilterAttribute(string[] commands, char[] prefixs = null, bool captureCaption = false)
        {
            SetFilter(new CommandFilter(commands, prefixs, captureCaption));
        }
    }
}
