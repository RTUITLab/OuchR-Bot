using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace OuchRBot.API.Services.RemoteServices.ProfileParser
{
    public class RealProfileParser : IProfileParser
    {
        private readonly IOptions<RealProfileParserOptions> options;
        private readonly ILogger<RealProfileParser> logger;

        public RealProfileParser(
            IOptions<RealProfileParserOptions> options,
            ILogger<RealProfileParser> logger)
        {
            this.options = options;
            this.logger = logger;
        }

        public async Task<MettingCreateResponse> CreateMeeting(MeettingInfo meettingInfo)
        {
            try
            {
                var client = new HttpClient();
                var body = JsonConvert.SerializeObject(meettingInfo, new JsonSerializerSettings
                {
                    ContractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new CamelCaseNamingStrategy()
                    },
                    Formatting = Formatting.Indented
                });
                logger.LogInformation(body);
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var result = await client.PostAsync(options.Value.CreateMeetingUrl, content);
                var text = await result.Content.ReadAsStringAsync();
                logger.LogInformation($"{(int)result.StatusCode}: {text}");
                return JsonConvert.DeserializeObject<MettingCreateResponse>(text);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, $"Can't create meeting by {options.Value.CreateMeetingUrl}");
                throw;
            }
        }

        public Task<List<(DateTimeOffset from, DateTimeOffset to)>> GetAvailableDates()
        {
            return MockProfileParser.StaticGetAvailableDates();
        }

        public async Task<ReadOnlyCollection<Intership>> GetInternshipsAsync(Stream resumeStream)
        {
            try
            {
                var client = new HttpClient();
                var content = new MultipartFormDataContent
                {
                    { new StreamContent(resumeStream), "file", "resume.pdf" }
                };
                var result = await client.PostAsync(options.Value.GetIntershipsUrl, content);
                var text = await result.Content.ReadAsStringAsync();
                logger.LogInformation($"{(int)result.StatusCode}: {text}");
                return JsonConvert.DeserializeObject<List<Intership>>(text).AsReadOnly();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, $"Can't get interships by {options.Value.GetIntershipsUrl}");
                throw;
            }
        }
    }
}
