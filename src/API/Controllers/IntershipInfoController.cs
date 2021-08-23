using HtmlAgilityPack;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace OuchRBot.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IntershipInfoController : ControllerBase
    {
        [HttpGet("html/{*id}")]
        public async Task<ActionResult> GetHtmlAsync(string id)
        {
            var url = $"https://edu.greenatom.ru/trainee/{id}/";
            var html = await new HttpClient().GetStringAsync(url);

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            var head = htmlDoc.DocumentNode.SelectSingleNode("//head");
            var htmlBody = htmlDoc.DocumentNode.SelectSingleNode("//*[contains(@class, 'trainee__wrapper')]");
            var link = htmlBody.SelectSingleNode("//*[contains(@class, 'trainee__wrapper')]//a");
            link.Remove();
            var htmlResult = @$"<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8""></head><body>{htmlBody.OuterHtml}</body></html>";

            return Content(htmlResult, "text/html");
        }
    }
}
