using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OuchRBot.API.Services.RemoteServices
{
    public record Intership(string Url, string Title);
    public interface IProfileParser
    {
        public Task<List<string>> GetInternshipsAsync(Stream resumeStream);
    }
}
