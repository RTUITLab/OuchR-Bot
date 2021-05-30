using Ical.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
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
        private readonly IOptions<CalendarOptions> options;

        public CalendarController(IOptions<CalendarOptions> options)
        {
            this.options = options;
        }

        [HttpGet]
        public async Task<ActionResult<List<CalendarEvent>>> GetEventsAsync()
        {

            var calendarRawContent = await new HttpClient().GetStringAsync(options.Value.GoogleCalendarUrl);
            var calendar = Calendar.Load(calendarRawContent);
            
            return calendar.Events.Select(e => new CalendarEvent(e.Summary, e.Description,  e.Start.AsDateTimeOffset, e.End.AsDateTimeOffset)).ToList();
        }
    }
    public record CalendarEvent(string Title, string Description, DateTimeOffset Start, DateTimeOffset End);
}
