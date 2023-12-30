using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace BokurApi.Models.Http
{
    public class ApiResponse : ObjectResult
    {
        public ApiResponse(HttpStatusCode statusCode = HttpStatusCode.OK) : base(statusCode)
        {
            StatusCode = (int)statusCode;
        }

        public ApiResponse(string message, HttpStatusCode statusCode = HttpStatusCode.OK) : base(statusCode)
        {
            Value = new { message };
            StatusCode = (int)statusCode;
        }

        public ApiResponse(object value, HttpStatusCode statusCode = HttpStatusCode.OK) : base(statusCode)
        {
            Value = value;
            StatusCode = (int)statusCode;
        }
    }
}
