using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OuchRBot.API.Controllers.PublicModels;
using OuchRBot.API.Database;
using OuchRBot.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
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
                finders.Add(ApiHelpers.MapUserToFinder(user));
            }
            
            return finders;
        }

        [HttpGet("readyToCheck")]
        public async Task<ActionResult<List<ApiFinder>>> ReadyToCheckAsync()
        {
            var users = await dbContext.Users
                .Include(u => u.ChangesHistory)
                .Where(u => u.ChangesHistory.OrderByDescending(h => h.Date).First().NewStatus == ProgressStatus.TestCaseChecking)
                .ToListAsync();
            foreach (var user in users)
            {
                user.ChangesHistory = user.ChangesHistory.OrderByDescending(c => c.Date).ToList();
            }
            var finders = new List<ApiFinder>();

            foreach (var user in users.Where(u => u.ChangesHistory.Count > 1))
            {
                finders.Add(ApiHelpers.MapUserToFinder(user));
            }

            return finders;
        }

        [HttpGet("inProgress")]
        public async Task<ActionResult<List<ApiFinder>>> InProgressAsync()
        {
            var users = await dbContext.Users
                .Include(u => u.ChangesHistory)
                .Where(u => u.ChangesHistory.OrderByDescending(h => h.Date).First().NewStatus == ProgressStatus.DoingTestCase)
                .ToListAsync();
            foreach (var user in users)
            {
                user.ChangesHistory = user.ChangesHistory.OrderByDescending(c => c.Date).ToList();
            }
            var finders = new List<ApiFinder>();

            foreach (var user in users.Where(u => u.ChangesHistory.Count > 1))
            {
                finders.Add(ApiHelpers.MapUserToFinder(user));
            }

            return finders;
        }

        [HttpGet("waitTimeConfirmation")]
        public async Task<ActionResult<List<ApiFinder>>> WaitTimeConfirmationAsync()
        {
            var users = await dbContext.Users
                .Include(u => u.ChangesHistory)
                .Where(u => u.ChangesHistory.OrderByDescending(h => h.Date).First().NewStatus == ProgressStatus.MeetTimeUserAccepted)
                .ToListAsync();
            foreach (var user in users)
            {
                user.ChangesHistory = user.ChangesHistory.OrderByDescending(c => c.Date).ToList();
            }
            var finders = new List<ApiFinder>();

            foreach (var user in users.Where(u => u.ChangesHistory.Count > 1))
            {
                finders.Add(ApiHelpers.MapUserToFinder(user));
            }

            return finders;
        }

        [HttpGet("scheduledConferences")]
        public async Task<ActionResult<List<ApiFinder>>> ScheduledConferencesAsync()
        {
            var users = await dbContext.Users
                .Include(u => u.ChangesHistory)
                .Where(u => u.ChangesHistory.OrderByDescending(h => h.Date).First().NewStatus == ProgressStatus.MeetScheduled)
                .ToListAsync();
            foreach (var user in users)
            {
                user.ChangesHistory = user.ChangesHistory.OrderByDescending(c => c.Date).ToList();
            }
            var finders = new List<ApiFinder>();

            foreach (var user in users.Where(u => u.ChangesHistory.Count > 1))
            {
                finders.Add(ApiHelpers.MapUserToFinder(user));
            }

            return finders;
        }

    }

    public record ApiEvent(
        DateTimeOffset Date,
        ApiStageType Stage,
        DateTimeOffset? MeetStartTime,
        DateTimeOffset? MeetEndTime
        );
    public record ApiFinder(
        long UserId,
        string Name,
        DateTimeOffset? Birthday,
        string Education,
        string WorkExperience,
        string CurrentIntership,
        string AvailableInterships,
        string PthotoUrl,
        string ResumeUrl,
        string TestResult,
        List<ApiEvent> Events
        );
}
