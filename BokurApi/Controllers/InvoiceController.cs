using BokurApi.Models;
using BokurApi.RateLimiting;
using InvoiceGenerator.Helpers;
using InvoiceGenerator.Manager;
using InvoiceGenerator.Models;
using InvoiceGenerator.Models.Configuration;
using InvoiceGenerator.Models.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using System.Net;
using System.Text;

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
        public async Task<IActionResult> GenerateInvoice([FromForm] string configuration, IFormFile timeCsv)
        {
            GenerationConfiguration generationConfiguration = GenerationConfiguration.FromJson(configuration);

            InvoiceManager manager = new InvoiceManager(generationConfiguration.GeneralInformation);

            using (MemoryStream memoryStream = new MemoryStream())
            {
                await timeCsv.CopyToAsync(memoryStream);

                TimeExport timeExport = TimeExport.FromCsv(Encoding.UTF8.GetString(memoryStream.ToArray()));

                await FontHelper.SetupFont("Roboto", "Regular", "Medium", "Bold");

                using (MemoryStream outputStream = new MemoryStream())
                {
                    Invoice invoice = manager.CreateInvoice(generationConfiguration.InvoiceInformation, timeExport);
                    invoice.GeneratePdf(outputStream);

                    return File(outputStream.ToArray(), "application/pdf", invoice.FileName);
                }
            }
        }

        [Authorize(AuthorizationRole.Admin)]
        [HttpGet("configuration-sample")]
        [ProducesResponseType(typeof(GenerationConfiguration), (int)HttpStatusCode.OK)]
        public IActionResult GetConfigurationSample()
        {
            GenerationConfiguration configuration = new GenerationConfiguration(new InvoiceInformation()
            {
                Comment = "This is a sample invoice",
                DaysToPay = 30,
                IncludeTax = true,
                IncludeBreakdown = true,
                IssueDate = DateTime.Now,
                Number = "INV-001",
                Receiver = new Company()
                {
                    Address = new Address()
                    {
                        FirstLine = "Sample street 1",
                        SecondLine = "Sample city",
                    },
                    Name = "Sample company",
                    OrganizationNumber = "123456-7890",
                    Reference = new Person()
                    {
                        Email = "sample@email.com",
                        Name = "Sample person",
                        PhoneNumber = "1234567890",
                    }
                }
            },
            new GeneralInformation()
            {
                DefaultUnitPrice = 100,
                LogoUrl = "https://i.pinimg.com/236x/ab/3d/e2/ab3de2f5cc08f507f728f39c66e596b8.jpg",
                PaymentInformation = new PaymentInformation()
                {
                    BankgiroNumber = "123-4567",
                    Bic = "BIC123",
                    Iban = "IBAN123",
                },
                Sender = new Company()
                {
                    Address = new Address()
                    {
                        FirstLine = "Sample street 2",
                        SecondLine = "Sample city",
                    },
                    Name = "Sample company",
                    OrganizationNumber = "123456-7890",
                    Reference = new Person()
                    {
                        Email = "sample2@email.com",
                        Name = "Sample person",
                        PhoneNumber = "1234567890",
                    }
                },
                UnitPriceOverride = new Dictionary<string, int>()
                {
                    { "ExpensivePerson", 200 }
                }
            });

            return Ok(configuration);
        }
    }
}
