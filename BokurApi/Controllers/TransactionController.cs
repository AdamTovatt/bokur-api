using BokurApi.Helpers;
using BokurApi.Managers.Files.Postgres;
using BokurApi.Managers.Transactions;
using BokurApi.Models;
using BokurApi.Models.Bokur;
using BokurApi.Models.Http;
using BokurApi.RateLimiting;
using BokurApi.Repositories;
using Microsoft.AspNetCore.Authorization;
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

        [Authorize(AuthorizationRole.Admin)]
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
        [ProducesResponseType(typeof(List<BokurTransaction>), (int)HttpStatusCode.OK)]
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

        [Authorize(AuthorizationRole.Admin)]
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

            if (transaction.AssociatedFileName != null)
                return new ApiResponse($"Transaction with id {transactionId} already has a file associated with it", HttpStatusCode.BadRequest);

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
            return new ApiResponse("ok", HttpStatusCode.Created);
        }

        [Authorize(AuthorizationRole.Admin)]
        [HttpPut("update-single")]
        [Limit(MaxRequests = 20, TimeWindow = 10)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        public async Task<ObjectResult> UpdateSingle(BokurTransaction transaction)
        {
            await TransactionRepository.Instance.UpdateAsync(transaction);
            return new ApiResponse("ok");
        }

        [Authorize(AuthorizationRole.Admin)]
        [HttpPut("get-single")]
        [Limit(MaxRequests = 20, TimeWindow = 10)]
        [ProducesResponseType(typeof(BokurTransaction), (int)HttpStatusCode.OK)]
        public async Task<ObjectResult> GetSingle(int transactionId)
        {
            BokurTransaction? transaction = await TransactionRepository.Instance.GetByIdAsync(transactionId);

            if (transaction == null)
                return new ApiResponse($"No transaction with id {transactionId}", HttpStatusCode.BadRequest);

            return new ApiResponse(transaction);
        }

        //[Authorize(AuthorizationRole.Admin)]
        [HttpGet("get-all-that-requires-action")]
        [Limit(MaxRequests = 20, TimeWindow = 10)]
        [ProducesResponseType(typeof(List<BokurTransaction>), (int)HttpStatusCode.OK)]
        public async Task<ObjectResult> GetAllThatRequiresAction()
        {
            return new ApiResponse(await TransactionRepository.Instance.GetAllThatRequiresActionAsync());
        }

        //[Authorize(AuthorizationRole.Admin)]
        [HttpGet("get-all")]
        [Limit(MaxRequests = 20, TimeWindow = 10)]
        [ProducesResponseType(typeof(List<BokurTransaction>), (int)HttpStatusCode.OK)]
        public async Task<ObjectResult> GetAll()
        {
            return new ApiResponse(await TransactionRepository.Instance.GetAllAsync());
        }

        [Authorize(AuthorizationRole.Admin)]
        [HttpGet("download-file")]
        [Limit(MaxRequests = 20, TimeWindow = 10)]
        [ProducesResponseType(typeof(File), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> DownloadFile(int? transactionId = null, string? fileName = null)
        {
            if (transactionId == null && fileName == null)
                return new ApiResponse("Either transactionId or fileName must be provided in query string", HttpStatusCode.BadRequest);

            byte[]? bytes = null;

            if (fileName != null)
            {
                BokurFile? file = await FileManager.Instance.GetFileAsync(fileName);

                if(file == null)
                    return new ApiResponse($"No file called {fileName} exists", HttpStatusCode.BadRequest);

                fileName = file.Name;
                bytes = file.Bytes;
            }
            else if(transactionId != null)
            {
                BokurTransaction? transaction = await TransactionRepository.Instance.GetByIdAsync((int)transactionId);

                if(transaction == null)
                    return new ApiResponse($"No transaction with id {transactionId}", HttpStatusCode.BadRequest);

                if (transaction.AssociatedFileName == null)
                    return new ApiResponse($"The transaction with id {transactionId} doesn't have a file associated with it", HttpStatusCode.BadRequest);

                BokurFile? file = await FileManager.Instance.GetFileAsync(transaction.AssociatedFileName);

                if (file == null)
                    return new ApiResponse($"No file called {fileName} exists", HttpStatusCode.BadRequest);

                fileName = file.Name;
                bytes = file.Bytes;
            }

            if(bytes == null)
                return new ApiResponse("Error when getting file", HttpStatusCode.InternalServerError);

            return File(bytes, "application/octet-stream", fileName);
        }
    }
}