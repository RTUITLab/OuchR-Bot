using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OuchRBot.API.Database;
using OuchRBot.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OuchRBot.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MyCandidatesController : ControllerBase
    {
        private readonly BotDbContext dbContext;

        public MyCandidatesController(BotDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        [HttpGet]
        public async Task<ActionResult<List<ApiFinder>>> GetTotalListAsync()
        {
            var users = await dbContext.Users.Include(u => u.ChangesHistory).ToListAsync();
            foreach (var user in users)
            {
                user.ChangesHistory = user.ChangesHistory.OrderByDescending(c => c.Date).ToList();
            }
            var finders = new List<ApiFinder>();

            foreach (var user in users.Where(u => u.ChangesHistory.Count > 1))
            {
                var newFinder = new ApiFinder(
                    user.VkPeerId,
                    user.Name,
                    user.Birthday,
                    user.Education,
                    user.WorExperience,
                    user.AvailableInterships,
                    user.PhotoUrl,
                    user.ChangesHistory.FirstOrDefault(c => c.NewStatus == ProgressStatus.DocumentSent)?.ResumeDocVkId,
                    user.ChangesHistory.FirstOrDefault(c => c.NewStatus == ProgressStatus.TestCaseChecking)?.TestResult,
                    new List<ApiEvent>());
                finders.Add(newFinder);
                foreach (var statusChange in user.ChangesHistory)
                {
                    var apiStatus = statusChange.NewStatus switch
                    {
                        ProgressStatus.NoDocument => ApiStageType.Applications,
                        ProgressStatus.DocumentSent => ApiStageType.Applications,
                        ProgressStatus.InternshipSelecting => ApiStageType.Applications,
                        ProgressStatus.DoingTestCase => ApiStageType.Testing,
                        ProgressStatus.TestCaseChecking => ApiStageType.Testing,
                        ProgressStatus.TestCaseDone => ApiStageType.Interview,
                        ProgressStatus.MeetTimeUserAccepted => ApiStageType.Interview,
                        ProgressStatus.MeetScheduled => ApiStageType.Interview,
                        ProgressStatus.Done => ApiStageType.Offer,
                        _ => throw new Exception($"incorrect new status ")
                    };
                    newFinder.Events.Add(new ApiEvent(statusChange.Date, apiStatus));
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
            }

            return finders;
        }
    }
    public enum ApiStageType
    {
        Applications,
        Testing,
        Interview,
        Offer
    }
    public record ApiEvent(
        DateTimeOffset Date,
        ApiStageType Stage
        );
    public record ApiFinder(
        long UserId,
        string Name,
        DateTimeOffset? Birthday,
        string Education,
        string WorkExperience,
        string AvailableInterships,
        string PthotoUrl,
        long? ResumeId,
        string TestResult,
        List<ApiEvent> Events
        );
}
