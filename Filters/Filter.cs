using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Telegram.Bot.Types;

namespace Telegram.Filters
{
    public class Filter
    {
        protected Update _update;
        protected bool _ok = false;
        protected bool _valid = false;
        protected bool inverse = false;
        protected User BotInfo;

        private readonly List<Filter> _filters;

        public Filter()
        {
        }

        public Filter(Update update)
        {
            _update = update;
        }

        public Filter(List<Filter> filters)
        {
            _filters = filters;
        }

        public void SetUpdate(Update update)
        {
            _update = update;
            if (_filters != null)
            {
                foreach (var f in _filters)
                {
                    if (f is null)
                        continue;

                    f.SetUpdate(update);
                }
            }
        }

        public void SetBotInfo(User user)
        {
            BotInfo = user;
            if (_filters != null)
            {
                foreach (var f in _filters)
                {
                    if (f != null)
                    {
                        f.SetBotInfo(user);
                    }
                }
            }
        }

        public Filter(bool ok, bool valid, List<Filter> filters)
        {
            _update = null;
            _ok = ok;
            _valid = valid;
            _filters = filters;
        }

        public virtual FilterResult ShouldProcess()
        {
            var data = new Dictionary<string, dynamic>();

            foreach(Filter f in _filters)
            {
                if (f is null)
                    continue;

                var p = f.ShouldProcess();
                if (!p && !f.inverse)
                {
                    _ok = false; _valid = true; return FilterResult.False;
                }
                else if(p.Result && f.inverse)
                {
                    _ok = false; _valid = true; return FilterResult.False;
                }

                if(!string.IsNullOrEmpty(p.Name))
                    data.TryAdd(p.Name, p.Data);
            }
            _ok = true;
            _valid = true;
            return FilterResult.True("main", data);
        }

        public static Filter operator +(Filter a, Filter b)
        {
            var all = new List<Filter>();
            if (a != null)
            {
                if (a._filters is null)
                {
                    all.Add(a);
                }
                else
                {
                    all = all.Union(a._filters).ToList();
                }
            }

            if (b != null)
            {
                if (b is null || b._filters is null)
                {
                    all.Add(b);
                }
                else
                {
                    all = all.Union(b._filters).ToList();
                }
            }

            //all = ((a._filters??new List<Filter>()).Union(b._filters??new List<Filter>())).ToList();

            //all.Add(a);
            //all.Add(b);

            return new Filter(all.ToList());
        }

        public static Filter operator -(Filter a, Filter b)
        {
            if(a._filters != null)
            {
                if (a._filters.Count == 0)
                    return new Filter(a._filters);

                var res = new List<Filter>();
                if(b._filters != null)
                {
                    foreach (var f in a._filters)
                    {
                        if(b._filters.Any(x=> x != f))
                        {
                            res.Add(f);
                        }
                    }
                }

                return new Filter(res);
            }
            return new Filter(new List<Filter>());
        }

        public static Filter operator ~(Filter a)
        {
            a.inverse = !a.inverse;
            return a;
        }

        #region Creator

        public static Filter TextMessage(Regex pattern = null)
        {
            return new TextMessageFilter(pattern);
        }

        public static Filter Regex(Regex pattern)
        {
            return new RegexFilter(pattern);
        }

        public static Filter Command(string command, char prefix = '/', bool captureCaption = false)
        {
            return new CommandFilter(command, prefix, captureCaption);
        }


        /// <summary>
        /// Check if the message replied to any message
        /// </summary>
        public static Filter Reply => ReplyFilter.GetInstance();

        /// <summary>
        /// Check if current chat is a private
        /// </summary>
        public static Filter Private => PrivateFilter.GetInstance();

        /// <summary>
        /// Check if current chat is a group or super group
        /// </summary>
        public static Filter Group => GroupFilter.GetInstance();

        #endregion
    }
}
