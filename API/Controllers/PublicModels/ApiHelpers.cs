using OuchRBot.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OuchRBot.API.Controllers.PublicModels
{
    public static class ApiHelpers
    {
        public static ApiStageType MapChangeToApiStage(ProgressStatus status)
        {
            return status switch
            {
                ProgressStatus.NoDocument => ApiStageType.Applications,
                ProgressStatus.DocumentSent => ApiStageType.Applications,
                ProgressStatus.InternshipSelecting => ApiStageType.Applications,
                ProgressStatus.DoingTestCase => ApiStageType.Testing,
                ProgressStatus.TestCaseChecking => ApiStageType.Testing,
                ProgressStatus.TestCaseDone => ApiStageType.Interview,
                ProgressStatus.MeetTimeUserAccepted => ApiStageType.Interview,
                ProgressStatus.MeetScheduled => ApiStageType.Interview,
                ProgressStatus.Offer => ApiStageType.Offer,
                ProgressStatus.Work => ApiStageType.Offer,
                _ => throw new Exception($"incorrect new status ")
            };
        }

        public static ApiFinder MapUserToFinder(BotUser user)
        {
            var newFinder = new ApiFinder(
                    user.VkPeerId,
                    user.Name,
                    user.Birthday,
                    user.Education,
                    user.WorExperience,
                    user.CurrentIntership,
                    user.AvailableInterships,
                    user.PhotoUrl,
                    user.ChangesHistory.FirstOrDefault(c => c.NewStatus == ProgressStatus.DocumentSent)?.ResumeLink,
                    user.ChangesHistory.FirstOrDefault(c => c.NewStatus == ProgressStatus.TestCaseChecking)?.TestResult,
                    new List<ApiEvent>());
            foreach (var statusChange in user.ChangesHistory)
            {
                var apiStatus = ApiHelpers.MapChangeToApiStage(statusChange.NewStatus);
                newFinder.Events.Add(new ApiEvent(statusChange.Date, apiStatus, statusChange.MeetStartTime, statusChange.MeetStartTime + statusChange.MeetDuration));
            }
            var lastEvent = newFinder.Events.First();
            var newList = new List<ApiEvent>();
            foreach (var result in newFinder.Events.Skip(1))
            {
                if (result.Stage != lastEvent.Stage)
                {
                    newList.Add(lastEvent);
                    lastEvent = result;
                }
            }
            newList.Add(lastEvent);
            newFinder.Events.Clear();
            newFinder.Events.AddRange(newList);
            return newFinder;
        }
    }
}
