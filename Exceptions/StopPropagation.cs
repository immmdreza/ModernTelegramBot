using System;

namespace Telegram.Exceptions
{
    public class StopPropagation : Exception
    {
        public StopPropagation()
        {
            Console.WriteLine("Handler chaine breaked!");
        }
    }
}
