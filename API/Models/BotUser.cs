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
        public ProgressStatus CurrectStatus { get; set; }

        public List<BouUserStatusChange> ChangesHistory { get; set; }
    }
}
