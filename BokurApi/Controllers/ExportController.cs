using BokurApi.Models;
using BokurApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BokurApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ExportController : ControllerBase
    {
        private readonly IExportService _exportService;

        public ExportController(IExportService exportService)
        {
            _exportService = exportService;
        }

        [Authorize(AuthorizationRole.Admin)]
        [HttpGet("exported")]
        public async Task<IActionResult> GetExportedData(DateTime startDate, DateTime? endDate = null)
        {
            DateTime endDateToUse = endDate ?? DateTime.Now;

            Response.Headers.Append("Content-Disposition", "attachment; filename=bokur_export.zip");
            Response.ContentType = "application/zip";

            await _exportService.ExportTransactionsAsync(startDate, endDateToUse, Response.BodyWriter.AsStream());

            return new EmptyResult(); // Response is already streamed
        }
    }
}
