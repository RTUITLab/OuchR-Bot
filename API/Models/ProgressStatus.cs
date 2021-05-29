using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OuchRBot.API.Models
{
    public enum ProgressStatus
    {
        NoDocument,
        DocumentSent,
        InternshipSelecting,
        DoingTestCase,
        TestCaseChecking,
        TestCaseDone,
        MeetTimeUserAccepted,
        MeetScheduled,
        Done
    }
}
