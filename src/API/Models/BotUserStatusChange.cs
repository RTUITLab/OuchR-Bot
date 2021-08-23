using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OuchRBot.API.Models
{
    public class BotUserStatusChange
    {
        public int Id { get; set; }
        public DateTimeOffset Date { get; set; }
        public ProgressStatus NewStatus { get; set; }
        public long ResumeDocVkId { get; set; }
        public string SelectedIntership { get; set; }
        public string TestResult { get; set; }
        public DateTimeOffset? MeetStartTime { get; set; }
        public TimeSpan? MeetDuration { get; set; }
        public BotUser BotUser { get; set; }
        public int BotUserId { get; set; }
        public string ResumeLink { get; set; }
        public string ZoomLink { get; set; }
        public string MeetCalendarUId { get; set; }
    }
}
