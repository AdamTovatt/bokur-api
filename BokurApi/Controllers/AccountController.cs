using BokurApi.Models.Http;
using BokurApi.RateLimiting;
using BokurApi.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace BokurApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AccountController : ControllerBase
    {
        [HttpPost("get-all")]
        [Limit(MaxRequests = 20, TimeWindow = 10)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.Created)]
        public async Task<ObjectResult> GetAllAccounts()
        {
            return new ApiResponse(await AccountRepository.Instance.GetAllAsync());
        }
    }
}
