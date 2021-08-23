using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OuchRBot.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VkNet;
using VkNet.Model;

namespace OuchRBot.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResumeController : ControllerBase
    {
        private readonly IOptions<VkBotOptions> options;

        public ResumeController(IOptions<VkBotOptions> options)
        {
            this.options = options;
        }

        [HttpGet("downloadResume/{resumeId:long}")]
        public async Task<ActionResult> DownloadResumeAsync(long resumeId)
        {
            var vkApi = new VkApi();
            await vkApi.AuthorizeAsync(new ApiAuthParams { AccessToken = options.Value.GroupAccessToken });
            var doc = vkApi.Docs.GetById(new VkNet.Model.Attachments.Document[] {new VkNet.Model.Attachments.Document
            {
                Id = resumeId
            } });
            return Ok(doc == null);
        }
    }
}
