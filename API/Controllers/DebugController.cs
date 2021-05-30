using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OuchRBot.API.Database;
using OuchRBot.API.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OuchRBot.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DebugController : ControllerBase
    {
        private readonly BotDbContext dbContext;
        private readonly ILogger<DebugController> logger;

        public DebugController(
            BotDbContext dbContext,
            ILogger<DebugController> logger)
        {
            this.dbContext = dbContext;
            this.logger = logger;
        }

        [HttpGet("exportDb")]
        public async Task<ActionResult> ApproveTestResultsAsync()
        {
            var users = await dbContext.Users.Include(u => u.ChangesHistory).ToListAsync();
            return Ok(users);
        }
    }
}
