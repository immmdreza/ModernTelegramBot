using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Filters;

namespace Telegram.Handlers
{
    public class MessageHandler : IHandler
    {
        public MessageHandler(Func<TelegramBotClient, Message, Dictionary<string, dynamic>, Task> callBack, Filter filter = null)
        {
            UpdateType = UpdateType.Message;
            CallBack = callBack;
            Filter = filter;
        }

        public Filter Filter { get; set; }
        public Func<TelegramBotClient, Message, Dictionary<string, dynamic>, Task> CallBack { get; set; }
        public UpdateType UpdateType { get; set; }

    }
}
