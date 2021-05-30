using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OuchRBot.API.Services.RemoteServices.ProfileParser
{
    public record MettingCreateResponse(string Zoom, string Calendar);
    public record MeettingInfo(string Name, DateTime Time, double Duration, string Password = "not-secure");
    public record Intership(string Id, string Url, string Title, string Description);
    public interface IProfileParser
    {
        public Task<ReadOnlyCollection<Intership>> GetInternshipsAsync(Stream resumeStream);
        public Task<MettingCreateResponse> CreateMeeting(MeettingInfo meettingInfo);
        public Task<List<(DateTimeOffset from, DateTimeOffset to)>> GetAvailableDates();
    }
}
