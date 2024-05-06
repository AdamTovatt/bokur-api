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
        public async Task<ObjectResult> GenerateInvoice([FromBody] GenerationConfiguration configuration)
        {
            InvoiceManager manager = new InvoiceManager(configuration.GeneralInformation);

            TimeExport timeExport = TimeExport.FromCsv(File.ReadAllText(clockifyExportPath));

            SetupFont("Roboto", "Regular", "Medium", "Bold").Wait();

            Invoice invoice = manager.CreateInvoice(instanceConfiguration, timeExport);
            invoice.GeneratePdf(invoice.FileName);

            return new ApiResponse(await AccountRepository.Instance.GetAllAsync());
        }
    }
}
