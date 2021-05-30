using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OuchRBot.API.Models
{
    public class BotUser
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public long VkPeerId { get; set; }
        public DateTimeOffset? Birthday { get; set; }
        public string Education { get; set; }
        public string WorExperience { get; set; }
        public string AvailableInterships { get; set; }
        public string PhotoUrl { get; set; }

        [JsonIgnore]
        public BotUserStatusChange CurrentStatus
        {
            get
            {
                if (ChangesHistory == null)
                {
                    throw new NotSupportedException($"Fill {nameof(ChangesHistory)} to receive {nameof(CurrentStatus)}");
                }
                return ChangesHistory.OrderByDescending(h => h.Date).FirstOrDefault();
            }
        }

        public List<BotUserStatusChange> ChangesHistory { get; set; }
    }
}
