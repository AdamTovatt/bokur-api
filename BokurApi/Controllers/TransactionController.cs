using BokurApi.Helpers;
using BokurApi.Managers.Transactions;
using BokurApi.Models.Bokur;
using BokurApi.Models.Http;
using BokurApi.RateLimiting;
using BokurApi.Repositories;
using Microsoft.AspNetCore.Mvc;
using RobinTTY.NordigenApiClient.Models.Responses;
using System.Net;

namespace BokurApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TransactionController : ControllerBase
    {
        public static DateTime? DefaultStartTime = new DateTime(2024, 1, 1); // never include transactions before this date

        [HttpPost("create-requsition")]
        [Limit(MaxRequests = 1, TimeWindow = 3)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.Created)]
        public async Task<ObjectResult> CreateRequisition(string? redirectUrl = null) // to create a new requisition
        {
            Requisition? requisition = await NordigenManager.Instance.GetLinkedRequisition();

            if (requisition != null)
                return new ApiResponse("A linked requisition already exists.", HttpStatusCode.Conflict);

            return new ApiResponse(await NordigenManager.Instance.CreateRequsition(redirectUrl), HttpStatusCode.Created);
        }

        [HttpPost("check-for-new-transactions")]
        [Limit(MaxRequests = 20, TimeWindow = 10)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.Created)]
        public async Task<ObjectResult> UpdateBankData(DateTime? startDate = null, DateTime? endDate = null)
        {
            Requisition? requisition = await NordigenManager.Instance.GetLinkedRequisition();

            if (requisition == null)
                return new ApiResponse("No linked requisition was found. One has to be created.", HttpStatusCode.BadRequest);

            if(startDate == null)
                startDate = DefaultStartTime;

            List<Transaction> nordigenTransactions = await NordigenManager.Instance.GetTransactionsAsync(requisition, startDate.ToDateOnly(), endDate.ToDateOnly());
            List<BokurTransaction> transactions = BokurTransaction.GetList(nordigenTransactions);
            List<string> existingIds = await TransactionRepository.Instance.GetExistingExternalIdsAsync();

            List<BokurTransaction> newTransactions = transactions.Where(x => !existingIds.Contains(x.ExternalId)).ToList().RemoveInternalTransactions();

            foreach (BokurTransaction transaction in newTransactions)
                await TransactionRepository.Instance.CreateAsync(transaction);

            return new ApiResponse(newTransactions);
        }

        [HttpGet("get-transactions")]
        [Limit(MaxRequests = 20, TimeWindow = 10)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.Created)]
        public async Task<ObjectResult> GetBankData()
        {
            return new ApiResponse(await TransactionRepository.Instance.GetAllAsync());
        }
    }
}