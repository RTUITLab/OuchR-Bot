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
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using VkNet;
using VkNet.Abstractions;
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
            var user = await GetBotUserInfoAsync(message.Message.PeerId.Value, api);

            switch (user.CurrentStatus.NewStatus)
            {
                case ProgressStatus.NoDocument:
                    await HandleNoDocumentStatus(api, user, message, cancellationToken);
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
                //break;
                case ProgressStatus.Done:
                //break;
                default:
                    await api.Messages.SendAsync(new MessagesSendParams
                    {
                        PeerId = message.Message.PeerId.Value,
                        Message = "Простите, мы пока не знаем, что с вами делать",
                        RandomId = RandomInt,
                        Keyboard = new KeyboardBuilder()
                                            .AddButton("Привет Текст", "нагрузка", KeyboardButtonColor.Negative)
                                            .SetInline(true)
                                            .AddLine()
                                            .AddButton("Отметить текст", "другая нагрузка", KeyboardButtonColor.Primary)
                                            .Build()
                    });
                    break;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
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
                    Message = $"Отлично, осталось дождаться подтверждения времени от HR специалиста!", // TODO
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
                Task.Run(async () =>
                {
                    await Task.Delay(5000);

                    logger.LogInformation($"try to approve time");
                    try
                    {
                        var response = await new HttpClient().PostAsync($"http://localhost:5000/api/controlflow/approveTime/{user.VkPeerId}", null);
                        logger.LogInformation(response.StatusCode.ToString());
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "can't send request");
                    }

                });

                return;
            }
            await api.Messages.SendAsync(new MessagesSendParams
            {
                PeerId = message.Message.PeerId,
                Message = $"Вам необходимо выбрать время собеседования", // TODO
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
            var currentUser = await GetBotUserInfoAsync(peerId, api);
            if (currentUser.CurrentStatus.NewStatus != ProgressStatus.TestCaseChecking)
            {
                return new ApproveError($"User {currentUser.Name} not in waiting for check mode");
            }

            logger.LogInformation("approve solution");

            var acceptedTime = await profileParser.GetAvailableDates();
            var keyboardBuilder = new KeyboardBuilder().SetInline();
            acceptedTime

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
                Message = "Ваше решеине принято, время выбрать корретное время", // TODO
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
            var currentUser = await GetBotUserInfoAsync(peerId, api);
            if (currentUser.CurrentStatus.NewStatus != ProgressStatus.MeetTimeUserAccepted)
            {
                return new ApproveError($"User {currentUser.Name} not in waiting for accept time");
            }

            logger.LogInformation("approve time");

            var url = await profileParser.CreateMeeting(new MeettingInfo(
                currentUser.ChangesHistory.Last(h => h.NewStatus == ProgressStatus.InternshipSelecting).SelectedIntership + " " + currentUser.Name, 
                currentUser.CurrentStatus.MeetStartTime.DateTime,
                currentUser.CurrentStatus.MeetDuration.TotalMinutes));

            await api.Messages.SendAsync(new MessagesSendParams
            {
                PeerId = peerId,
                Message = url.Zoom,
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
                Message = "Ваше решение принято, мы ответим вам в течение пяти рабочих дней.",
                RandomId = RandomInt
            });

            // Do it from controller
            Task.Run(async () =>
            {
                await Task.Delay(500);
                logger.LogInformation($"try to approve task {user.CurrentStatus.Id}");
                try
                {
                    await new HttpClient().PostAsync($"http://localhost:5000/api/controlflow/approveTestResults/{user.VkPeerId}", null);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "can't send request");
                }
            });
        }

        private async Task HandleNoDocumentStatus(
            IVkApi api,
            BotUser user,
            MessageNew message,
            CancellationToken cancellationToken)
        {
            if (message.Message.Attachments.Count == 0)
            {
                await api.Messages.SendAsync(new MessagesSendParams
                {
                    PeerId = message.Message.PeerId.Value,
                    Message = $"Уважаемый {user.Name}, пришлите нам ваше резюме, и мы покажем, какие стажировки вам доступны!",
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
                ResumeDocVkId = resume.Id.Value
            });


            logger.LogInformation("try to get interships");
            var profileInfo = await profileParser.GetInternshipsAsync(await new HttpClient().GetStreamAsync(resume.Uri));

            var keyboardBuilder = new KeyboardBuilder().SetInline();
            foreach (var intership in profileInfo)
            {
                keyboardBuilder
                    .AddButton(intership.Title.Length > 35 ? intership.Title.Substring(0, 35) + "..." : intership.Title, $"{SUBMIT_INTERSHIP}{intership.Url}")
                    .AddLine();
            }

            user.ChangesHistory.Add(new BotUserStatusChange
            {
                Date = DateTimeOffset.UtcNow,
                NewStatus = ProgressStatus.InternshipSelecting
            });

            await api.Messages.SendAsync(new MessagesSendParams
            {
                PeerId = message.Message.PeerId.Value,
                Message = "тут красивое описание каждой ваканчии",
                RandomId = RandomInt,
                Keyboard = keyboardBuilder.Build()
            });
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
                user.ChangesHistory.Add(new BotUserStatusChange
                {
                    Date = DateTimeOffset.UtcNow,
                    NewStatus = ProgressStatus.DoingTestCase
                });
                await api.Messages.SendAsync(new MessagesSendParams
                {
                    PeerId = message.Message.PeerId.Value,
                    Message = @"Тут мы пошлем тестовое задание.

Ответьте на это сообщение для сдачи задания. В сообщении приложите единственную строку - id публично доступного docker образа. 
Например - clue/json-server

На выполнение задания у вас есть 7 дней, игра началась!",
                    RandomId = RandomInt,
                    Attachments = new List<MediaAttachment> { uploadedDocument }
                });
                return;
            }
            await api.Messages.SendAsync(new MessagesSendParams
            {
                PeerId = message.Message.PeerId.Value,
                Message = "Пожвлуйста, выберите интересующую вас стажировку",
                RandomId = RandomInt
            });
        }

        private async Task LogUsersExport()
        {
            var users = await dbContext.Users.Include(u => u.ChangesHistory).ToListAsync();
            logger.LogInformation(JsonConvert.SerializeObject(users, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }));
        }

        private async Task<BotUser> GetBotUserInfoAsync(long peerId, IVkApi api)
        {
            var targetUser = await dbContext.Users
                .Include(u => u.ChangesHistory)
                .SingleOrDefaultAsync(u => u.VkPeerId == peerId);

            if (targetUser != null)
            {
                return targetUser;
            }
            var targetUserInfoList = await api.Users.GetAsync(new long[] { peerId });
            var targetUserInfo = targetUserInfoList.Single();
            targetUser = new BotUser
            {
                VkPeerId = peerId,
                Name = targetUserInfo.LastName + " " + targetUserInfo.FirstName,
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
            return targetUser;
        }
    }
}
