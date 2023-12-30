using RobinTTY.NordigenApiClient.Models;
using RobinTTY.NordigenApiClient;
using BokurApi.Helpers;

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
    }
}
