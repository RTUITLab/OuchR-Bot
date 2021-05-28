using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OuchRBot.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VkNet.FluentCommands.GroupBot;

namespace OuchRBot.API.Services
{
    public class VkBotWorker : BackgroundService
    {
        private readonly IOptions<VkBotOptions> options;
        private readonly ILogger<VkBotWorker> logger;

        public VkBotWorker(
            IOptions<VkBotOptions> options,
            ILogger<VkBotWorker> logger)
        {
            this.options = options;
            this.logger = logger;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            FluentGroupBotCommands commands = new();

            commands.ConfigureGroupLongPoll(options.Value.GroupId);

            await commands.InitBotAsync(options.Value.GroupAccessToken);

            commands.OnText("^ping", "pong");
            commands.OnText("^hello$", new[] { "hi!", "hey!", "good day!" });
            commands.OnText("command not found");

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
    }
}
