using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OuchRBot.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VkNet.Abstractions;
using VkNet.Enums.SafetyEnums;
using VkNet.FluentCommands.GroupBot;
using VkNet.Model.GroupUpdate;
using VkNet.Model.Keyboard;
using VkNet.Model.RequestParams;

namespace OuchRBot.API.Services
{
    public class VkBotWorker : BackgroundService
    {
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly IOptions<VkBotOptions> options;
        private readonly ILogger<VkBotWorker> logger;

        public VkBotWorker(
            IServiceScopeFactory serviceScopeFactory,
            IOptions<VkBotOptions> options,
            ILogger<VkBotWorker> logger)
        {
            this.serviceScopeFactory = serviceScopeFactory;
            this.options = options;
            this.logger = logger;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            FluentGroupBotCommands commands = new();

            commands.ConfigureGroupLongPoll(options.Value.GroupId);

            await commands.InitBotAsync(options.Value.GroupAccessToken);



            commands.OnDocument(HandleMessage);
            commands.OnText(HandleMessage);

            commands.OnException((e, token) =>
            {
                logger.LogWarning("Wake up, everything is broken");
                logger.LogWarning($"[{DateTime.UtcNow}] {e.Message} {Environment.NewLine} {e.StackTrace}");
                return Task.CompletedTask;
            });
            try
            {
                logger.LogInformation("starting receiving");
                await commands.ReceiveMessageAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "error");
            }
            logger.LogInformation("after receiving");
        }

        private async Task HandleMessage(IVkApi api, MessageNew message, CancellationToken cancellationToken)
        {
            logger.LogInformation("Handle message");
            try
            {

                using var scope = serviceScopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<MessageHandlerService>().HandleMessage(api, message, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error while handling message");
            }
        }
    }
}
