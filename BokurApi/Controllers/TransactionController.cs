using BokurApi.Helpers;
using BokurApi.Managers.Files.Postgres;
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

            if (startDate == null)
                startDate = DefaultStartTime;

            List<Transaction> nordigenTransactions = await NordigenManager.Instance.GetTransactionsAsync(requisition, startDate.ToDateOnly(), endDate.ToDateOnly());
            List<BokurTransaction> transactions = BokurTransaction.GetList(nordigenTransactions);
            List<string> existingIds = await TransactionRepository.Instance.GetExistingExternalIdsAsync();

            List<BokurTransaction> newTransactions = transactions.Where(x => !existingIds.Contains(x.ExternalId)).ToList().RemoveInternalTransactions();

            foreach (BokurTransaction transaction in newTransactions)
                await TransactionRepository.Instance.CreateAsync(transaction);

            return new ApiResponse(newTransactions);
        }

        [HttpPut("upload-file")]
        [Limit(MaxRequests = 20, TimeWindow = 10)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.Created)]
        public async Task<ObjectResult> UploadFile(IFormFile file, int transactionId)
        {
            if (file == null)
                return new ApiResponse("No file was provided in the request, should be IFormFile", HttpStatusCode.BadRequest);

            if (file.Length > 1000 * 1000 * 128)
                return new ApiResponse("File is too large, max size is 128MB", HttpStatusCode.BadRequest);

            BokurTransaction? transaction = await TransactionRepository.Instance.GetByIdAsync(transactionId);

            if (transaction == null)
                return new ApiResponse($"No transaction with id {transaction}", HttpStatusCode.BadRequest);

            string fileName = file.FileName;
            if (await FileManager.Instance.FileNameExistsAsync(fileName))
                return new ApiResponse($"A file with the name {fileName} already exists", HttpStatusCode.Conflict);

            using (MemoryStream stream = new MemoryStream(new byte[file.Length]))
            {
                await file.CopyToAsync(stream);

                bool saveFileResult = await FileManager.Instance.SaveFileAsync(new BokurFile(fileName, stream.ToArray()));

                if (!saveFileResult)
                    return new ApiResponse("Error when saving file", HttpStatusCode.InternalServerError);
            }

            transaction.AssociatedFileName = fileName;
            await TransactionRepository.Instance.UpdateAsync(transaction);
            return new ApiResponse("ok");
        }

        [HttpPut("update-single")]
        [Limit(MaxRequests = 20, TimeWindow = 10)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.Created)]
        public async Task<ObjectResult> UpdateSingle(BokurTransaction transaction)
        {
            await TransactionRepository.Instance.UpdateAsync(transaction);
            return new ApiResponse("ok");
        }

        [HttpPut("get-single")]
        [Limit(MaxRequests = 20, TimeWindow = 10)]
        [ProducesResponseType(typeof(BokurTransaction), (int)HttpStatusCode.Created)]
        public async Task<ObjectResult> GetSingle(int transactionId)
        {
            BokurTransaction? transaction = await TransactionRepository.Instance.GetByIdAsync(transactionId);

            if (transaction == null)
                return new ApiResponse($"No transaction with id {transactionId}", HttpStatusCode.BadRequest);

            return new ApiResponse(transaction);
        }

        [HttpGet("get-all")]
        [Limit(MaxRequests = 20, TimeWindow = 10)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.Created)]
        public async Task<ObjectResult> GetAll()
        {
            return new ApiResponse(await TransactionRepository.Instance.GetAllAsync());
        }
    }
}