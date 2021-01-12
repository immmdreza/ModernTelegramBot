using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Attributes;
using Telegram.Attributes.Filters;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Requests;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.Payments;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Exceptions;
using Telegram.Filters;
using Telegram.Handlers;
using File = Telegram.Bot.Types.File;

namespace Telegram.Bot
{
    /// <summary>
    /// A client to use the Telegram Bot API
    /// </summary>
    public class TelegramBotClient : ITelegramBotClient
    {
        /// <inheritdoc/>
        public int BotId { get; }

        private static readonly Update[] EmptyUpdates = { };

        private const string BaseUrl = "https://api.telegram.org/bot";

        private const string BaseFileUrl = "https://api.telegram.org/file/bot";

        private readonly string _baseRequestUrl;

        private readonly string _token;

        private readonly HttpClient _httpClient;

        #region Config Properties

        /// <summary>
        /// Timeout for requests
        /// </summary>
        public TimeSpan Timeout
        {
            get => _httpClient.Timeout;
            set => _httpClient.Timeout = value;
        }

        /// <summary>
        /// Indicates if receiving updates
        /// </summary>
        public bool IsReceiving { get; set; }

        private CancellationTokenSource _receivingCancellationTokenSource;

        /// <summary>
        /// The current message offset
        /// </summary>
        public int MessageOffset { get; set; }

        #endregion Config Properties

        #region Events

        /// <summary>
        /// Occurs before sending a request to API
        /// </summary>
        public event EventHandler<ApiRequestEventArgs> MakingApiRequest;

        /// <summary>
        /// Occurs after receiving the response to an API request
        /// </summary>
        public event EventHandler<ApiResponseEventArgs> ApiResponseReceived;

        /// <summary>
        /// Raises the <see cref="OnUpdate" />, <see cref="OnMessage"/>, <see cref="OnInlineQuery"/>, <see cref="OnInlineResultChosen"/> and <see cref="OnCallbackQuery"/> events.
        /// </summary>
        /// <param name="e">The <see cref="UpdateEventArgs"/> instance containing the event data.</param>
        protected virtual void OnUpdateReceived(UpdateEventArgs e)
        {
            OnUpdate?.Invoke(this, e);

            switch (e.Update.Type)
            {
                case UpdateType.Message:
                    OnMessage?.Invoke(this, e);
                    break;

                case UpdateType.InlineQuery:
                    OnInlineQuery?.Invoke(this, e);
                    break;

                case UpdateType.ChosenInlineResult:
                    OnInlineResultChosen?.Invoke(this, e);
                    break;

                case UpdateType.CallbackQuery:
                    OnCallbackQuery?.Invoke(this, e);
                    break;

                case UpdateType.EditedMessage:
                    OnMessageEdited?.Invoke(this, e);
                    break;
            }
        }

        /// <summary>
        /// Occurs when an <see cref="Update"/> is received.
        /// </summary>
        public event EventHandler<UpdateEventArgs> OnUpdate;

        /// <summary>
        /// Occurs when a <see cref="Message"/> is received.
        /// </summary>
        public event EventHandler<MessageEventArgs> OnMessage;

        /// <summary>
        /// Occurs when <see cref="Message"/> was edited.
        /// </summary>
        public event EventHandler<MessageEventArgs> OnMessageEdited;

        /// <summary>
        /// Occurs when an <see cref="InlineQuery"/> is received.
        /// </summary>
        public event EventHandler<InlineQueryEventArgs> OnInlineQuery;

        /// <summary>
        /// Occurs when a <see cref="ChosenInlineResult"/> is received.
        /// </summary>
        public event EventHandler<ChosenInlineResultEventArgs> OnInlineResultChosen;

        /// <summary>
        /// Occurs when an <see cref="CallbackQuery"/> is received
        /// </summary>
        public event EventHandler<CallbackQueryEventArgs> OnCallbackQuery;

        /// <summary>
        /// Occurs when an error occurs during the background update pooling.
        /// </summary>
        public event EventHandler<ReceiveErrorEventArgs> OnReceiveError;

        /// <summary>
        /// Occurs when an error occurs during the background update pooling.
        /// </summary>
        public event EventHandler<ReceiveGeneralErrorEventArgs> OnReceiveGeneralError;

        #endregion

        /// <summary>
        /// Create a new <see cref="TelegramBotClient"/> instance.
        /// </summary>
        /// <param name="token">API token</param>
        /// <param name="httpClient">A custom <see cref="HttpClient"/></param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="token"/> format is invalid</exception>
        public TelegramBotClient(string token, HttpClient httpClient = null, bool logActions = true)
        {
            this.logActions = logActions;
            _token = token ?? throw new ArgumentNullException(nameof(token));
            string[] parts = _token.Split(':');
            if (parts.Length > 1 && int.TryParse(parts[0], out int id))
            {
                BotId = id;
            }
            else
            {
                throw new ArgumentException(
                    "Invalid format. A valid token looks like \"1234567:4TT8bAc8GHUspu3ERYn-KGcvsvGB9u_n4ddy\".",
                    nameof(token)
                );
            }

            _baseRequestUrl = $"{BaseUrl}{_token}/";
            _httpClient = httpClient ?? new HttpClient();
            Const.TelegramBotClient = this;
            Const.BotInfo = this.GetMeAsync().Result;
        }

        public User BotInfo { get => Const.BotInfo; }

        /// <summary>
        /// Create a new <see cref="TelegramBotClient"/> instance behind a proxy.
        /// </summary>
        /// <param name="token">API token</param>
        /// <param name="webProxy">Use this <see cref="IWebProxy"/> to connect to the API</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="token"/> format is invalid</exception>
        public TelegramBotClient(string token, IWebProxy webProxy, bool logActions = true)
        {
            this.logActions = logActions;
            _token = token ?? throw new ArgumentNullException(nameof(token));
            string[] parts = _token.Split(':');
            if (int.TryParse(parts[0], out int id))
            {
                BotId = id;
            }
            else
            {
                throw new ArgumentException(
                    "Invalid format. A valid token looks like \"1234567:4TT8bAc8GHUspu3ERYn-KGcvsvGB9u_n4ddy\".",
                    nameof(token)
                );
            }

            _baseRequestUrl = $"{BaseUrl}{_token}/";
            HttpClientHandler httpClientHander = new HttpClientHandler
            {
                Proxy = webProxy,
                UseProxy = true
            };
            _httpClient = new HttpClient(httpClientHander);
            Const.TelegramBotClient = this;
        }


        private Dictionary<string, Filter> _customFilters = new Dictionary<string, Filter>();


        public void AddCustomFilter(string tag, Filter filter)
        {
            _customFilters.Add(tag, filter);
        }

        public void RemoveCustomFilter(string tag)
        {
            _customFilters.Remove(tag);
        }


        private int AddAttributeHandlers()
        {
            int foundCount = 0;
            foreach (var item in System.Reflection.Assembly.GetEntryAssembly().GetTypes().Where(x => x.IsClass))
            {
                foreach (var m in item.GetMethods())
                {
                    var allAttr = Attribute.GetCustomAttributes(m);
                    var handlerAttr = (HandlerAttribute)allAttr.FirstOrDefault(x => x is HandlerAttribute);
                    Attribute[] filterAttrs = new Attribute[] { };
                    if (handlerAttr != null)
                        filterAttrs = allAttr.Where(x => x is FilterAttribute).ToArray();
                    else
                        continue;

                    Filter resultFilter = null;
                    if (filterAttrs.Any())
                    {  
                        foreach (var filterAttribute in filterAttrs.Cast<FilterAttribute>())
                        {
                            resultFilter += filterAttribute.ResultFilter;
                        }
                    }

                    if (!string.IsNullOrEmpty(handlerAttr.FilterTag))
                    {
                        if (_customFilters.TryGetValue(handlerAttr.FilterTag, out Filter filter))
                        {
                            resultFilter += filter;
                        }
                    }

                    if(handlerAttr is MessageHandlerAttribute)
                    {
                        var callback = (Func<TelegramBotClient, Message, Dictionary<string, dynamic>, Task>)m.CreateDelegate(
                            typeof(Func<TelegramBotClient, Message, Dictionary<string, dynamic>, Task>));

                        AddHandler(new MessageHandler(callback, resultFilter), handlerAttr.Group);
                        foundCount ++;
                        break;
                    }
                    else if (handlerAttr is CallBackQueryHandlerAttribute)
                    {
                        var callback = (Func<TelegramBotClient, CallbackQuery, Dictionary<string, dynamic>, Task>)m.CreateDelegate(
                            typeof(Func<TelegramBotClient, CallbackQuery, Dictionary<string, dynamic>, Task>));

                        AddHandler(new CallBackQueryHandler(callback, resultFilter), handlerAttr.Group);
                        foundCount++;
                        break;
                    }
                    else if (handlerAttr is InlineQueryHandlerAttribute)
                    {
                        var callback = (Func<TelegramBotClient, InlineQuery, Dictionary<string, dynamic>, Task>)m.CreateDelegate(
                            typeof(Func<TelegramBotClient, InlineQuery, Dictionary<string, dynamic>, Task>));

                        AddHandler(new InlineQueryHandler(callback, resultFilter), handlerAttr.Group);
                        foundCount++;
                        break;
                    }
                    
                }
            }
            return foundCount;
        }

        private Dictionary<int, List<IHandler>> _groupedHandlers = new Dictionary<int, List<IHandler>>();

        public void AddHandler(IHandler handler, int group = 0)
        {
            if (_groupedHandlers.ContainsKey(group))
            {
                _groupedHandlers[group].Add(handler);
            }
            else
            {
                _groupedHandlers.Add(group, new List<IHandler> { handler });
            }

            _groupedHandlers = _groupedHandlers.OrderBy(x => x.Key).ToDictionary(x=> x.Key, x=> x.Value);
        }


        public static void StopPropagation()
        {
            throw new StopPropagation();
        }

        private readonly bool logActions;

        public async Task Dispatcher(
            UpdateType[] allowed = null,
            Func<TelegramBotClient, Exception, Task> ErrorHandler = null, 
            Func<Update, bool> beforeHandle = null,
            CancellationToken token = default, 
            bool resetQueue = false)
        {
            if (IsReceiving)
                throw new Exception("You can't use this while client is receiving.");

            var found = AddAttributeHandlers();
            if (logActions && found > 0)
                Console.WriteLine($"{found} Attributed handlers found!");

            UpdateType[]? allowedUpdates = allowed;
            int messageOffset = 0;
            Update[] emptyUpdates = Array.Empty<Update>();

            if (resetQueue) {
                _ = await MakeRequestAsync(new GetUpdatesRequest()
                {
                    Offset = -1
                }, token).ConfigureAwait(false);
                if(logActions)
                    Console.WriteLine("Queue reset!");
            }

            if(logActions)
                Console.WriteLine($"Getting updates from {BotInfo.Username} ({BotInfo.Id})");
            while (!token.IsCancellationRequested) {
                int timeout = (int)Timeout.TotalSeconds;
                Update[] updates = emptyUpdates;
                try {
                    updates = await MakeRequestAsync(new GetUpdatesRequest()
                    {
                        Offset = messageOffset,
                        Timeout = timeout,
                        AllowedUpdates = allowedUpdates,
                    }, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) {
                    // Ignore
                }
                catch (Exception ex) {
                    if (ErrorHandler != null)
                        _ = ErrorHandler(this, ex);
                }

                foreach (Update update in updates) {
                    _ = Task.Run(async () => {
                        try {
                            if(beforeHandle != null) {
                                if (!beforeHandle(update))
                                    return;
                            }

                            var updatObj = update.GetUpdateObj();

                            foreach (var value in _groupedHandlers.Values) {
                                try {
                                    foreach (var item in value) {
                                        if (item.UpdateType == update.Type) {
                                            if (item.Filter == null) {
                                                //await item.CallBack(this, updatObj, null);
                                                await Director(item, updatObj, null);
                                                break;
                                            }

                                            item.Filter.SetUpdate(update);
                                            item.Filter.SetBotInfo(BotInfo);
                                            Filters.FilterResult p = item.Filter.ShouldProcess();
                                            if (p) {
                                                Dictionary<string, dynamic> data = new Dictionary<string, dynamic>();
                                                if (p.Data is Dictionary<string, dynamic>) {
                                                    data = p.Data;
                                                }
                                                else {
                                                    data.Add(p.Name, p.Data);
                                                }

                                                // await item.CallBack(this, updatObj, data);
                                                await Director(item, updatObj, data);
                                                break;
                                            }
                                        }
                                    }
                                }
                                catch (StopPropagation)
                                {
                                    if(logActions)
                                        Console.WriteLine("Handler chaine breaked!");
                                    break;
                                }
                                catch { throw; }
                            }
                        }
                        catch(Exception e) {
                            if (ErrorHandler != null)
                                _ = ErrorHandler(this, e);
                        }
                    }, token);

                    messageOffset = update.Id + 1;
                }
            }
        }

        private async Task Director(IHandler handler, IUpdate update, Dictionary<string, dynamic> data)
        {
            if(update is Message m)
            {
                var h = (MessageHandler)handler;
                await h.CallBack(this, m, data);
            }
            else if(update is CallbackQuery c)
            {
                var h = (CallBackQueryHandler)handler;
                await h.CallBack(this, c, data);
            }
            else if (update is InlineQuery i)
            {
                var h = (InlineQueryHandler)handler;
                await h.CallBack(this, i, data);
            }
            else
            {
                var h = (DefualtHandler)handler;
                await h.CallBack(this, update, data);
            }
        }

        public static void SetDefualtClient(TelegramBotClient client)
        {
            Const.TelegramBotClient = client;
        }

        #region Helpers

        /// <inheritdoc />
        public async Task<TResponse> MakeRequestAsync<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            string url = _baseRequestUrl + request.MethodName;

            HttpRequestMessage httpRequest = new HttpRequestMessage(request.Method, url)
            {
                Content = request.ToHttpContent()
            };

            ApiRequestEventArgs reqDataArgs = new ApiRequestEventArgs
            {
                MethodName = request.MethodName,
                HttpContent = httpRequest.Content,
            };
            MakingApiRequest?.Invoke(this, reqDataArgs);

            HttpResponseMessage httpResponse;
            try
            {
                httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (TaskCanceledException e)
            {
                if (cancellationToken.IsCancellationRequested)
                    throw;

                throw new ApiRequestException("Request timed out", 408, e);
            }

            // required since user might be able to set new status code using following event arg
            HttpStatusCode actualResponseStatusCode = httpResponse.StatusCode;
            string responseJson = await httpResponse.Content.ReadAsStringAsync()
                .ConfigureAwait(false);

            ApiResponseReceived?.Invoke(this, new ApiResponseEventArgs
            {
                ResponseMessage = httpResponse,
                ApiRequestEventArgs = reqDataArgs
            });

            switch (actualResponseStatusCode)
            {
                case HttpStatusCode.OK:
                    break;
                case HttpStatusCode.Unauthorized:
                case HttpStatusCode.BadRequest when !string.IsNullOrWhiteSpace(responseJson):
                case HttpStatusCode.Forbidden when !string.IsNullOrWhiteSpace(responseJson):
                case HttpStatusCode.Conflict when !string.IsNullOrWhiteSpace(responseJson):
                    // Do NOT throw here, an ApiRequestException will be thrown next
                    break;
                default:
                    httpResponse.EnsureSuccessStatusCode();
                    break;
            }

            ApiResponse<TResponse> apiResponse =
                JsonConvert.DeserializeObject<ApiResponse<TResponse>>(responseJson)
                ?? new ApiResponse<TResponse> // ToDo is required? unit test
                {
                    Ok = false,
                    Description = "No response received"
                };

            if (!apiResponse.Ok)
                throw ApiExceptionParser.Parse(apiResponse);

            return apiResponse.Result;
        }

        /// <summary>
        /// Test the API token
        /// </summary>
        /// <returns><c>true</c> if token is valid</returns>
        public async Task<bool> TestApiAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await GetMeAsync(cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (ApiRequestException e)
                when (e.ErrorCode == 401)
            {
                return false;
            }
        }

        /// <summary>
        /// Start update receiving
        /// </summary>
        /// <param name="allowedUpdates">List the types of updates you want your bot to receive.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <exception cref="ApiRequestException"> Thrown if token is invalid</exception>
        public void StartReceiving(UpdateType[] allowedUpdates = null,
                                   CancellationToken cancellationToken = default)
        {
            _receivingCancellationTokenSource = new CancellationTokenSource();

            cancellationToken.Register(() => _receivingCancellationTokenSource.Cancel());

            ReceiveAsync(allowedUpdates, _receivingCancellationTokenSource.Token);
        }

#pragma warning disable AsyncFixer03 // Avoid fire & forget async void methods
        private async void ReceiveAsync(
            UpdateType[] allowedUpdates,
            CancellationToken cancellationToken = default)
        {
            IsReceiving = true;
            while (!cancellationToken.IsCancellationRequested)
            {
                int timeout = Convert.ToInt32(Timeout.TotalSeconds);
                Update[] updates = EmptyUpdates;

                try
                {
                    updates = await GetUpdatesAsync(
                        MessageOffset,
                        timeout: timeout,
                        allowedUpdates: allowedUpdates,
                        cancellationToken: cancellationToken
                    ).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
                catch (ApiRequestException apiException)
                {
                    OnReceiveError?.Invoke(this, apiException);
                }
                catch (Exception generalException)
                {
                    OnReceiveGeneralError?.Invoke(this, generalException);
                }

                try
                {
                    foreach (Update update in updates)
                    {
                        OnUpdateReceived(new UpdateEventArgs(update));
                        MessageOffset = update.Id + 1;
                    }
                }
                catch
                {
                    IsReceiving = false;
                    throw;
                }
            }

            IsReceiving = false;
        }
#pragma warning restore AsyncFixer03 // Avoid fire & forget async void methods

        /// <summary>
        /// Stop update receiving
        /// </summary>
        public void StopReceiving()
        {
            try
            {
                _receivingCancellationTokenSource.Cancel();
            }
            catch (WebException)
            {
            }
            catch (TaskCanceledException)
            {
            }
        }

        #endregion Helpers

        #region Getting updates

        /// <inheritdoc />
        public Task<Update[]> GetUpdatesAsync(
            int offset = default,
            int limit = default,
            int timeout = default,
            IEnumerable<UpdateType> allowedUpdates = default,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new GetUpdatesRequest
            {
                Offset = offset,
                Limit = limit,
                Timeout = timeout,
                AllowedUpdates = allowedUpdates
            }, cancellationToken);
        }

        /// <inheritdoc />
        public Task SetWebhookAsync(
            string url,
            InputFileStream certificate = default,
            int maxConnections = default,
            IEnumerable<UpdateType> allowedUpdates = default,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new SetWebhookRequest(url, certificate)
            {
                MaxConnections = maxConnections,
                AllowedUpdates = allowedUpdates
            }, cancellationToken);
        }

        /// <inheritdoc />
        public Task DeleteWebhookAsync(CancellationToken cancellationToken = default)
        {
            return MakeRequestAsync(new DeleteWebhookRequest(), cancellationToken);
        }

        /// <inheritdoc />
        public Task<WebhookInfo> GetWebhookInfoAsync(CancellationToken cancellationToken = default)
        {
            return MakeRequestAsync(new GetWebhookInfoRequest(), cancellationToken);
        }

        #endregion Getting updates

        #region Available methods

        /// <inheritdoc />
        public Task<User> GetMeAsync(CancellationToken cancellationToken = default)
        {
            return MakeRequestAsync(new GetMeRequest(), cancellationToken);
        }

        /// <inheritdoc />
        public Task<Message> SendTextMessageAsync(
            ChatId chatId,
            string text,
            ParseMode parseMode = default,
            bool disableWebPagePreview = default,
            bool disableNotification = default,
            int replyToMessageId = default,
            IReplyMarkup replyMarkup = default,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new SendMessageRequest(chatId, text)
            {
                ParseMode = parseMode,
                DisableWebPagePreview = disableWebPagePreview,
                DisableNotification = disableNotification,
                ReplyToMessageId = replyToMessageId,
                ReplyMarkup = replyMarkup
            }, cancellationToken);
        }

        /// <inheritdoc />
        public Task<Message> ForwardMessageAsync(
            ChatId chatId,
            ChatId fromChatId,
            int messageId,
            bool disableNotification = default,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new ForwardMessageRequest(chatId, fromChatId, messageId)
            {
                DisableNotification = disableNotification
            }, cancellationToken);
        }

        /// <inheritdoc />
        public Task<Message> SendPhotoAsync(
            ChatId chatId,
            InputOnlineFile photo,
            string caption = default,
            ParseMode parseMode = default,
            bool disableNotification = default,
            int replyToMessageId = default,
            IReplyMarkup replyMarkup = default,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new SendPhotoRequest(chatId, photo)
            {
                Caption = caption,
                ParseMode = parseMode,
                ReplyToMessageId = replyToMessageId,
                DisableNotification = disableNotification,
                ReplyMarkup = replyMarkup
            }, cancellationToken);
        }

        /// <inheritdoc />
        public Task<Message> SendAudioAsync(
            ChatId chatId,
            InputOnlineFile audio,
            string caption = default,
            ParseMode parseMode = default,
            int duration = default,
            string performer = default,
            string title = default,
            bool disableNotification = default,
            int replyToMessageId = default,
            IReplyMarkup replyMarkup = default,
            CancellationToken cancellationToken = default,
            InputMedia thumb = default
        )
        {
            return MakeRequestAsync(new SendAudioRequest(chatId, audio)
            {
                Caption = caption,
                ParseMode = parseMode,
                Duration = duration,
                Performer = performer,
                Title = title,
                Thumb = thumb,
                DisableNotification = disableNotification,
                ReplyToMessageId = replyToMessageId,
                ReplyMarkup = replyMarkup
            }, cancellationToken);
        }

        /// <inheritdoc />
        public Task<Message> SendDocumentAsync(
            ChatId chatId,
            InputOnlineFile document,
            string caption = default,
            ParseMode parseMode = default,
            bool disableNotification = default,
            int replyToMessageId = default,
            IReplyMarkup replyMarkup = default,
            CancellationToken cancellationToken = default,
            InputMedia thumb = default
        )
        {
            return MakeRequestAsync(new SendDocumentRequest(chatId, document)
            {
                Caption = caption,
                Thumb = thumb,
                ParseMode = parseMode,
                DisableNotification = disableNotification,
                ReplyToMessageId = replyToMessageId,
                ReplyMarkup = replyMarkup
            }, cancellationToken);
        }

        /// <inheritdoc />
        public Task<Message> SendStickerAsync(
            ChatId chatId,
            InputOnlineFile sticker,
            bool disableNotification = default,
            int replyToMessageId = default,
            IReplyMarkup replyMarkup = default,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new SendStickerRequest(chatId, sticker)
            {
                DisableNotification = disableNotification,
                ReplyToMessageId = replyToMessageId,
                ReplyMarkup = replyMarkup
            }, cancellationToken);
        }

        /// <inheritdoc />
        public Task<Message> SendVideoAsync(
            ChatId chatId,
            InputOnlineFile video,
            int duration = default,
            int width = default,
            int height = default,
            string caption = default,
            ParseMode parseMode = default,
            bool supportsStreaming = default,
            bool disableNotification = default,
            int replyToMessageId = default,
            IReplyMarkup replyMarkup = default,
            CancellationToken cancellationToken = default,
            InputMedia thumb = default
        )
        {
            return MakeRequestAsync(new SendVideoRequest(chatId, video)
            {
                Duration = duration,
                Width = width,
                Height = height,
                Thumb = thumb,
                Caption = caption,
                ParseMode = parseMode,
                SupportsStreaming = supportsStreaming,
                DisableNotification = disableNotification,
                ReplyToMessageId = replyToMessageId,
                ReplyMarkup = replyMarkup
            }, cancellationToken);
        }

        /// <inheritdoc />
        public Task<Message> SendAnimationAsync(
            ChatId chatId,
            InputOnlineFile animation,
            int duration = default,
            int width = default,
            int height = default,
            InputMedia thumb = default,
            string caption = default,
            ParseMode parseMode = default,
            bool disableNotification = default,
            int replyToMessageId = default,
            IReplyMarkup replyMarkup = default,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new SendAnimationRequest(chatId, animation)
            {
                Duration = duration,
                Width = width,
                Height = height,
                Thumb = thumb,
                Caption = caption,
                ParseMode = parseMode,
                DisableNotification = disableNotification,
                ReplyToMessageId = replyToMessageId,
                ReplyMarkup = replyMarkup,
            }, cancellationToken);
        }

        /// <inheritdoc />
        public Task<Message> SendVoiceAsync(
            ChatId chatId,
            InputOnlineFile voice,
            string caption = default,
            ParseMode parseMode = default,
            int duration = default,
            bool disableNotification = default,
            int replyToMessageId = default,
            IReplyMarkup replyMarkup = default,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new SendVoiceRequest(chatId, voice)
            {
                Caption = caption,
                ParseMode = parseMode,
                Duration = duration,
                DisableNotification = disableNotification,
                ReplyToMessageId = replyToMessageId,
                ReplyMarkup = replyMarkup
            }, cancellationToken);
        }

        /// <inheritdoc />
        public Task<Message> SendVideoNoteAsync(
            ChatId chatId,
            InputTelegramFile videoNote,
            int duration = default,
            int length = default,
            bool disableNotification = default,
            int replyToMessageId = default,
            IReplyMarkup replyMarkup = default,
            CancellationToken cancellationToken = default,
            InputMedia thumb = default
        )
        {
            return MakeRequestAsync(new SendVideoNoteRequest(chatId, videoNote)
            {
                Duration = duration,
                Length = length,
                Thumb = thumb,
                DisableNotification = disableNotification,
                ReplyToMessageId = replyToMessageId,
                ReplyMarkup = replyMarkup
            }, cancellationToken);
        }

        /// <inheritdoc />
        [Obsolete("Use the other overload of this method instead. Only photo and video input types are allowed.")]
        public Task<Message[]> SendMediaGroupAsync(
            ChatId chatId,
            IEnumerable<InputMediaBase> media,
            bool disableNotification = default,
            int replyToMessageId = default,
            CancellationToken cancellationToken = default
        )
        {
            IAlbumInputMedia[] inputMedia = media
                .Select(m => m as IAlbumInputMedia)
                .Where(m => m != null)
                .ToArray();
            return MakeRequestAsync(new SendMediaGroupRequest(chatId, inputMedia)
            {
                DisableNotification = disableNotification,
                ReplyToMessageId = replyToMessageId,
            }, cancellationToken);
        }

        /// <inheritdoc />
        public Task<Message[]> SendMediaGroupAsync(
            IEnumerable<IAlbumInputMedia> inputMedia,
            ChatId chatId,
            bool disableNotification = default,
            int replyToMessageId = default,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new SendMediaGroupRequest(chatId, inputMedia)
            {
                DisableNotification = disableNotification,
                ReplyToMessageId = replyToMessageId,
            }, cancellationToken);
        }

        /// <inheritdoc />
        public Task<Message> SendLocationAsync(
            ChatId chatId,
            float latitude,
            float longitude,
            int livePeriod = default,
            bool disableNotification = default,
            int replyToMessageId = default,
            IReplyMarkup replyMarkup = default,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new SendLocationRequest(chatId, latitude, longitude)
            {
                LivePeriod = livePeriod,
                DisableNotification = disableNotification,
                ReplyToMessageId = replyToMessageId,
                ReplyMarkup = replyMarkup
            }, cancellationToken);
        }

        /// <inheritdoc />
        public Task<Message> SendVenueAsync(
            ChatId chatId,
            float latitude,
            float longitude,
            string title,
            string address,
            string foursquareId = default,
            bool disableNotification = default,
            int replyToMessageId = default,
            IReplyMarkup replyMarkup = default,
            CancellationToken cancellationToken = default,
            string foursquareType = default
        )
        {
            return MakeRequestAsync(new SendVenueRequest(chatId, latitude, longitude, title, address)
            {
                FoursquareId = foursquareId,
                FoursquareType = foursquareType,
                DisableNotification = disableNotification,
                ReplyToMessageId = replyToMessageId,
                ReplyMarkup = replyMarkup
            }, cancellationToken);
        }

        /// <inheritdoc />
        public Task<Message> SendContactAsync(
            ChatId chatId,
            string phoneNumber,
            string firstName,
            string lastName = default,
            bool disableNotification = default,
            int replyToMessageId = default,
            IReplyMarkup replyMarkup = default,
            CancellationToken cancellationToken = default,
            string vCard = default
        )
        {
            return MakeRequestAsync(new SendContactRequest(chatId, phoneNumber, firstName)
            {
                LastName = lastName,
                Vcard = vCard,
                DisableNotification = disableNotification,
                ReplyToMessageId = replyToMessageId,
                ReplyMarkup = replyMarkup
            }, cancellationToken);
        }

        /// <inheritdoc />
        public Task<Message> SendPollAsync(
            ChatId chatId,
            string question,
            IEnumerable<string> options,
            bool disableNotification = default,
            int replyToMessageId = default,
            IReplyMarkup replyMarkup = default,
            CancellationToken cancellationToken = default,
            bool? isAnonymous = default,
            PollType? type = default,
            bool? allowsMultipleAnswers = default,
            int? correctOptionId = default,
            bool? isClosed = default,
            string explanation = default,
            ParseMode explanationParseMode = default,
            int? openPeriod = default,
            DateTime? closeDate = default
        )
        {
            return MakeRequestAsync(new SendPollRequest(chatId, question, options)
            {
                DisableNotification = disableNotification,
                ReplyToMessageId = replyToMessageId,
                ReplyMarkup = replyMarkup,
                IsAnonymous = isAnonymous,
                Type = type,
                AllowsMultipleAnswers = allowsMultipleAnswers,
                CorrectOptionId = correctOptionId,
                IsClosed = isClosed,
                OpenPeriod = openPeriod,
                CloseDate = closeDate,
                Explanation = explanation,
                ExplanationParseMode = explanationParseMode
            }, cancellationToken);
        }

        /// <inheritdoc />
        public Task<Message> SendDiceAsync(
            ChatId chatId,
            bool disableNotification = default,
            int replyToMessageId = default,
            IReplyMarkup replyMarkup = default,
            CancellationToken cancellationToken = default,
            Emoji? emoji = default)
        {
            return MakeRequestAsync(
new SendDiceRequest(chatId)
{
DisableNotification = disableNotification,
ReplyToMessageId = replyToMessageId,
ReplyMarkup = replyMarkup,
Emoji = emoji
},
cancellationToken
);
        }

        /// <inheritdoc />
        public Task SendChatActionAsync(
            ChatId chatId,
            ChatAction chatAction,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new SendChatActionRequest(chatId, chatAction), cancellationToken);
        }

        /// <inheritdoc />
        public Task<UserProfilePhotos> GetUserProfilePhotosAsync(
            int userId,
            int offset = default,
            int limit = default,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new GetUserProfilePhotosRequest(userId)
            {
                Offset = offset,
                Limit = limit
            }, cancellationToken);
        }

        /// <inheritdoc />
        public Task<File> GetFileAsync(
            string fileId,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new GetFileRequest(fileId), cancellationToken);
        }

        /// <inheritdoc />
        [Obsolete("This method will be removed in next major release. Use its overload instead.")]
        public async Task<Stream> DownloadFileAsync(
            string filePath,
            CancellationToken cancellationToken = default
        )
        {
            MemoryStream stream = new MemoryStream();
            await DownloadFileAsync(filePath, stream, cancellationToken)
                .ConfigureAwait(false);
            return stream;
        }

        /// <inheritdoc />
        public async Task DownloadFileAsync(
            string filePath,
            Stream destination,
            CancellationToken cancellationToken = default
        )
        {
            if (string.IsNullOrWhiteSpace(filePath) || filePath.Length < 2)
            {
                throw new ArgumentException("Invalid file path", nameof(filePath));
            }

            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            Uri fileUri = new Uri($"{BaseFileUrl}{_token}/{filePath}");

            HttpResponseMessage response = await _httpClient
                .GetAsync(fileUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            using (response)
            {
                await response.Content.CopyToAsync(destination)
                    .ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task<File> GetInfoAndDownloadFileAsync(
            string fileId,
            Stream destination,
            CancellationToken cancellationToken = default
        )
        {
            File file = await GetFileAsync(fileId, cancellationToken)
                .ConfigureAwait(false);

            await DownloadFileAsync(file.FilePath, destination, cancellationToken)
                .ConfigureAwait(false);

            return file;
        }

        /// <inheritdoc />
        public Task KickChatMemberAsync(
            ChatId chatId,
            int userId,
            DateTime untilDate = default,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new KickChatMemberRequest(chatId, userId)
            {
                UntilDate = untilDate
            }, cancellationToken);
        }

        /// <inheritdoc />
        public Task LeaveChatAsync(
            ChatId chatId,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new LeaveChatRequest(chatId), cancellationToken);
        }

        /// <inheritdoc />
        public Task UnbanChatMemberAsync(
            ChatId chatId,
            int userId,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new UnbanChatMemberRequest(chatId, userId), cancellationToken);
        }

        /// <inheritdoc />
        public Task<Chat> GetChatAsync(
            ChatId chatId,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new GetChatRequest(chatId), cancellationToken);
        }

        /// <inheritdoc />
        public Task<ChatMember[]> GetChatAdministratorsAsync(
            ChatId chatId,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new GetChatAdministratorsRequest(chatId), cancellationToken);
        }

        /// <inheritdoc />
        public Task<int> GetChatMembersCountAsync(
            ChatId chatId,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new GetChatMembersCountRequest(chatId), cancellationToken);
        }

        /// <inheritdoc />
        public Task<ChatMember> GetChatMemberAsync(
            ChatId chatId,
            int userId,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new GetChatMemberRequest(chatId, userId), cancellationToken);
        }

        /// <inheritdoc />
        public Task AnswerCallbackQueryAsync(
            string callbackQueryId,
            string text = default,
            bool showAlert = default,
            string url = default,
            int cacheTime = default,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new AnswerCallbackQueryRequest(callbackQueryId)
            {
                Text = text,
                ShowAlert = showAlert,
                Url = url,
                CacheTime = cacheTime
            }, cancellationToken);
        }

        /// <inheritdoc />
        public Task RestrictChatMemberAsync(
            ChatId chatId,
            int userId,
            ChatPermissions permissions,
            DateTime untilDate = default,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(
                 new RestrictChatMemberRequest(chatId, userId, permissions)
                 {
                     UntilDate = untilDate
                 },
                 cancellationToken);
        }

        /// <inheritdoc />
        public Task PromoteChatMemberAsync(
            ChatId chatId,
            int userId,
            bool? canChangeInfo = default,
            bool? canPostMessages = default,
            bool? canEditMessages = default,
            bool? canDeleteMessages = default,
            bool? canInviteUsers = default,
            bool? canRestrictMembers = default,
            bool? canPinMessages = default,
            bool? canPromoteMembers = default,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new PromoteChatMemberRequest(chatId, userId)
            {
                CanChangeInfo = canChangeInfo,
                CanPostMessages = canPostMessages,
                CanEditMessages = canEditMessages,
                CanDeleteMessages = canDeleteMessages,
                CanInviteUsers = canInviteUsers,
                CanRestrictMembers = canRestrictMembers,
                CanPinMessages = canPinMessages,
                CanPromoteMembers = canPromoteMembers
            }, cancellationToken);
        }

        /// <inheritdoc />
        public Task SetChatAdministratorCustomTitleAsync(
            ChatId chatId,
            int userId,
            string customTitle,
            CancellationToken cancellationToken = default)
        {
            return MakeRequestAsync(
                           new SetChatAdministratorCustomTitleRequest(chatId, userId, customTitle),
                           cancellationToken);
        }

        /// <inheritdoc />
        public Task SetChatPermissionsAsync(
            ChatId chatId,
            ChatPermissions permissions,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new SetChatPermissionsRequest(chatId, permissions), cancellationToken);
        }

        /// <inheritdoc />
        public Task<BotCommand[]> GetMyCommandsAsync(CancellationToken cancellationToken = default)
        {
            return MakeRequestAsync(new GetMyCommandsRequest(), cancellationToken);
        }

        /// <inheritdoc />
        public Task SetMyCommandsAsync(
            IEnumerable<BotCommand> commands,
            CancellationToken cancellationToken = default)
        {
            return MakeRequestAsync(new SetMyCommandsRequest(commands), cancellationToken);
        }

        /// <inheritdoc />
        public Task<Message> StopMessageLiveLocationAsync(
            ChatId chatId,
            int messageId,
            InlineKeyboardMarkup replyMarkup = default,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new StopMessageLiveLocationRequest(chatId, messageId)
            {
                ReplyMarkup = replyMarkup
            }, cancellationToken);
        }

        /// <inheritdoc />
        public Task StopMessageLiveLocationAsync(
            string inlineMessageId,
            InlineKeyboardMarkup replyMarkup = default,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new StopInlineMessageLiveLocationRequest(inlineMessageId)
            {
                ReplyMarkup = replyMarkup
            }, cancellationToken);
        }

        #endregion Available methods

        #region Updating messages

        /// <inheritdoc />
        public Task<Message> EditMessageTextAsync(
            ChatId chatId,
            int messageId,
            string text,
            ParseMode parseMode = default,
            bool disableWebPagePreview = default,
            InlineKeyboardMarkup replyMarkup = default,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new EditMessageTextRequest(chatId, messageId, text)
            {
                ParseMode = parseMode,
                DisableWebPagePreview = disableWebPagePreview,
                ReplyMarkup = replyMarkup
            }, cancellationToken);
        }

        /// <inheritdoc />
        public Task EditMessageTextAsync(
            string inlineMessageId,
            string text,
            ParseMode parseMode = default,
            bool disableWebPagePreview = default,
            InlineKeyboardMarkup replyMarkup = default,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new EditInlineMessageTextRequest(inlineMessageId, text)
            {
                DisableWebPagePreview = disableWebPagePreview,
                ReplyMarkup = replyMarkup,
                ParseMode = parseMode
            }, cancellationToken);
        }

        /// <inheritdoc />
        public Task<Message> EditMessageCaptionAsync(
            ChatId chatId,
            int messageId,
            string caption,
            InlineKeyboardMarkup replyMarkup = default,
            CancellationToken cancellationToken = default,
            ParseMode parseMode = default
        )
        {
            return MakeRequestAsync(new EditMessageCaptionRequest(chatId, messageId, caption)
            {
                ParseMode = parseMode,
                ReplyMarkup = replyMarkup
            }, cancellationToken);
        }

        /// <inheritdoc />
        public Task EditMessageCaptionAsync(
            string inlineMessageId,
            string caption,
            InlineKeyboardMarkup replyMarkup = default,
            CancellationToken cancellationToken = default,
            ParseMode parseMode = default
        )
        {
            return MakeRequestAsync(new EditInlineMessageCaptionRequest(inlineMessageId, caption)
            {
                ParseMode = parseMode,
                ReplyMarkup = replyMarkup
            }, cancellationToken);
        }

        /// <inheritdoc />
        public Task<Message> EditMessageMediaAsync(
            ChatId chatId,
            int messageId,
            InputMediaBase media,
            InlineKeyboardMarkup replyMarkup = default,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new EditMessageMediaRequest(chatId, messageId, media)
            {
                ReplyMarkup = replyMarkup
            }, cancellationToken);
        }

        /// <inheritdoc />
        public Task EditMessageMediaAsync(
            string inlineMessageId,
            InputMediaBase media,
            InlineKeyboardMarkup replyMarkup = default,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new EditInlineMessageMediaRequest(inlineMessageId, media)
            {
                ReplyMarkup = replyMarkup
            }, cancellationToken);
        }

        /// <inheritdoc />
        public Task<Message> EditMessageReplyMarkupAsync(
            ChatId chatId,
            int messageId,
            InlineKeyboardMarkup replyMarkup = default,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(
                 new EditMessageReplyMarkupRequest(chatId, messageId, replyMarkup),
                 cancellationToken);
        }

        /// <inheritdoc />
        public Task EditMessageReplyMarkupAsync(
            string inlineMessageId,
            InlineKeyboardMarkup replyMarkup = default,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(
                 new EditInlineMessageReplyMarkupRequest(inlineMessageId, replyMarkup),
                 cancellationToken);
        }

        /// <inheritdoc />
        public Task<Message> EditMessageLiveLocationAsync(
            ChatId chatId,
            int messageId,
            float latitude,
            float longitude,
            InlineKeyboardMarkup replyMarkup = default,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new EditMessageLiveLocationRequest(chatId, messageId, latitude, longitude)
            {
                ReplyMarkup = replyMarkup
            }, cancellationToken);
        }

        /// <inheritdoc />
        public Task EditMessageLiveLocationAsync(
            string inlineMessageId,
            float latitude,
            float longitude,
            InlineKeyboardMarkup replyMarkup = default,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new EditInlineMessageLiveLocationRequest(inlineMessageId, latitude, longitude)
            {
                ReplyMarkup = replyMarkup
            }, cancellationToken);
        }

        /// <inheritdoc />
        public Task<Poll> StopPollAsync(
            ChatId chatId,
            int messageId,
            InlineKeyboardMarkup replyMarkup = default,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new StopPollRequest(chatId, messageId)
            {
                ReplyMarkup = replyMarkup
            }, cancellationToken);
        }

        /// <inheritdoc />
        public Task DeleteMessageAsync(
            ChatId chatId,
            int messageId,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new DeleteMessageRequest(chatId, messageId), cancellationToken);
        }

        #endregion Updating messages

        #region Inline mode

        /// <inheritdoc />
        public Task AnswerInlineQueryAsync(
            string inlineQueryId,
            IEnumerable<InlineQueryResultBase> results,
            int? cacheTime = default,
            bool isPersonal = default,
            string nextOffset = default,
            string switchPmText = default,
            string switchPmParameter = default,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new AnswerInlineQueryRequest(inlineQueryId, results)
            {
                CacheTime = cacheTime,
                IsPersonal = isPersonal,
                NextOffset = nextOffset,
                SwitchPmText = switchPmText,
                SwitchPmParameter = switchPmParameter
            }, cancellationToken);
        }

        #endregion Inline mode

        #region Payments

        /// <inheritdoc />
        public Task<Message> SendInvoiceAsync(
            int chatId,
            string title,
            string description,
            string payload,
            string providerToken,
            string startParameter,
            string currency,
            IEnumerable<LabeledPrice> prices,
            string providerData = default,
            string photoUrl = default,
            int photoSize = default,
            int photoWidth = default,
            int photoHeight = default,
            bool needName = default,
            bool needPhoneNumber = default,
            bool needEmail = default,
            bool needShippingAddress = default,
            bool isFlexible = default,
            bool disableNotification = default,
            int replyToMessageId = default,
            InlineKeyboardMarkup replyMarkup = default,
            CancellationToken cancellationToken = default,
            bool sendPhoneNumberToProvider = default,
            bool sendEmailToProvider = default
        )
        {
            return MakeRequestAsync(new SendInvoiceRequest(
                 chatId,
                 title,
                 description,
                 payload,
                 providerToken,
                 startParameter,
                 currency,
                 // ReSharper disable once PossibleMultipleEnumeration
                 prices
             )
            {
                ProviderData = providerData,
                PhotoUrl = photoUrl,
                PhotoSize = photoSize,
                PhotoWidth = photoWidth,
                PhotoHeight = photoHeight,
                NeedName = needName,
                NeedPhoneNumber = needPhoneNumber,
                NeedEmail = needEmail,
                NeedShippingAddress = needShippingAddress,
                SendPhoneNumberToProvider = sendPhoneNumberToProvider,
                SendEmailToProvider = sendEmailToProvider,
                IsFlexible = isFlexible,
                DisableNotification = disableNotification,
                ReplyToMessageId = replyToMessageId,
                ReplyMarkup = replyMarkup
            }, cancellationToken);
        }

        /// <inheritdoc />
        public Task AnswerShippingQueryAsync(
            string shippingQueryId,
            IEnumerable<ShippingOption> shippingOptions,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new AnswerShippingQueryRequest(shippingQueryId, shippingOptions), cancellationToken);
        }

        /// <inheritdoc />
        public Task AnswerShippingQueryAsync(
            string shippingQueryId,
            string errorMessage,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new AnswerShippingQueryRequest(shippingQueryId, errorMessage), cancellationToken);
        }

        /// <inheritdoc />
        public Task AnswerPreCheckoutQueryAsync(
            string preCheckoutQueryId,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new AnswerPreCheckoutQueryRequest(preCheckoutQueryId), cancellationToken);
        }

        /// <inheritdoc />
        public Task AnswerPreCheckoutQueryAsync(
            string preCheckoutQueryId,
            string errorMessage,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new AnswerPreCheckoutQueryRequest(preCheckoutQueryId, errorMessage), cancellationToken);
        }

        #endregion Payments

        #region Games

        /// <inheritdoc />
        public Task<Message> SendGameAsync(
            long chatId,
            string gameShortName,
            bool disableNotification = default,
            int replyToMessageId = default,
            InlineKeyboardMarkup replyMarkup = default,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new SendGameRequest(chatId, gameShortName)
            {
                DisableNotification = disableNotification,
                ReplyToMessageId = replyToMessageId,
                ReplyMarkup = replyMarkup
            }, cancellationToken);
        }

        /// <inheritdoc />
        public Task<Message> SetGameScoreAsync(
            int userId,
            int score,
            long chatId,
            int messageId,
            bool force = default,
            bool disableEditMessage = default,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new SetGameScoreRequest(userId, score, chatId, messageId)
            {
                Force = force,
                DisableEditMessage = disableEditMessage
            }, cancellationToken);
        }

        /// <inheritdoc />
        public Task SetGameScoreAsync(
            int userId,
            int score,
            string inlineMessageId,
            bool force = default,
            bool disableEditMessage = default,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new SetInlineGameScoreRequest(userId, score, inlineMessageId)
            {
                Force = force,
                DisableEditMessage = disableEditMessage
            }, cancellationToken);
        }

        /// <inheritdoc />
        public Task<GameHighScore[]> GetGameHighScoresAsync(
            int userId,
            long chatId,
            int messageId,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(
                 new GetGameHighScoresRequest(userId, chatId, messageId),
                 cancellationToken);
        }

        /// <inheritdoc />
        public Task<GameHighScore[]> GetGameHighScoresAsync(
            int userId,
            string inlineMessageId,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(
                 new GetInlineGameHighScoresRequest(userId, inlineMessageId),
                 cancellationToken);
        }

        #endregion Games

        #region Group and channel management

        /// <inheritdoc />
        public Task<string> ExportChatInviteLinkAsync(
            ChatId chatId,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new ExportChatInviteLinkRequest(chatId), cancellationToken);
        }

        /// <inheritdoc />
        public Task SetChatPhotoAsync(
            ChatId chatId,
            InputFileStream photo,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new SetChatPhotoRequest(chatId, photo), cancellationToken);
        }

        /// <inheritdoc />
        public Task DeleteChatPhotoAsync(
            ChatId chatId,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new DeleteChatPhotoRequest(chatId), cancellationToken);
        }

        /// <inheritdoc />
        public Task SetChatTitleAsync(
            ChatId chatId,
            string title,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new SetChatTitleRequest(chatId, title), cancellationToken);
        }

        /// <inheritdoc />
        public Task SetChatDescriptionAsync(
            ChatId chatId,
            string description = default,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new SetChatDescriptionRequest(chatId, description), cancellationToken);
        }

        /// <inheritdoc />
        public Task PinChatMessageAsync(
            ChatId chatId,
            int messageId,
            bool disableNotification = default,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new PinChatMessageRequest(chatId, messageId)
            {
                DisableNotification = disableNotification
            }, cancellationToken);
        }

        /// <inheritdoc />
        public Task UnpinChatMessageAsync(
            ChatId chatId,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new UnpinChatMessageRequest(chatId), cancellationToken);
        }

        /// <inheritdoc />
        public Task SetChatStickerSetAsync(
            ChatId chatId,
            string stickerSetName,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new SetChatStickerSetRequest(chatId, stickerSetName), cancellationToken);
        }

        /// <inheritdoc />
        public Task DeleteChatStickerSetAsync(
            ChatId chatId,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new DeleteChatStickerSetRequest(chatId), cancellationToken);
        }

        #endregion

        #region Stickers

        /// <inheritdoc />
        public Task<StickerSet> GetStickerSetAsync(
            string name,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new GetStickerSetRequest(name), cancellationToken);
        }

        /// <inheritdoc />
        public Task<File> UploadStickerFileAsync(
            int userId,
            InputFileStream pngSticker,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new UploadStickerFileRequest(userId, pngSticker), cancellationToken);
        }

        /// <inheritdoc />
        public Task CreateNewStickerSetAsync(
            int userId,
            string name,
            string title,
            InputOnlineFile pngSticker,
            string emojis,
            bool isMasks = default,
            MaskPosition maskPosition = default,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new CreateNewStickerSetRequest(userId, name, title, pngSticker, emojis)
            {
                ContainsMasks = isMasks,
                MaskPosition = maskPosition
            }, cancellationToken);
        }

        /// <inheritdoc />
        public Task AddStickerToSetAsync(
            int userId,
            string name,
            InputOnlineFile pngSticker,
            string emojis,
            MaskPosition maskPosition = default,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new AddStickerToSetRequest(userId, name, pngSticker, emojis)
            {
                MaskPosition = maskPosition
            }, cancellationToken);
        }

        /// <inheritdoc />
        public Task CreateNewAnimatedStickerSetAsync(
            int userId,
            string name,
            string title,
            InputFileStream tgsSticker,
            string emojis,
            bool isMasks = default,
            MaskPosition maskPosition = default,
            CancellationToken cancellationToken = default)
        {
            return MakeRequestAsync(
new CreateNewAnimatedStickerSetRequest(userId, name, title, tgsSticker, emojis)
{
ContainsMasks = isMasks,
MaskPosition = maskPosition
},
cancellationToken
);
        }

        /// <inheritdoc />
        public Task AddAnimatedStickerToSetAsync(
            int userId,
            string name,
            InputFileStream tgsSticker,
            string emojis,
            MaskPosition maskPosition = default,
            CancellationToken cancellationToken = default)
        {
            return MakeRequestAsync(
new AddAnimatedStickerToSetRequest(userId, name, tgsSticker, emojis)
{
MaskPosition = maskPosition
},
cancellationToken
);
        }

        /// <inheritdoc />
        public Task SetStickerPositionInSetAsync(
            string sticker,
            int position,
            CancellationToken cancellationToken = default)
        {
            return MakeRequestAsync(
new SetStickerPositionInSetRequest(sticker, position),
cancellationToken
);
        }

        /// <inheritdoc />
        public Task DeleteStickerFromSetAsync(
            string sticker,
            CancellationToken cancellationToken = default
        )
        {
            return MakeRequestAsync(new DeleteStickerFromSetRequest(sticker), cancellationToken);
        }

        /// <inheritdoc />
        public Task SetStickerSetThumbAsync(
            string name,
            int userId,
            InputOnlineFile thumb = default,
            CancellationToken cancellationToken = default)
        {
            return MakeRequestAsync(
new SetStickerSetThumbRequest(name, userId, thumb),
cancellationToken
);
        }

        #endregion
    }
}
