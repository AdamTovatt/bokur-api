using BokurApi.Helpers;
using BokurApi.Managers.Emails;
using BokurApi.Managers.Files;
using BokurApi.Managers.Files.Postgres;
using BokurApi.Managers.Transactions;
using BokurApi.Models;
using BokurApi.Models.Bokur;
using BokurApi.Models.Exceptions;
using BokurApi.Models.Http;
using BokurApi.RateLimiting;
using BokurApi.Repositories.Account;
using BokurApi.Repositories.Transaction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RobinTTY.NordigenApiClient.Models.Responses;
using System.Net;

namespace BokurApi.Controllers
{
    [ApiController]
    [Route("transaction")]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IFileManager _fileManager;

        public TransactionController(ITransactionRepository transactionRepository, IAccountRepository accountRepository, IFileManager fileManager)
        {
            _transactionRepository = transactionRepository;
            _accountRepository = accountRepository;
            _fileManager = fileManager;
        }

        public static DateTime? DefaultStartTime = new DateTime(2024, 1, 1); // never include transactions before this date

        [Authorize(AuthorizationRole.Admin)]
        [HttpPost("create-transaction")]
        [Limit(MaxRequests = 1, TimeWindow = 3)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.Created)]
        public async Task<ObjectResult> CreateRequisition([FromBody] BokurTransaction bokurTransaction) // to create a new transaction (should almost never be used, only for manually inserting balance)
        {
            int createdId = await _transactionRepository.CreateAsync(bokurTransaction);

            return new ApiResponse(createdId.ToString(), HttpStatusCode.Created);
        }

        [Authorize(AuthorizationRole.Admin)]
        [HttpPost("create-requsition")]
        [Limit(MaxRequests = 1, TimeWindow = 3)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.Created)]
        public async Task<ObjectResult> CreateRequisition(string? redirectUrl = null) // to create a new requisition
        {
            return new ApiResponse(await NordigenManager.Instance.CreateRequsition(redirectUrl), HttpStatusCode.Created);
        }

        [Authorize(AuthorizationRole.Admin)]
        [HttpPut("clean-duplicate-transactions")]
        [Limit(MaxRequests = 2, TimeWindow = 10)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        public async Task<ObjectResult> CleanDuplicateTransactions()
        {
            List<BokurTransaction> transactions = await _transactionRepository.GetAllThatRequiresActionAsync();

            List<DuplicateTransactionPair> duplicatePairs = DuplicateTransactionPair.CreateListOfDuplicates(transactions);

            foreach (DuplicateTransactionPair pair in duplicatePairs)
            {
                BokurTransaction transactionToRemove = pair.GetTransactionToRemove();

                transactionToRemove.Ignored = true;
                await _transactionRepository.UpdateAsync(transactionToRemove);
            }

            return new ApiResponse();
        }

        [AllowAnonymous]
        [HttpPost("check-for-new-transactions")]
        [Limit(MaxRequests = 20, TimeWindow = 10)]
        [ProducesResponseType(typeof(List<BokurTransaction>), (int)HttpStatusCode.OK)]
        public async Task<ObjectResult> UpdateBankData(DateTime? startDate = null, DateTime? endDate = null)
        {
            Requisition? requisition = await NordigenManager.Instance.GetLinkedRequisition();

            if (requisition == null)
                return new ApiResponse("No linked requisition was found. One has to be created.", HttpStatusCode.FailedDependency);

            if (startDate == null)
                startDate = DefaultStartTime;

            List<Transaction> nordigenTransactions = await NordigenManager.Instance.GetTransactionsAsync(requisition, startDate.ToDateOnly(), endDate.ToDateOnly());
            List<BokurTransaction> transactions = BokurTransaction.GetList(nordigenTransactions);
            List<string> existingIds = await _transactionRepository.GetExistingExternalIdsAsync();

            List<BokurTransaction> newTransactions = transactions.Where(x => x.ExternalId != null && !existingIds.Contains(x.ExternalId)).ToList().RemoveInternalTransactions();

            List<int> createdTransactionIds = new List<int>();

            foreach (BokurTransaction transaction in newTransactions) // create the new transactions
            {
                try
                {
                    createdTransactionIds.Add(await _transactionRepository.CreateAsync(transaction));
                }
                catch (ApiException) { } // if the transaction already existed, just skip it, we don't need to add it again
            }

            if (createdTransactionIds.Count > 0)
            {
                List<BokurAccount> accounts = await _accountRepository.GetAllAsync();
                if (accounts.Count > 0)
                {
                    string[] accountEmails = accounts.Where(x => x.Email != null).Select(x => x.Email).ToArray()!;

                    const int maxEmails = 5;
                    int sentEmails = 0;

                    foreach (int id in createdTransactionIds) // send email for each new transaction
                    {
                        if (sentEmails >= maxEmails)
                            break;

                        BokurTransaction? transaction = await _transactionRepository.GetByIdAsync(id);

                        if (transaction != null)
                        {
                            await EmailManager.Instance.SendEmailAsync(transaction.CreateNewTransactionEmail(to: accountEmails));
                            sentEmails++;
                        }
                    }
                }
            }

            return new ApiResponse(newTransactions);
        }

        [Authorize(AuthorizationRole.Admin)]
        [HttpGet("requisition/days-left")]
        [Limit(MaxRequests = 20, TimeWindow = 10)]
        [ProducesResponseType(typeof(int), (int)HttpStatusCode.OK)]
        public async Task<ObjectResult> GetRequisitionDaysLeft()
        {
            Requisition? requisition = await NordigenManager.Instance.GetLinkedRequisition();

            int daysLeft = 0;

            if (requisition != null)
                daysLeft = requisition.GetDaysLeft();

            return new ApiResponse(daysLeft);
        }

        [Authorize(AuthorizationRole.Admin)]
        [HttpGet("summary-of-all")]
        [Limit(MaxRequests = 20, TimeWindow = 10)]
        [ProducesResponseType(typeof(List<AccountSummary>), (int)HttpStatusCode.OK)]
        public async Task<ObjectResult> GetSummaryOfAll()
        {
            List<AccountSummary> summary = await _transactionRepository.GetSummaryAsync();

            return new ApiResponse(summary);
        }

        [Authorize(AuthorizationRole.Admin)]
        [HttpPut("update")]
        [Limit(MaxRequests = 20, TimeWindow = 10)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        public async Task<ObjectResult> UpdateSingle([FromBody] BokurTransaction transaction)
        {
            await _transactionRepository.UpdateAsync(transaction);
            return new ApiResponse("ok");
        }

        [Authorize(AuthorizationRole.Admin)]
        [HttpGet("{transactionId}")]
        [Limit(MaxRequests = 20, TimeWindow = 10)]
        [ProducesResponseType(typeof(BokurTransaction), (int)HttpStatusCode.OK)]
        public async Task<ObjectResult> GetSingle(int transactionId)
        {
            if (transactionId <= 0)
                return new ApiResponse("Non zero positive integer transactionId must be provided", HttpStatusCode.BadRequest);

            BokurTransaction? transaction = await _transactionRepository.GetByIdAsync(transactionId);

            if (transaction == null)
                return new ApiResponse($"No transaction with id {transactionId}", HttpStatusCode.BadRequest);

            return new ApiResponse(transaction);
        }

        [HttpPost("transfer/create")]
        [Limit(MaxRequests = 20, TimeWindow = 10)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.Created)]
        public async Task<ObjectResult> CreateTransfer(int parentTransactionId, int toAccountId, decimal amount)
        {
            bool succes = await _transactionRepository.CreateTransferAsync(parentTransactionId, toAccountId, amount);

            if (!succes)
                return new ApiResponse("Error when creating transfer", HttpStatusCode.InternalServerError);

            return new ApiResponse("ok", HttpStatusCode.Created);
        }

        [Authorize(AuthorizationRole.Admin)]
        [HttpGet("get-all-that-requires-action")]
        [Limit(MaxRequests = 20, TimeWindow = 10)]
        [ProducesResponseType(typeof(List<BokurTransaction>), (int)HttpStatusCode.OK)]
        public async Task<ObjectResult> GetAllThatRequiresAction()
        {
            return new ApiResponse(await _transactionRepository.GetAllThatRequiresActionAsync());
        }

        [Authorize(AuthorizationRole.Admin)]
        [HttpGet("get-all")]
        [Limit(MaxRequests = 20, TimeWindow = 10)]
        [ProducesResponseType(typeof(List<BokurTransaction>), (int)HttpStatusCode.OK)]
        public async Task<ObjectResult> GetAll(int pageSize = 10, int page = 0)
        {
            return new ApiResponse(await _transactionRepository.GetAllWithoutParentAsync(pageSize, page));
        }

        [Authorize(AuthorizationRole.Admin)]
        [HttpPut("{transactionId}/delete")]
        [Limit(MaxRequests = 20, TimeWindow = 10)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        public async Task<ObjectResult> DeleteTransaction(int transactionId)
        {
            try
            {
                await _transactionRepository.DeleteTransactionAsync(transactionId);

                return new ApiResponse("ok", HttpStatusCode.OK);
            }
            catch (ApiException exception)
            {
                return exception.Response;
            }
        }

        [Authorize(AuthorizationRole.Admin)]
        [HttpPut("{transactionId}/set-account")]
        [Limit(MaxRequests = 20, TimeWindow = 10)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        public async Task<ObjectResult> CreateTransfer(int transactionId, int accountId)
        {
            try
            {
                await _transactionRepository.SetAffectedAccountAsync(transactionId, accountId);

                return new ApiResponse("ok", HttpStatusCode.OK);
            }
            catch (ApiException exception)
            {
                return exception.Response;
            }
        }

        [Authorize(AuthorizationRole.Admin)]
        [HttpPut("{transactionId}/set-amount")]
        [Limit(MaxRequests = 20, TimeWindow = 10)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        public async Task<ObjectResult> CreateTransfer(int transactionId, decimal amount)
        {
            try
            {
                await _transactionRepository.SetTransactionValueAsync(transactionId, amount);

                return new ApiResponse("ok", HttpStatusCode.OK);
            }
            catch (ApiException exception)
            {
                return exception.Response;
            }
        }

        [Authorize(AuthorizationRole.Admin)]
        [HttpPut("{transactionId}/file/upload")]
        [Limit(MaxRequests = 20, TimeWindow = 10)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.Created)]
        public async Task<ObjectResult> UploadTransactionFile(IFormFile file, int transactionId)
        {
            if (file == null)
                return new ApiResponse("No file was provided in the request, should be IFormFile", HttpStatusCode.BadRequest);

            if (file.Length > 1000 * 1000 * 128)
                return new ApiResponse("File is too large, max size is 128MB", HttpStatusCode.BadRequest);

            BokurTransaction? transaction = await _transactionRepository.GetByIdAsync(transactionId);

            if (transaction == null)
                return new ApiResponse($"No transaction with id {transaction}", HttpStatusCode.BadRequest);

            if (transaction.AssociatedFileName != null)
                return new ApiResponse($"Transaction with id {transactionId} already has a file associated with it", HttpStatusCode.BadRequest);

            string fileName = file.FileName;
            if (await _fileManager.FileNameExistsAsync(fileName))
                return new ApiResponse($"A file with the name {fileName} already exists", HttpStatusCode.Conflict);

            using (MemoryStream stream = new MemoryStream(new byte[file.Length]))
            {
                await file.CopyToAsync(stream);

                bool saveFileResult = await _fileManager.SaveFileAsync(new BokurFile(fileName, stream.ToArray()));

                if (!saveFileResult)
                    return new ApiResponse("Error when saving file", HttpStatusCode.InternalServerError);
            }

            transaction.AssociatedFileName = fileName;
            await _transactionRepository.UpdateAsync(transaction);
            return new ApiResponse("ok", HttpStatusCode.Created);
        }

        [HttpDelete("{transactionId}/file/delete")]
        [Limit(MaxRequests = 20, TimeWindow = 10)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        public async Task<ObjectResult> DeleteTransactionFile(int transactionId)
        {
            BokurTransaction? transaction = await _transactionRepository.GetByIdAsync(transactionId);

            if (transaction == null)
                return new ApiResponse($"No transaction with id {transactionId}", HttpStatusCode.BadRequest);

            if (transaction.AssociatedFileName == null)
                return new ApiResponse($"The transaction with id {transactionId} doesn't have a file associated with it", HttpStatusCode.BadRequest);

            bool deleteFileResult = await _fileManager.DeleteFileAsync(transaction.AssociatedFileName);

            if (!deleteFileResult)
                return new ApiResponse("Error when deleting file", HttpStatusCode.InternalServerError);

            transaction.AssociatedFileName = null;

            await _transactionRepository.RemoveAssociatedFileAsync(transaction.Id);

            return new ApiResponse("ok");
        }

        [Authorize(AuthorizationRole.Admin)]
        [HttpGet("{transactionId}/file/download")]
        [Limit(MaxRequests = 20, TimeWindow = 10)]
        [ProducesResponseType(typeof(File), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> DownloadTransactionFile(int transactionId)
        {
            if (transactionId <= 0)
                return new ApiResponse("Non zero positive integer transactionId must be provided", HttpStatusCode.BadRequest);

            BokurTransaction? transaction = await _transactionRepository.GetByIdAsync((int)transactionId);

            if (transaction == null)
                return new ApiResponse($"No transaction with id {transactionId}", HttpStatusCode.BadRequest);

            if (transaction.AssociatedFileName == null)
                return new ApiResponse($"The transaction with id {transactionId} doesn't have a file associated with it", HttpStatusCode.BadRequest);

            BokurFile? file = await _fileManager.GetFileAsync(transaction.AssociatedFileName);

            if (file == null)
                return new ApiResponse($"No file could be found for transaction with id {transactionId} and filename {transaction.AssociatedFileName}", HttpStatusCode.BadRequest);

            if (file.Bytes == null)
                return new ApiResponse("Error when getting file", HttpStatusCode.InternalServerError);

            return File(file.Bytes, "application/octet-stream", file.Name);
        }
    }
}