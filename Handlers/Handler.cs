using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram.Handlers
{
    public class Handler
    {
        public Filters.Filter Filter { get; set; }

        public Func<Bot.TelegramBotClient, Update, Dictionary<string, dynamic>, Task> CallBack { get; set; }

        public UpdateType UpdateType { get; set; }
    }
}
