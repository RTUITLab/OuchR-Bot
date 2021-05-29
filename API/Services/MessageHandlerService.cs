using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OuchRBot.API.Database;
using OuchRBot.API.Models;
using OuchRBot.API.Services.RemoteServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using VkNet.Abstractions;
using VkNet.Enums.SafetyEnums;
using VkNet.Model.Attachments;
using VkNet.Model.GroupUpdate;
using VkNet.Model.Keyboard;
using VkNet.Model.RequestParams;

namespace OuchRBot.API.Services
{
    public class MessageHandlerService
    {
        private readonly BotDbContext dbContext;
        private readonly IProfileParser profileParser;
        private readonly ILogger<MessageHandlerService> logger;

        private static readonly Random random = new Random();
        private static int RandomInt => random.Next();

        public MessageHandlerService(
            BotDbContext dbContext, 
            IProfileParser profileParser,
            ILogger<MessageHandlerService> logger)
        {
            this.dbContext = dbContext;
            this.profileParser = profileParser;
            this.logger = logger;
        }

        public async Task HandleMessage(IVkApi api, MessageNew message, CancellationToken cancellationToken)
        {
            var user = await GetBotUserInfoAsync(message.Message.PeerId.Value, api);

            switch (user.CurrectStatus)
            {
                case ProgressStatus.NoDocument:
                    await HandleNoDocumentStatus(api, user, message, cancellationToken);
                    break;
                case ProgressStatus.DocumentSent:
                //break;
                case ProgressStatus.InternshipSelecting:
                //break;
                case ProgressStatus.DoingTestCase:
                //break;
                case ProgressStatus.TestCaseDone:
                //break;
                case ProgressStatus.MeetScheduled:
                //break;
                case ProgressStatus.Done:
                //break;
                default:
                    await api.Messages.SendAsync(new MessagesSendParams
                    {
                        PeerId = message.Message.PeerId.Value,
                        Message = "Простите, мы пока не знаем, что с вами делать",
                        RandomId = new Random().Next(int.MinValue, int.MaxValue),
                        Keyboard = new KeyboardBuilder()
                                            .AddButton("Привет Текст", "нагрузка", KeyboardButtonColor.Negative)
                                            .SetInline(true)
                                            .AddLine()
                                            .AddButton("Отметить текст", "другая нагрузка", KeyboardButtonColor.Primary)
                                            .Build()
                    });
                    break;
            }

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

            var profileInfo = await profileParser.GetInternshipsAsync(await new HttpClient().GetStreamAsync(resume.Uri));
            await api.Messages.SendAsync(new MessagesSendParams
            {
                PeerId = message.Message.PeerId.Value,
                Message = profileInfo[0]    ,
                RandomId = RandomInt
            });
        }


        private async Task<BotUser> GetBotUserInfoAsync(long peerId, IVkApi api)
        {
            var targetUser = await dbContext.Users.SingleOrDefaultAsync(u => u.VkPeerId == peerId);

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
                CurrectStatus = ProgressStatus.NoDocument,
                ChangesHistory = new List<BouUserStatusChange>
                {
                    new BouUserStatusChange
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
