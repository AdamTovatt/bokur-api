using RobinTTY.NordigenApiClient.Models;
using RobinTTY.NordigenApiClient;
using BokurApi.Helpers;
using RobinTTY.NordigenApiClient.Models.Responses;
using RobinTTY.NordigenApiClient.Models.Errors;
using RobinTTY.NordigenApiClient.Models.Requests;
using BokurApi.Models.Exceptions;
using System.Collections.Concurrent;

namespace BokurApi.Managers.Transactions
{
    public class NordigenManager
    {
        /// <summary>
        /// The singleton instance of the nordigen manager
        /// </summary>
        public static NordigenManager Instance { get { if (_instance == null) _instance = new NordigenManager(); return _instance; } }

        private static NordigenManager? _instance;

        /// <summary>
        /// The nordigen client that the manager uses
        /// </summary>
        public NordigenClient Client { get; set; }

        public NordigenManager()
        {
            HttpClient httpClient = new HttpClient();
            NordigenClientCredentials credentials = new NordigenClientCredentials(EnvironmentHelper.GetNordigenId(), EnvironmentHelper.GetNordigenKey());
            Client = new NordigenClient(httpClient, credentials);
        }

        /// <summary>
        /// Will get a list of transactions that have been booked
        /// </summary>
        /// <param name="requisition">The requisitino to list the transactions for</param>
        /// <param name="startDate">Optional start date</param>
        /// <param name="endDate">Optional end date</param>
        /// <returns>A list of transactions</returns>
        /// <exception cref="NordigenException"></exception>
        public async Task<List<Transaction>> GetTransactionsAsync(Requisition requisition, DateOnly? startDate = null, DateOnly? endDate = null)
        {
            List<Transaction> result = new List<Transaction>();
            List<NordigenApiResponse<AccountTransactions, AccountsError>> responses = new List<NordigenApiResponse<AccountTransactions, AccountsError>>();
            List<Task> tasks = new List<Task>();

            foreach (Guid accountId in requisition.Accounts)
            {
                tasks.Add(Task.Run(async () =>
                {
                    responses.Add(await Client.AccountsEndpoint.GetTransactions(accountId, startDate, endDate));
                }));
            }

            await Task.WhenAll(tasks);

            foreach (NordigenApiResponse<AccountTransactions, AccountsError> response in responses)
            {
                if (!response.IsSuccess)
                    throw new NordigenException(response.Error);

                result.AddRange(response.Result.BookedTransactions);
            }

            return result;
        }

        /// <summary>
        /// Will try to get a requistion that is linked
        /// </summary>
        /// <returns>The linked requsition that was found with the most available accounts, or null if none was found</returns>
        /// <exception cref="Exception"></exception>
        public async Task<Requisition?> GetLinkedRequisition()
        {
            NordigenApiResponse<ResponsePage<Requisition>, BasicResponse> response = await Client.RequisitionsEndpoint.GetRequisitions(0, 0);

            if (response.Error != null)
                throw new Exception(response.Error.Summary);

            if (response.Result == null)
                throw new Exception("Missing response value when getting requsitions");

            Requisition? requisition = response.Result.Results
                .Where(x => x.Status == RequisitionStatus.Linked)
                .MaxBy(x => x.Accounts.Count);

            return requisition;
        }

        /// <summary>
        /// Will create a new requisition and return the authentication url
        /// </summary>
        /// <param name="redirectUrl">The redirect url to redirect to when the authentication is completed. Is optional, if set to null, the value from the back end env variables will be used</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<string> CreateRequsition(string? redirectUrl = null)
        {
            string url = redirectUrl == null ? EnvironmentHelper.GetRedirectUrl() : redirectUrl;
            string bankId = EnvironmentHelper.GetBankId();
            string internalReference = EnvironmentHelper.GetInternalReference(); // this is not used anymore, it should be null when sending to the api. This row can be removed
            string language = EnvironmentHelper.GetUserLanguage();

            NordigenApiResponse<Requisition, CreateRequisitionError> response = await Client.RequisitionsEndpoint.CreateRequisition(bankId, new Uri(url), reference: null, userLanguage: language);

            if (!response.IsSuccess)
                throw new Exception(response.Error.Summary);

            return response.Result.AuthenticationLink.ToString();
        }
    }
}
