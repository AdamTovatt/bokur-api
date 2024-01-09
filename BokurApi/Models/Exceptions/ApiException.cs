using BokurApi.Models.Http;
using System.Net;

namespace BokurApi.Models.Exceptions
{
    public class ApiException : Exception
    {
        public ApiResponse Response { get; private set; }

        public ApiException(string message, HttpStatusCode statusCode) : base(message)
        {
            Response = new ApiResponse(message, statusCode);
        }
    }
}
