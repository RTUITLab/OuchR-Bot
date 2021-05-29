using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OuchRBot.API.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OuchRBot.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ControlFlowController : ControllerBase
    {
        private readonly MessageHandlerService messageHandlerService;
        private readonly ILogger<ControlFlowController> logger;

        public ControlFlowController(
            MessageHandlerService messageHandlerService,
            ILogger<ControlFlowController> logger)
        {
            this.messageHandlerService = messageHandlerService;
            this.logger = logger;
        }

        [Produces("application/json")]
        [HttpPost("approveTestResults/{userId:long}")]
        public async Task<ActionResult> ApproveTestResultsAsync(long userId)
        {
            var result = await messageHandlerService.ApproveSolution(userId);
            return result.Match(ok => Ok(ok) as ActionResult, error => BadRequest(error.Reason));
        }

        [Produces("application/json")]
        [HttpPost("approveTime/{userId:long}")]
        public async Task<ActionResult> ApproveTimeAsync(long userId)
        {
            var result = await messageHandlerService.ApproveTime(userId);
            return result.Match(ok => Ok(ok) as ActionResult, error => BadRequest(error.Reason));
        }
    }
}
