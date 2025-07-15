using BokurApi.Helpers;
using BokurApi.Models;
using BokurApi.Models.Bokur;
using BokurApi.Models.Http;
using BokurApi.RateLimiting;
using BokurApi.Repositories.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace BokurApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly SummaryHelper _summaryHelper;
        private readonly IAccountRepository _accountRepository;

        public AccountController(SummaryHelper summaryHelper, IAccountRepository accountRepository)
        {
            _summaryHelper = summaryHelper;
            _accountRepository = accountRepository;
        }

        [Authorize(AuthorizationRole.Admin)]
        [HttpGet("get-all")]
        [Limit(MaxRequests = 20, TimeWindow = 10)]
        [ProducesResponseType(typeof(List<BokurAccount>), (int)HttpStatusCode.Created)]
        public async Task<ObjectResult> GetAllAccounts()
        {
            return new ApiResponse(await _accountRepository.GetAllAsync());
        }

        //[Authorize(AuthorizationRole.Admin)]
        [HttpGet("monthly-summary")]
        [Limit(MaxRequests = 20, TimeWindow = 1)]
        [ProducesResponseType(typeof(List<SummaryPerMonth>), (int)HttpStatusCode.Created)]
        public async Task<ObjectResult> GetMonthlySummary(DateTime? startTime = null, DateTime? endTime = null)
        {
            if (startTime == null && endTime == null)
            {
                endTime = DateTime.Now;
                startTime = endTime + TimeSpan.FromDays(-30 * 12);
            }
            else if (startTime == null || endTime == null)
            {
                return new ApiResponse("Invalid values for query parameters startTime and endTime.", HttpStatusCode.BadRequest);
            }

            return new ApiResponse(await _summaryHelper.CreateMonthlySummaryAsync(startTime.Value, endTime.Value));
        }
    }
}
