using BokurApi.Helpers;
using BokurApi.Managers.Transactions;
using BokurApi.Models.Http;
using BokurApi.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using RobinTTY.NordigenApiClient.Models.Errors;
using RobinTTY.NordigenApiClient.Models.Requests;
using RobinTTY.NordigenApiClient.Models.Responses;
using System.Net;

namespace BokurApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TransactionController : ControllerBase
    {
        [HttpGet("get-bank-data")]
        [Limit(MaxRequests = 20, TimeWindow = 10)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.Created)]
        public async Task<ObjectResult> GetBankData()
        {
            Requisition? requisition = await NordigenManager.Instance.GetLinkedRequisition();

            return new ApiResponse(await NordigenManager.Instance.Client.AccountsEndpoint.GetTransactions("60ad3e1b-9a1b-4dc1-aa63-9ab46e4a1821"));
        }
    }
}