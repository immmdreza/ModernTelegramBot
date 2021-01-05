using System.Collections.Generic;
using System.Linq;

namespace Telegram.Filters
{
    public class FromUsersFilter : Filter
    {
        private readonly List<int> _userids;
        private readonly List<string> _usernames;

        public FromUsersFilter(string username) : base()
        {
            _usernames = new List<string> { username.Replace("@", "") };
        }

        public FromUsersFilter(int userid) : base()
        {
            _userids = new List<int> { userid };
        }

        public FromUsersFilter(List<int> userids) : base()
        {
            _userids = userids;
        }

        public FromUsersFilter(List<string> usernames) : base()
        {
            List<string> n = new List<string>();
            foreach (string i in usernames)
                n.Add(i.Replace("@", ""));

            _usernames = n;
        }

        public override FilterResult ShouldProcess()
        {
            Bot.Types.User sender = _update.GetSender();
            if (sender is null)
                return FilterResult.False;

            if (_userids != null)
            {
                if (_userids.Any(x => sender.Id == x))
                    return FilterResult.True("sender", sender);
            }
            else
            {
                if (string.IsNullOrEmpty(sender.Username))
                    return FilterResult.False;

                if(_usernames.Any(x => sender.Username == x))
                    return FilterResult.True("sender", sender);
            }
            return FilterResult.False;
        }
    }
}
