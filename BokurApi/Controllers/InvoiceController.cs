using BokurApi.Models.Bokur;
using BokurApi.Models.Http;
using BokurApi.Models;
using BokurApi.RateLimiting;
using BokurApi.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using InvoiceGenerator.Manager;
using InvoiceGenerator.Models.Configuration;
using InvoiceGenerator.Models.Data;
using System.Text;
using InvoiceGenerator.Helpers;
using InvoiceGenerator.Models;
using QuestPDF.Fluent;

namespace BokurApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class InvoiceController : ControllerBase
    {
        [Authorize(AuthorizationRole.Admin)]
        [HttpPost("generate")]
        [Limit(MaxRequests = 20, TimeWindow = 10)]
        [ProducesResponseType(typeof(List<IFileHttpResult>), (int)HttpStatusCode.Created)]
        public async Task<IActionResult> GenerateInvoice([FromBody] GenerationConfiguration configuration, IFormFile timeCsv)
        {
            InvoiceManager manager = new InvoiceManager(configuration.GeneralInformation);

            using (MemoryStream memoryStream = new MemoryStream())
            {
                await timeCsv.CopyToAsync(memoryStream);

                TimeExport timeExport = TimeExport.FromCsv(Encoding.UTF8.GetString(memoryStream.ToArray()));

                await FontHelper.SetupFont("Roboto", "Regular", "Medium", "Bold");

                using (MemoryStream outputStream = new MemoryStream())
                {
                    Invoice invoice = manager.CreateInvoice(configuration.InvoiceInformation, timeExport);
                    invoice.GeneratePdf(outputStream);

                    return File(outputStream.ToArray(), "application/pdf", invoice.FileName);
                }
            }
        }
    }
}
