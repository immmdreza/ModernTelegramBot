using System;
using System.Threading.Tasks;

namespace Telegram.Handlers
{
    public class ErorrHandler
    {
        public ErorrHandler(Func<Bot.TelegramBotClient, Exception, Task> func)
        {
            CallBack = func;
        }

        public Func<Bot.TelegramBotClient, Exception, Task> CallBack { get; set; }

    }
}
