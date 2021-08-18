using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OuchRBot.API.Database;
using OuchRBot.API.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace OuchRBot.API.Services
{
    public class Dumper : BackgroundService
    {
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly ILogger<Dumper> logger;

        public Dumper(IServiceScopeFactory serviceScopeFactory, ILogger<Dumper> logger)
        {
            this.serviceScopeFactory = serviceScopeFactory;
            this.logger = logger;
        }
        //private string TargetFile => $"Dumps/{DateTimeOffset.Now:HH-mm-ss}.json";
        private string TargetFile => $"dbExport.json";

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Starting");
            await ReadDump();
            var client = new HttpClient();
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
                    var dump = await client.GetStringAsync("http://localhost:5000/api/Debug/exportDb", stoppingToken);
                    await File.WriteAllTextAsync(TargetFile, dump, stoppingToken);
                    logger.LogDebug("Data was dumped");
                }
                catch (OperationCanceledException)
                {
                    logger.LogInformation("Dumper was cancelled");
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "error while dump");
                }
            }
        }

        private async Task ReadDump()
        {
            if (!File.Exists(TargetFile))
            {
                logger.LogWarning($"There is no dump file '{TargetFile}'");
                return;
            }
            var dumpText = await File.ReadAllTextAsync(TargetFile);
            List<BotUser> users;
            try
            {
                users = JsonConvert.DeserializeObject<List<BotUser>>(dumpText);
            }
            catch
            {
                logger.LogError($"File '{TargetFile}' contains incorrect data");
                return;
            }
            using var scope = serviceScopeFactory.CreateScope();
            using var botDbContext  = scope.ServiceProvider.GetRequiredService<BotDbContext>();
            botDbContext.Users.AddRange(users);
            await botDbContext.SaveChangesAsync();
            logger.LogInformation($"Data from '{TargetFile}' was successfully restored");
        }
    }
}
