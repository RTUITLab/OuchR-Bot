using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OuchRBot.API.Models
{
    public class BouUserStatusChange
    {
        public int Id { get; set; }
        public DateTimeOffset Date { get; set; }
        public ProgressStatus NewStatus { get; set; }

        public BotUser BotUser { get; set; }
        public int BotUserId { get; set; }
    }
}
