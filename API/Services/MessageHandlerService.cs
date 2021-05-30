using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OneOf;
using OuchRBot.API.Database;
using OuchRBot.API.Extensions;
using OuchRBot.API.Models;
using OuchRBot.API.Services.RemoteServices.ProfileParser;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using VkNet;
using VkNet.Abstractions;
using VkNet.Enums.Filters;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.GroupUpdate;
using VkNet.Model.Keyboard;
using VkNet.Model.RequestParams;

namespace OuchRBot.API.Services
{
    public class MessageHandlerService
    {
        private const string SUBMIT_INTERSHIP = "sub_intership:";
        private const string DECLINE_INTERSHIP = "dec_intership:";
        private const string SUBMIT_MEET_TIME = "sub_meet_time:";

        private readonly BotDbContext dbContext;
        private readonly IProfileParser profileParser;
        private readonly IOptions<VkBotOptions> options;
        private readonly ILogger<MessageHandlerService> logger;

        private static readonly Random random = new Random();
        private static int RandomInt => random.Next();

        public MessageHandlerService(
            BotDbContext dbContext,
            IProfileParser profileParser,
            IOptions<VkBotOptions> options,
            ILogger<MessageHandlerService> logger)
        {
            this.dbContext = dbContext;
            this.profileParser = profileParser;
            this.options = options;
            this.logger = logger;
        }



        public async Task HandleMessage(IVkApi api, MessageNew message, CancellationToken cancellationToken)
        {
            var (user, isFirstTime) = await GetBotUserInfoAsync(message.Message.PeerId.Value, api);

            switch (user.CurrentStatus.NewStatus)
            {
                case ProgressStatus.NoDocument:
                    await HandleNoDocumentStatus(api, user, isFirstTime, message, cancellationToken);
                    break;
                case ProgressStatus.DocumentSent:
                //break;
                case ProgressStatus.InternshipSelecting:
                    await HandleSlectingIntership(api, user, message);
                    break;
                case ProgressStatus.DoingTestCase:
                    await LogUsersExport();
                    await HandleDonigReply(api, user, message);
                    break;
                case ProgressStatus.TestCaseDone:
                    await HandleMeetTimeSelected(api, user, message);
                    break;
                case ProgressStatus.MeetScheduled:
                    await HandleMessageWhileScheduled(api, user, message);
                    break;
                case ProgressStatus.Offer:
                    await HandleMessageWhileOffer(api, user, message);
                    break;
                case ProgressStatus.Work:
                    await api.Messages.SendAsync(new MessagesSendParams
                    {
                        PeerId = message.Message.PeerId.Value,
                        Message = "Вы произвели отличное впечатление! В ближайшее время с вами свяжется наш HR-специалист, чтобы мы могли продолжить сотрудничество. О дальнейших шагах мы сообщим вам лично.",
                        RandomId = RandomInt
                    });
                    break;
                default:
                    await api.Messages.SendAsync(new MessagesSendParams
                    {
                        PeerId = message.Message.PeerId.Value,
                        Message = "Простите, мы пока не знаем, что с вами делать",
                        RandomId = RandomInt
                    });
                    break;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task HandleMessageWhileOffer(IVkApi api, BotUser user, MessageNew message)
        {
            if (!string.IsNullOrEmpty(message.Message.Payload))
            {
                if (message.Message.Payload.Contains("agr"))
                {
                    await api.Messages.SendAsync(new MessagesSendParams
                    {
                        PeerId = message.Message.PeerId.Value,
                        Message = $"Вы произвели отличное впечатление! В ближайшее время с вами свяжется наш HR-специалист, чтобы мы могли продолжить сотрудничество. О дальнейших шагах мы сообщим вам лично.",
                        RandomId = RandomInt
                    });
                    user.ChangesHistory.Add(new BotUserStatusChange
                    {
                        Date = DateTimeOffset.UtcNow,
                        NewStatus = ProgressStatus.Work
                    });
                    return;
                }
                if (message.Message.Payload.Contains("dis"))
                {
                    await api.Messages.SendAsync(new MessagesSendParams
                    {
                        PeerId = message.Message.PeerId.Value,
                        Message = $"Редиска, вы, товарищь.",
                        RandomId = RandomInt
                    });
                    user.ChangesHistory.Add(new BotUserStatusChange
                    {
                        Date = DateTimeOffset.UtcNow,
                        NewStatus = ProgressStatus.NoDocument
                    });
                    return;
                }
            }
        }

        private async Task HandleMessageWhileScheduled(IVkApi api, BotUser user, MessageNew message)
        {
            if (!string.IsNullOrEmpty(message.Message.Payload) && message.Message.Payload.Contains("loose"))
            {
                await api.Messages.SendAsync(new MessagesSendParams
                {
                    PeerId = message.Message.PeerId.Value,
                    Message = $"Большое спасибо, что уделили нам время! Если хотите попробовать еще раз, пришлите нам свое резюме в формате PDF.",
                    RandomId = RandomInt
                });
                user.ChangesHistory.Add(new BotUserStatusChange
                {
                    Date = DateTimeOffset.UtcNow,
                    NewStatus = ProgressStatus.NoDocument
                });
                return;
            }

            if (message.Message.Text == "/offer")
            {
                await api.Messages.SendAsync(new MessagesSendParams
                {
                    PeerId = message.Message.PeerId.Value,
                    Message = $"Вам выдано приглашение на работу! Поздравляем!",
                    RandomId = RandomInt,
                    Keyboard = new KeyboardBuilder().SetInline()
                        .AddButton("Отказаться от предложения", "dis")
                        .AddButton("Согласиться", "agr", KeyboardButtonColor.Primary)
                        .Build()
                });
                user.ChangesHistory.Add(new BotUserStatusChange
                {
                    Date = DateTimeOffset.UtcNow,
                    NewStatus = ProgressStatus.Offer
                });
                return;
            }

            await api.Messages.SendAsync(new MessagesSendParams
            {
                PeerId = message.Message.PeerId.Value,
                Message = $"С нетерпением ждем вас на встрече, которая запланирована на {user.CurrentStatus.MeetStartTime.Value:dd.MM.yyyy HH:mm}.",
                RandomId = RandomInt,
                Keyboard = new KeyboardBuilder().SetInline()
                    .AddButton("Отказаться от собеседования", "loose")
                    .Build()
            });
        }

        private record MeetTime(DateTimeOffset From, DateTimeOffset To);
        private async Task HandleMeetTimeSelected(IVkApi api, BotUser user, MessageNew message)
        {
            logger.LogInformation(message.Message.Payload);
            var payload = JsonConvert.DeserializeObject<ButtonPayload>(message.Message.Payload ?? "{}");
            if (payload.Button?.StartsWith(SUBMIT_MEET_TIME) == true)
            {
                var base64Payload = payload.Button.Substring(SUBMIT_MEET_TIME.Length);
                var jsonPayload = JsonConvert.DeserializeObject<MeetTime>(Encoding.UTF8.GetString(Convert.FromBase64String(base64Payload)));
                await api.Messages.SendAsync(new MessagesSendParams
                {
                    PeerId = message.Message.PeerId,
                    Message = $"Отлично, осталось дождаться подтверждения времени от HR специалиста!",
                    RandomId = RandomInt
                });
                user.ChangesHistory.Add(new BotUserStatusChange
                {
                    Date = DateTimeOffset.UtcNow,
                    NewStatus = ProgressStatus.MeetTimeUserAccepted,
                    MeetStartTime = jsonPayload.From,
                    MeetDuration = jsonPayload.To - jsonPayload.From
                });
                // TODO
                //Task.Run(async () =>
                //{
                //    await Task.Delay(5000);

                //    logger.LogInformation($"try to approve time");
                //    try
                //    {
                //        var response = await new HttpClient().PostAsync($"http://localhost:5000/api/controlflow/approveTime/{user.VkPeerId}", null);
                //        logger.LogInformation(response.StatusCode.ToString());
                //    }
                //    catch (Exception ex)
                //    {
                //        logger.LogWarning(ex, "can't send request");
                //    }

                //});

                return;
            }
            await api.Messages.SendAsync(new MessagesSendParams
            {
                PeerId = message.Message.PeerId,
                Message = $"Вам необходимо выбрать время собеседования.",
                RandomId = RandomInt
            });
        }

        private static readonly string[] DAY_OF_WEEKS = new string[] { "Вс", "Пн", "Вт", "Ср", "Чт", "Пт", "Сб" };
        public record ApproveError(string Reason);
        public async Task<OneOf<string, ApproveError>> ApproveSolution(long peerId)
        {
            var api = new VkApi();
            await api.AuthorizeAsync(new ApiAuthParams
            {
                AccessToken = options.Value.GroupAccessToken
            });
            var (currentUser, _) = await GetBotUserInfoAsync(peerId, api);
            if (currentUser.CurrentStatus.NewStatus != ProgressStatus.TestCaseChecking)
            {
                return new ApproveError($"User {currentUser.Name} not in waiting for check mode");
            }

            logger.LogInformation("approve solution");

            var acceptedTime = await profileParser.GetAvailableDates();
            var keyboardBuilder = new KeyboardBuilder().SetInline();
            acceptedTime
                .OrderBy(t => t.from)
                .ToList()
                .ForEach(t => keyboardBuilder
                    .AddButton(
                        $"{t.from:dd.MM.yyyy} {DAY_OF_WEEKS[(int)t.from.DayOfWeek]} {t.from:HH:mm}-{t.to:HH:mm}",
                        $"{SUBMIT_MEET_TIME}{Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new MeetTime(t.from, t.to))))}")
                    .AddLine()
                );

            currentUser.ChangesHistory.Add(new BotUserStatusChange
            {
                Date = DateTimeOffset.UtcNow,
                NewStatus = ProgressStatus.TestCaseDone,
            });
            await dbContext.SaveChangesAsync();

            await api.Messages.SendAsync(new MessagesSendParams
            {
                PeerId = peerId,
                Message = "Отличные новости! Ваше решение очень понравилось нашим специалистам, мы готовы пригласить вас на онлайн-собеседование. Теперь вам нужно выбрать время для онлайн-встречи в Zoom с нашим HR-специалистом.",
                RandomId = RandomInt,
                Keyboard = keyboardBuilder.Build()
            });

            return "OK";
        }

        public async Task<OneOf<string, ApproveError>> ApproveTime(long peerId)
        {
            var api = new VkApi();
            await api.AuthorizeAsync(new ApiAuthParams
            {
                AccessToken = options.Value.GroupAccessToken
            });
            var (currentUser, _) = await GetBotUserInfoAsync(peerId, api);
            if (currentUser.CurrentStatus.NewStatus != ProgressStatus.MeetTimeUserAccepted)
            {
                return new ApproveError($"User {currentUser.Name} not in waiting for accept time");
            }

            logger.LogInformation("approve time");


            var intershipInfo = await profileParser.GetIntershipInfo(currentUser.ChangesHistory.Last(h => h.NewStatus == ProgressStatus.DoingTestCase).SelectedIntership);

            var url = await profileParser.CreateMeeting(new MeettingInfo(
                intershipInfo.Title + " " + currentUser.Name,
                currentUser.CurrentStatus.MeetStartTime.Value.DateTime,
                currentUser.CurrentStatus.MeetDuration.Value.TotalMinutes));

            currentUser.ChangesHistory.Add(new BotUserStatusChange
            {
                Date = DateTimeOffset.UtcNow,
                ZoomLink = url.Zoom,
                MeetCalendarUId = url.Calendar,
                MeetStartTime = currentUser.CurrentStatus.MeetStartTime,
                MeetDuration = currentUser.CurrentStatus.MeetDuration,
                NewStatus = ProgressStatus.MeetScheduled
            });
            await dbContext.SaveChangesAsync();

            await api.Messages.SendAsync(new MessagesSendParams
            {
                PeerId = peerId,
                Message = @$"Собеседование с вами будет проводить Зинаида Котова.
Ссылка на Zoom-конференцию: {url.Zoom}

Чтобы собеседование прошло комфортно, позаботьтесь о том, чтобы вас не отвлекали посторонние звуки, камера и микрофон работали исправно, а интернет-соединение работало стабильно. Рекомендуем вам подключаться по ссылке за 5 минут до назначенного времени.
    ",
                RandomId = RandomInt,
            });
            return "Ok";
        }

        private async Task HandleDonigReply(IVkApi api, BotUser user, MessageNew message)
        {


            if (message.Message.Text?.Contains("/") != true)
            {
                await api.Messages.SendAsync(new MessagesSendParams
                {
                    PeerId = message.Message.PeerId.Value,
                    Message = "Вам необходимо отправить id docker образа, например clue/json-server",
                    RandomId = RandomInt
                });
                return;
            }

            logger.LogInformation($"Receive correct task done {message.Message.Text}");

            user.ChangesHistory.Add(new BotUserStatusChange
            {
                Date = DateTimeOffset.UtcNow,
                NewStatus = ProgressStatus.TestCaseChecking,
                TestResult = message.Message.Text
            });
            await dbContext.SaveChangesAsync();
            await api.Messages.SendAsync(new MessagesSendParams
            {
                PeerId = message.Message.PeerId.Value,
                Message = "Спасибо, теперь нам нужно немного времени, чтобы проверить ваше решение. Ожидайте ответа в течение трех дней и будьте готовы к приглашению на онлайн-собеседование!",
                RandomId = RandomInt
            });

            // Do it from controller
            //Task.Run(async () =>
            //{
            //    await Task.Delay(500);
            //    logger.LogInformation($"try to approve task {user.CurrentStatus.Id}");
            //    try
            //    {
            //        await new HttpClient().PostAsync($"http://localhost:5000/api/controlflow/approveTestResults/{user.VkPeerId}", null);
            //    }
            //    catch (Exception ex)
            //    {
            //        logger.LogWarning(ex, "can't send request");
            //    }
            //});
        }

        private async Task HandleNoDocumentStatus(
            IVkApi api,
            BotUser user,
            bool isFirstTime,
            MessageNew message,
            CancellationToken cancellationToken)
        {
            if (isFirstTime)
            {
                await api.Messages.SendAsync(new MessagesSendParams
                {
                    PeerId = message.Message.PeerId.Value,
                    Message = @"Здравствуйте! Мы очень рады, что вас заинтересовала возможность присоединиться к команде Росатома. Хотите попробовать свои силы и начать карьеру в атомной отрасли прямо сейчас?",
                    RandomId = RandomInt,
                    Keyboard = new KeyboardBuilder().SetInline(true)
                        .AddButton("Не сейчас", "nope", color: KeyboardButtonColor.Default)
                        .AddButton("Конечно", "sure", color: KeyboardButtonColor.Primary)
                        .Build()
                });
                return;
            }

            if (!string.IsNullOrEmpty(message.Message.Payload))
            {
                if (message.Message.Payload.Contains("sure"))
                {
                    await api.Messages.SendAsync(new MessagesSendParams
                    {
                        PeerId = message.Message.PeerId.Value,
                        Message = $"Пришлите нам свое резюме в формате PDF, чтобы мы могли предложить интересные вакансии по вашему профилю.",
                        RandomId = RandomInt
                    });
                    return;
                }
                if (message.Message.Payload.Contains("nope"))
                {
                    await api.Messages.SendAsync(new MessagesSendParams
                    {
                        PeerId = message.Message.PeerId.Value,
                        Message = $"Что ж, не сейчас, так не сейчас, ничего страшного. Если вдруг вы захотите узнать какие вакансии могут вам подойти, просто отправьте свое резюме в формате PDF.",
                        RandomId = RandomInt
                    });
                    return;
                }
                return;
            }

            if (message.Message.Attachments.Count == 0)
            {
                await api.Messages.SendAsync(new MessagesSendParams
                {
                    PeerId = message.Message.PeerId.Value,
                    Message = $"Пришлите нам свое резюме в формате PDF, чтобы мы могли предложить интересные вакансии по вашему профилю",
                    RandomId = RandomInt
                });
                return;
            }
            if (message.Message.Attachments.Count > 1 ||
                message.Message.Attachments.Single().Instance is not Document resume ||
                resume.Ext != "pdf")
            {
                await api.Messages.SendAsync(new MessagesSendParams
                {
                    PeerId = message.Message.PeerId.Value,
                    Message = $"Необходимо приложить единственный документ .pdf, спасибо.",
                    RandomId = RandomInt
                });
                return;
            }
            user.ChangesHistory.Add(new BotUserStatusChange
            {
                Date = DateTimeOffset.UtcNow,
                NewStatus = ProgressStatus.DocumentSent,
                ResumeDocVkId = resume.Id.Value,
                ResumeLink = resume.Uri
            });


            logger.LogInformation("try to get interships");

            await api.Messages.SendAsync(new MessagesSendParams
            {
                PeerId = message.Message.PeerId.Value,
                Message = $"Пожалуйста, подождите. Работает NLP.",
                RandomId = RandomInt
            });

            var profileInfo = await profileParser.GetInternshipsAsync(await new HttpClient().GetStreamAsync(resume.Uri));

            var keyboardBuilder = new KeyboardBuilder().SetInline();
            foreach (var intership in profileInfo)
            {
                keyboardBuilder
                    .AddButton(
                    intership.Title.Length > 35 ? intership.Title.Substring(0, 35) + "..." : intership.Title,
                    $"{SUBMIT_INTERSHIP}{intership.Id}",
                    KeyboardButtonColor.Primary)
                    .AddLine();
            }
            keyboardBuilder.AddButton("Мне ничего не подходит", DECLINE_INTERSHIP);
            user.AvailableInterships = string.Join(", ", profileInfo.Select(p => p.Title));
            user.ChangesHistory.Add(new BotUserStatusChange
            {
                Date = DateTimeOffset.UtcNow,
                NewStatus = ProgressStatus.InternshipSelecting
            });

            await api.Messages.SendAsync(new MessagesSendParams
            {
                PeerId = message.Message.PeerId.Value,
                Message = BuildIntershipsIntros(profileInfo),
                RandomId = RandomInt,
                Keyboard = keyboardBuilder.Build(),
                DontParseLinks = true
            });
        }

        private string BuildIntershipsIntros(ReadOnlyCollection<Intership> interships)
        {
            var builder = new StringBuilder();

            builder.AppendLine("Мы считаем, что вам могут подойти следующие вакансии. Выберите наиболее интересное направление, и мы вышлем тестовое задание, которое нужно будет выполнить в течение 7 дней. Ближе к концу срока мы пришлем напоминание о сдаче.");

            foreach (var intership in interships)
            {
                builder.AppendLine();
                builder.AppendLine(intership.Title);
                builder.AppendLine(intership.Url);
            }

            return builder.ToString();
        }

        private record ButtonPayload(string Button);
        private async Task HandleSlectingIntership(IVkApi api, BotUser user, MessageNew message)
        {
            var payload = JsonConvert.DeserializeObject<ButtonPayload>(message.Message.Payload ?? "{}");
            if (payload.Button?.StartsWith(SUBMIT_INTERSHIP) == true)
            {
                var uploadedDocument = await api.LoadDocumentToChatAsync(
                    await new HttpClient().GetStreamAsync("http://localhost:5000/MarkdownExample.pdf"),
                    DocMessageType.Doc,
                    message.Message.PeerId.Value,
                    $"Тестовое задание.pdf");

                var intershipInfo = await profileParser.GetIntershipInfo(payload.Button.Substring(SUBMIT_INTERSHIP.Length));

                user.CurrentIntership = intershipInfo.Title;
                user.ChangesHistory.Add(new BotUserStatusChange
                {
                    Date = DateTimeOffset.UtcNow,
                    NewStatus = ProgressStatus.DoingTestCase,
                    SelectedIntership = payload.Button.Substring(SUBMIT_INTERSHIP.Length)
                });
                await api.Messages.SendAsync(new MessagesSendParams
                {
                    PeerId = message.Message.PeerId.Value,
                    Message = @"Отличный выбор! 
Направляем вам тестовое задание. Напоминаем, что выполнить его нужно в течение 7 дней.  Игра началась!

В сообщении приложите единственную строку - id публично доступного docker образа.
Например - clue/json-server

На выполнение задания у вас есть 7 дней, игра началась!",
                    RandomId = RandomInt,
                    Attachments = new List<MediaAttachment> { uploadedDocument }
                });
                return;
            }
            if (payload.Button?.Contains(DECLINE_INTERSHIP) == true)
            {
                await api.Messages.SendAsync(new MessagesSendParams
                {
                    PeerId = message.Message.PeerId.Value,
                    RandomId = RandomInt,
                    Message = @"Возможно, для вас сейчас нет подходящих вакансий. С другими открытыми позициями вы можете ознакомиться по ссылке: https://edu.greenatom.ru/
Также мы постараемся присылать те вакансии, которые могут вас заинтересовать."
                });
                return;
            }
            await api.Messages.SendAsync(new MessagesSendParams
            {
                PeerId = message.Message.PeerId.Value,
                Message = "Пожалуйста, выберите интересующую вас стажировку",
                RandomId = RandomInt
            });
        }

        private async Task LogUsersExport()
        {
            var users = await dbContext.Users.Include(u => u.ChangesHistory).ToListAsync();
            logger.LogInformation(JsonConvert.SerializeObject(users, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }));
        }

        private async Task<(BotUser, bool isFirstTime)> GetBotUserInfoAsync(long peerId, IVkApi api)
        {
            var targetUser = await dbContext.Users
                .Include(u => u.ChangesHistory)
                .SingleOrDefaultAsync(u => u.VkPeerId == peerId);

            if (targetUser != null)
            {
                return (targetUser, false);
            }
            var targetUserInfoList = await api.Users.GetAsync(new long[] { peerId }, ProfileFields.Photo100);
            var targetUserInfo = targetUserInfoList.Single();
            targetUser = new BotUser
            {
                VkPeerId = peerId,
                Name = targetUserInfo.LastName + " " + targetUserInfo.FirstName,
                PhotoUrl = targetUserInfo.Photo100.ToString(),
                ChangesHistory = new List<BotUserStatusChange>
                {
                    new BotUserStatusChange
                    {
                        NewStatus = ProgressStatus.NoDocument,
                        Date = DateTimeOffset.UtcNow
                    }
                }
            };
            dbContext.Users.Add(targetUser);
            await dbContext.SaveChangesAsync();
            logger.LogInformation($"saved user {targetUser.Id}");
            return (targetUser, true);
        }
    }
}
