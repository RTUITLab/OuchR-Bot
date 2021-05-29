using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OuchRBot.API.Services.RemoteServices.GroupMaker
{

    public class Rootobject
    {
        public int membersCount { get; set; }
        public int itCount { get; set; }
        public int gradsCount { get; set; }
        public Member[] members { get; set; }
        public Dictionary<string, int> cityTop { get; set; }
        public Dictionary<string, int> eduStats { get; set; }
    }

    public class Member
    {
        public string city { get; set; }
        public string university { get; set; }
        public int graduation { get; set; }
        public string faculty { get; set; }
        public string gradYear { get; set; }
        public bool employed { get; set; }
        public bool isITSpec { get; set; }
        public Itmetrics iTMetrics { get; set; }
        public int iTScore { get; set; }
        public int id { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public int sex { get; set; }
        public int age { get; set; }
    }

    public class Itmetrics
    {
        public int faculty { get; set; }
    }

}
