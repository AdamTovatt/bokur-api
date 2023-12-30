using RobinTTY.NordigenApiClient.Models;
using RobinTTY.NordigenApiClient;
using BokurApi.Helpers;
using RobinTTY.NordigenApiClient.Models.Responses;
using RobinTTY.NordigenApiClient.Models.Errors;
using RobinTTY.NordigenApiClient.Models.Requests;

namespace BokurApi.Managers.Transactions
{
    public class NordigenManager
    {
        public static NordigenManager Instance { get { if (_instance == null) _instance = new NordigenManager(); return _instance; } }

        private static NordigenManager? _instance;

        public NordigenClient Client { get; set; }

        public NordigenManager()
        {
            HttpClient httpClient = new HttpClient();
            NordigenClientCredentials credentials = new NordigenClientCredentials(EnvironmentHelper.GetNordigenId(), EnvironmentHelper.GetNordigenKey());
            Client = new NordigenClient(httpClient, credentials);
        }

        /// <summary>
        /// Will try to get a requistion that is linked
        /// </summary>
        /// <returns>The linked requsition that was found with the most available accounts, or null if none was found</returns>
        /// <exception cref="Exception"></exception>
        public async Task<Requisition?> GetLinkedRequisition()
        {
            NordigenApiResponse<ResponsePage<Requisition>, BasicError> response = await Client.RequisitionsEndpoint.GetRequisitions(0, 0);

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
            string internalReference = EnvironmentHelper.GetInternalReference();
            string language = EnvironmentHelper.GetUserLanguage();

            CreateRequisitionRequest requisitionRequest = new CreateRequisitionRequest(new Uri(url), bankId, internalReference, language);
            NordigenApiResponse<Requisition, CreateRequisitionError> response = await Client.RequisitionsEndpoint.CreateRequisition(requisitionRequest);

            if (!response.IsSuccess)
                throw new Exception(response.Error.Summary);

            return response.Result.AuthenticationLink.ToString();
        }
    }
}
