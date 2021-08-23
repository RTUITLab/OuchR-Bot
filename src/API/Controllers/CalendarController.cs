using Ical.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OuchRBot.API.Database;
using OuchRBot.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace OuchRBot.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CalendarController : ControllerBase
    {
        private readonly BotDbContext dbContext;
        private readonly IOptions<CalendarOptions> options;

        public CalendarController(
            BotDbContext dbContext,
            IOptions<CalendarOptions> options)
        {
            this.dbContext = dbContext;
            this.options = options;
        }

        private string ConvertTitle(string title)
        {
            var tokens = title.Split(" ");
            return string.Join(' ', tokens.TakeLast(2).Concat(tokens.Take(tokens.Length - 2)));
        }

        [HttpGet]
        public async Task<ActionResult<List<CalendarEvent>>> GetEventsAsync()
        {
            var selection = dbContext.Users.SelectMany(u => u.ChangesHistory)
                .Include(c => c.BotUser)
                .AsEnumerable()
                .Select(c => new { c.MeetCalendarUId, c.BotUser.VkPeerId })
                .GroupBy(c => c.MeetCalendarUId)
                .ToDictionary(c => c.Key ?? "", c => c.First().VkPeerId);

            var calendarRawContent = await new HttpClient().GetStringAsync(options.Value.GoogleCalendarUrl);
            var calendar = Calendar.Load(calendarRawContent);
            return calendar.Events
                .Select(e => new CalendarEvent(ConvertTitle(e.Summary), e.Description, e.Start.AsDateTimeOffset,
                    e.End?.AsDateTimeOffset ?? (e.IsAllDay ? e.Start.AsDateTimeOffset + TimeSpan.FromDays(1) : e.Start.AsDateTimeOffset),
                selection.TryGetValue(e.Uid, out long id) ? id : null)).ToList();
        }
    }
    public record CalendarEvent(string Title, string Description, DateTimeOffset Start, DateTimeOffset End, long? UserId);
}
