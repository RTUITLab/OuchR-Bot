using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace OuchRBot.API.Services.RemoteServices
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
        public async Task<List<string>> GetInternshipsAsync(Stream resumeStream)
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
                return new List<string> { text };
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, $"Can't get interships by {options.Value.GetIntershipsUrl}");
                throw;
            }
        }
    }
}
