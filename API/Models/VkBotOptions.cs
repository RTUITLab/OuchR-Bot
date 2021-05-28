using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OuchRBot.API.Models
{
    public record VkBotOptions
    {
        public ulong GroupId { get; init; }
        public string GroupAccessToken { get; init; }
    }
}
