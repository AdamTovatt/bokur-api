using BokurApi.Models;
using BokurApi.Models.Bokur;
using BokurApi.Models.Http;
using BokurApi.RateLimiting;
using BokurApi.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace BokurApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AccountController : ControllerBase
    {
        [Authorize(AuthorizationRole.Admin)]
        [HttpGet("get-all")]
        [Limit(MaxRequests = 20, TimeWindow = 10)]
        [ProducesResponseType(typeof(List<BokurAccount>), (int)HttpStatusCode.Created)]
        public async Task<ObjectResult> GetAllAccounts()
        {
            return new ApiResponse(await AccountRepository.Instance.GetAllAsync());
        }
    }
}
