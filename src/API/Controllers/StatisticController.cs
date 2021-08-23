using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OuchRBot.API.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OuchRBot.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatisticController : ControllerBase
    {
        private readonly BotDbContext dbContext;

        public StatisticController(BotDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        [HttpGet("salesfunnel")]
        public async Task<ActionResult<Funnel>> SalesFunnelAsync()
        {
            var users = await dbContext.Users
                .Where(u => u.ChangesHistory.Count > 1)
                .Include(u => u.ChangesHistory)
                .ToListAsync();
            var result = new Funnel
            {
                ApplicationsTotal = users.Count(u => u.CurrentStatus.NewStatus >= Models.ProgressStatus.DocumentSent),
                TestTotal = users.Count(u => u.CurrentStatus.NewStatus >= Models.ProgressStatus.DoingTestCase),
                InterviewTotal = users.Count(u => u.CurrentStatus.NewStatus >= Models.ProgressStatus.TestCaseDone),
                Offer = users.Count(u => u.CurrentStatus.NewStatus >= Models.ProgressStatus.Offer),
                Work = users.Count(u => u.CurrentStatus.NewStatus >= Models.ProgressStatus.Work),
            };

            result.ApplicationsDropped = result.ApplicationsTotal - result.TestTotal;
            result.TestDateBeingLate = result.TestTotal - result.InterviewTotal;

            result.InterviewSkip = users.Count(u => u.CurrentStatus.NewStatus <= Models.ProgressStatus.DocumentSent && u.ChangesHistory.Select(Half => Half.NewStatus).Max() >= Models.ProgressStatus.TestCaseDone);
            result.InterviewFailed = result.InterviewTotal - result.Offer - result.InterviewSkip;
            
            result.OfferRenouncement = users.Count(u => u.CurrentStatus.NewStatus <= Models.ProgressStatus.DocumentSent && u.ChangesHistory.Select(Half => Half.NewStatus).Max() >= Models.ProgressStatus.Offer);

            return result;
        }

        [HttpGet("mainNumbers")]
        public async Task<ActionResult<MainNumbers>> MainNumbers()
        {

            var users = await dbContext.Users
                .Where(u => u.ChangesHistory.Count > 1)
                .Include(u => u.ChangesHistory)
                .ToListAsync();
            return new MainNumbers
            {
                Work = users.Count(u => u.CurrentStatus.NewStatus >= Models.ProgressStatus.Work),
                InterviewNow = users.Count(
                    u => u.CurrentStatus.NewStatus >= Models.ProgressStatus.TestCaseDone && 
                    u.CurrentStatus.NewStatus < Models.ProgressStatus.Offer),
                InterviewTotal = users.Count(
                    u => u.CurrentStatus.NewStatus >= Models.ProgressStatus.DocumentSent &&
                    u.CurrentStatus.NewStatus < Models.ProgressStatus.Offer),
                Potencial = 2223
            };
        } 
    }

    public class MainNumbers
    {
        public long Work { get; set; }
        public long InterviewNow { get; set; }
        public long InterviewTotal { get; set; }
        public long Potencial { get; set; }
    }
    public class Funnel
    {
        public long ApplicationsTotal { get; set; }
        public long ApplicationsDropped { get; set; }
        public long TestTotal { get; set; }
        public long TestDateBeingLate { get; set; }
        public long TestRenouncement { get; set; }
        public long InterviewTotal { get; set; }
        public long InterviewSkip { get; set; }
        public long InterviewFailed { get; set; }
        public long Offer { get; set; }
        public long OfferRenouncement { get; set; }
        public long Work { get; set; }
    };
}
