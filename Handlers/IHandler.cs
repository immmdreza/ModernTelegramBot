using Telegram.Bot.Types.Enums;

namespace Telegram.Handlers
{
    public interface IHandler
    {
        public Filters.Filter Filter { get; set; }

        public UpdateType UpdateType { get; set; }
    }
}
