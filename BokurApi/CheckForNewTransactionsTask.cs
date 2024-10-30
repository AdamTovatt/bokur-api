using BokurApi.Helpers;
using BokurApi.Managers.Emails;
using BokurApi.Managers.Transactions;
using BokurApi.Models.Bokur;
using BokurApi.Models.Exceptions;
using BokurApi.Repositories;
using RobinTTY.NordigenApiClient.Models.Responses;
using Sakur.WebApiUtilities.TaskScheduling;

namespace BokurApi
{
    public class CheckForNewTransactionsTask : TimeOfDayTask
    {
        public static DateTime DefaultStartTime = new DateTime(2024, 1, 1); // never include transactions before this date
        public override TimeSpan ScheduledTime => new TimeSpan(9, 0, 0); // 9:00 AM

        private ILogger logger;

        public CheckForNewTransactionsTask(ILogger<CheckForNewTransactionsTask> logger)
        {
            this.logger = logger;
        }

        public override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                logger.LogInformation("Will check for new transactions");

                Requisition? requisition = await NordigenManager.Instance.GetLinkedRequisition();

                if (requisition == null)
                    throw new Exception("Error when checking for new transactions: No linked requisition was found. One has to be created.");

                DateTime startDate = DefaultStartTime;
                DateTime endDate = DateTime.Now;

                List<Transaction> nordigenTransactions = await NordigenManager.Instance.GetTransactionsAsync(requisition, startDate.ToDateOnly(), endDate.ToDateOnly());
                List<BokurTransaction> transactions = BokurTransaction.GetList(nordigenTransactions);
                List<string> existingIds = await TransactionRepository.Instance.GetExistingExternalIdsAsync();

                List<BokurTransaction> newTransactions = transactions.Where(x => x.ExternalId != null && !existingIds.Contains(x.ExternalId)).ToList().RemoveInternalTransactions();

                List<int> createdTransactionIds = new List<int>();

                foreach (BokurTransaction transaction in newTransactions) // create the new transactions
                {
                    try
                    {
                        createdTransactionIds.Add(await TransactionRepository.Instance.CreateAsync(transaction));
                    }
                    catch (ApiException) { } // if the transaction already existed, just skip it, we don't need to add it again
                }

                if (createdTransactionIds.Count > 0)
                {
                    List<BokurAccount> accounts = await AccountRepository.Instance.GetAllAsync();
                    if (accounts.Count > 0)
                    {
                        string[] accountEmails = accounts.Where(x => x.Email != null).Select(x => x.Email).ToArray()!;

                        const int maxEmails = 5;
                        int sentEmails = 0;

                        foreach (int id in createdTransactionIds) // send email for each new transaction
                        {
                            if (sentEmails >= maxEmails)
                                break;

                            BokurTransaction? transaction = await TransactionRepository.Instance.GetByIdAsync(id);

                            if (transaction != null)
                            {
                                await EmailManager.Instance.SendEmailAsync(transaction.CreateNewTransactionEmail(to: accountEmails));
                                sentEmails++;
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                logger.LogError(exception: exception, message: exception.Message);
            }
        }
    }
}
