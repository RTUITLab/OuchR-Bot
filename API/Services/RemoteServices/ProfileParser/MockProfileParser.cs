using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OuchRBot.API.Services.RemoteServices.ProfileParser
{
    public class MockProfileParser : IProfileParser
    {
        public Task<MettingCreateResponse> CreateMeeting(MeettingInfo meettingInfo)
        {
            return Task.FromResult(new MettingCreateResponse($"http://rtuitlab.dev#{JsonConvert.SerializeObject(meettingInfo)}", $"http://rtuitlab.dev#{JsonConvert.SerializeObject(meettingInfo)}"));
        }


        public static Task<List<(DateTimeOffset from, DateTimeOffset to)>> StaticGetAvailableDates()
        {
            var r = new Random();
            var dates = new HashSet<(DateTimeOffset from, DateTimeOffset to)>();
            while(dates.Count < 3)
            {
                var fromDate = DateTimeOffset.Now.Date.AddDays(1).AddHours(r.Next(9, 9 + 8));
                dates.Add((fromDate, fromDate.AddHours(1)));

            }
            return Task.FromResult(dates.ToList());
        }
        public Task<List<(DateTimeOffset from, DateTimeOffset to)>> GetAvailableDates()
        {
            return StaticGetAvailableDates();
        }

        public Task<ReadOnlyCollection<Intership>> GetInternshipsAsync(Stream resumeStream)
        {
            var list = new List<Intership>
            {
                new Intership("2", "http://rtuitlab.dev#nut", "Собиратель орехов", "11", ""),
                new Intership("3", "http://rtuitlab.dev#front", "Frontend разработчик", "11", ""),
                new Intership("4", "http://rtuitlab.dev#back", "Backdoor разработчик", "11", ""),
                new Intership("5", "http://rtuitlab.dev#ml", "ML разработчик", "11", ""),
            };
            int n = list.Count;
            var rnd = new Random();
            while (n > 1)
            {
                n--;
                int k = rnd.Next(n + 1);
                var value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
            return Task.FromResult(list.AsReadOnly());
        }

        public async Task<Intership> GetIntershipInfo(string intershipId)
        {
            return (await GetInternshipsAsync(null)).First();
        }
    }
}
