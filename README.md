# ModernTelegramBot
An updated Telegram.Bot package
First of all: the base of these codes is a Copy of [Telegram.Bot](https://github.com/TelegramBots/Telegram.Bot)

the package is for c# and **.Net Core 3.1 and higher **

## Installation 
You can install this package using GitHub that is avaliable in this repo

The package is also available at nuget.org:
_https://www.nuget.org/packages/ModernTelegramBot/_

## Usage
This package keeps everything that is in [Telegram.Bot](https://github.com/TelegramBots/Telegram.Bot) project, so for basic stuff take a look at it.

### Extra features 
Here is a list of currently add features :
  - Handlers
    - MessageHandler
    - CallBackQueryHandler
    - InlineQueryHandler (not quite)
  - Filters
    - RegexFilter
    - CommandFilter
    - Group & Private Filter
    - ReplyFilter
    - ...
  - Bound Methods (_This methods use a shared static instance of client! So they are not safe to use in multi client apps._)
    - For Message: `ReplyText`
    - For CallBackQuery: `Answer`, `EditMessage`, `ChangeKeys`
    
## Getting Startted Example

```cs
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Filters;
using Telegram.Handlers;

namespace TestMTB
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Create a instance of TelegramBotClient and pass the token you got from @BotFather
            var TelegramBotClient = new TelegramBotClient("YOUR_API_TOKEN");


            // Makin' a MessageHandler
            // First parameter shoud be a callback function like helloFunc here
            // Second parameter is a Filter class to filter incomming updates for this handler
            // Here we filter the results using regex to caputre only messages starting with Hello.
            var myHandler = new MessageHandler(
                helloFunc, 
                Filter.Regex(new Regex(@"^hello", RegexOptions.IgnoreCase))
            );


            // Making another handler for command /start in private chat only.
            // See how you can combine Filters using +
            // And ~ will reverse the Filter so here we don't handle any messages from '1234577'
            var myOtherHandler = new MessageHandler(
                startFunc,
                Filter.Command("start") + Filter.Private + ~new FromUsersFilter(1234577)
            );


            // Pass handlers we just made to the BotClinet.
            TelegramBotClient.AddHandler(myHandler);
            TelegramBotClient.AddHandler(myOtherHandler);


            // Use Dispatcher to start receiving updates and handle them
            // Note that this method blocks the code here!
            await TelegramBotClient.Dispatcher(new UpdateType[] { UpdateType.Message });
        }


        // Callback function for myOtherHandler
        private static async Task startFunc(TelegramBotClient client, Message message, Dictionary<string, dynamic> data)
        {
            // data parameter is a dynamic Dictionary that containes data depending on your filters
            var args = (string[])data["args"]; // When you use CommandFilter you have args in data Dictionary

            // see ReplyText is a extension method to the Message obj.
            _ = await message.ReplyText("Just Started!");
        }


        // Callback function for myHandler
        private static async Task helloFunc(TelegramBotClient client, Message message, Dictionary<string, dynamic> data)
        {
            _ = await message.ReplyText("Hi there!");
        }
    }
}
```

## Using Attributes
*And yes*, you can setup your handlers easy and fast using attributes.

You can add EVERY `public` and `static` `method` in your entry assembly as callback for your handler.

### Here is an example

You should always add a handler attribute then add your filters

Every Filter attribute has a Reverse property that do the same as ~, and reverses the filter

And just that easy you can handle __/start__ Command which is not replied and sent in private chat

```cs
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Attributes;
using Telegram.Attributes.Filters;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Filters;
using Telegram.Handlers;

namespace TestPackage
{
    class Program
    {
        static async Task Main()
        {
            TelegramBotClient bot = new TelegramBotClient("BOT_TOKEN");

            await bot.Dispatcher();
        }


        [MessageHandler]
        [CommandFilter("start")]
        [ReplyFilter(Reverse = true)]
        [PrivateFilter]
        public static async Task CallBack(TelegramBotClient client, Message message, Dictionary<string, dynamic> data)
        {
            await message.ReplyText("OK");
        }
    }
}
```


Current features are almost tested.

This project has many things left to do yet!

    

