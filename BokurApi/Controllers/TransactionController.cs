using BokurApi.Helpers;
using BokurApi.Managers.Transactions;
using BokurApi.Models.Http;
using BokurApi.RateLimiting;
using Microsoft.AspNetCore.Mvc;
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

            if (requisition == null)
                return new ApiResponse("No linked requisition was found. One has to be created.",  HttpStatusCode.BadRequest);

            return new ApiResponse(await NordigenManager.Instance.GetTransactionsAsync(requisition));
        }
    }
}