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
            //CreateRequisitionRequest requisitionRequest = new CreateRequisitionRequest(new Uri(EnvironmentHelper.GetRedirectUrl()), EnvironmentHelper.GetBankId(), EnvironmentHelper.GetInternalReference(), EnvironmentHelper.GetUserLanguage());
            //NordigenApiResponse<Requisition, CreateRequisitionError> response = await NordigenManager.Instance.Client.RequisitionsEndpoint.CreateRequisition(requisitionRequest);

            //await NordigenManager.Instance.Client.RequisitionsEndpoint.GetRequisition("8fe4296e-337b-47a5-bb50-d5cd8c8c27ee")

            return new ApiResponse(await NordigenManager.Instance.Client.AccountsEndpoint.GetTransactions("60ad3e1b-9a1b-4dc1-aa63-9ab46e4a1821"));
        }
    }
}