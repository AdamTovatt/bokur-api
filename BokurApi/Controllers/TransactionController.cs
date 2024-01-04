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
        public static DateOnly DefaultStartDate = new DateOnly(2024, 1, 1); // never include transactions before this date

        [HttpPost("update-bank-data")]
        [Limit(MaxRequests = 20, TimeWindow = 10)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.Created)]
        public async Task<ObjectResult> UpdateBankData(DateTime? startDate = null, DateTime? endDate = null)
        {
            Requisition? requisition = await NordigenManager.Instance.GetLinkedRequisition();

            if (requisition == null)
                return new ApiResponse("No linked requisition was found. One has to be created.", HttpStatusCode.BadRequest);

            List<BokurTransaction> transactions = BokurTransaction.GetList(await NordigenManager.Instance.GetTransactionsAsync(requisition, startDate: DefaultStartDate));
            List<string> existingIds = await TransactionRepository.Instance.GetExistingExternalIdsAsync();

            List<BokurTransaction> newTransactions = transactions.Where(x => !existingIds.Contains(x.ExternalId)).ToList().RemoveInternalTransactions();

            foreach (BokurTransaction transaction in newTransactions)
                await TransactionRepository.Instance.CreateAsync(transaction);

            return new ApiResponse(newTransactions);
        }

        [HttpGet("get-bank-data")]
        [Limit(MaxRequests = 20, TimeWindow = 10)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.Created)]
        public async Task<ObjectResult> GetBankData()
        {
            Requisition? requisition = await NordigenManager.Instance.GetLinkedRequisition();

            if (requisition == null)
                return new ApiResponse("No linked requisition was found. One has to be created.", HttpStatusCode.BadRequest);

            List<Transaction> transactions = await NordigenManager.Instance.GetTransactionsAsync(requisition, startDate: DefaultStartDate);

            return new ApiResponse(BokurTransaction.GetList(transactions));
        }

        [HttpPost]
        [Limit(MaxRequests = 10, TimeWindow = 10)]
        [ProducesResponseType(typeof(bool), (int)HttpStatusCode.Created)]
        public async Task<ObjectResult> CreateRequisition()
        {
            Requisition? requisition = await NordigenManager.Instance.GetLinkedRequisition();

            if (requisition != null)
                return new ApiResponse("A linked requisition already exists.", HttpStatusCode.Conflict);

            await NordigenManager.Instance.CreateRequsition();

            return new ApiResponse(true, HttpStatusCode.Created);
        }
    }
}