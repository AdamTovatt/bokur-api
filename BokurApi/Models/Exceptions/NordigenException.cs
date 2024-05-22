using RobinTTY.NordigenApiClient.Models.Responses;

namespace BokurApi.Models.Exceptions
{
    public class NordigenException : Exception
    {
        public NordigenException(BasicResponse error) : base(error.Summary) { }
    }
}
