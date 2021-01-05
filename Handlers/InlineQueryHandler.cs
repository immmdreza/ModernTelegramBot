using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Filters;

namespace Telegram.Handlers
{
    class InlineQueryHandler : IHandler
    {
        public Filter Filter { get; set; }
        public Func<TelegramBotClient, InlineQuery, Dictionary<string, dynamic>, Task> CallBack { get; set; }
        public UpdateType UpdateType { get; set; }
    }
}
