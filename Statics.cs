using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram
{
    public static class Statics
    {

        public static IUpdate GetUpdateObj(this Update update)
        {
            return update.Type switch
            {
                UpdateType.Message => update.Message,
                UpdateType.CallbackQuery => update.CallbackQuery,
                UpdateType.InlineQuery => update.InlineQuery,
                UpdateType.ChannelPost => update.ChannelPost,
                UpdateType.ChosenInlineResult => update.ChosenInlineResult,
                UpdateType.EditedChannelPost => update.Message,
                UpdateType.EditedMessage => update.Message,
                UpdateType.Poll => update.Poll,
                UpdateType.PollAnswer => update.PollAnswer,
                UpdateType.PreCheckoutQuery => update.PreCheckoutQuery,
                UpdateType.ShippingQuery => update.ShippingQuery,
                _ => null
            };
        }

        /// <summary>
        /// Get the sender of an update
        /// </summary>
        /// <param name="update"></param>
        /// <returns>User obj</returns>
        public static User GetSender(this Update update)
        {
            switch (update)
            {
                case { CallbackQuery: { } call }:
                    {
                        return call.From;
                    }
                case { ChannelPost: { } chnlpost }:
                    {
                        return chnlpost.From;
                    }
                case { Message: { } m }:
                    {
                        return m.From;
                    }
                case { EditedMessage: { } e }:
                    {
                        return e.From;
                    }
                case { ChosenInlineResult: { } ci }:
                    {
                        return ci.From;
                    }
                case { EditedChannelPost: { } chnlpost }:
                    {
                        return chnlpost.From;
                    }
                case { InlineQuery: { } iq }:
                    {
                        return iq.From;
                    }
                default: return null;
            }
        }
    }
}
