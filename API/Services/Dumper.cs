using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<Dumper> logger;

        public Dumper(ILogger<Dumper> logger)
        {
            this.logger = logger;
        }
        //private string TargetFile => $"Dumps/{DateTimeOffset.Now:HH-mm-ss}.json";
        private string TargetFile => $"dbExport.json";

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var client = new HttpClient();
            logger.LogInformation("Starting");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {

                    await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
                    var dump = await client.GetStringAsync("http://localhost:5000/api/Debug/exportDb", stoppingToken);
                    File.WriteAllText(TargetFile, dump);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "error while dump");
                }
            }
        }
    }
}
