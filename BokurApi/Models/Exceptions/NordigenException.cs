using RobinTTY.NordigenApiClient.Models.Errors;

namespace BokurApi.Models.Exceptions
{
    public class NordigenException : Exception
    {
        public NordigenException(BasicError error) : base(error.Summary) { }
    }
}
