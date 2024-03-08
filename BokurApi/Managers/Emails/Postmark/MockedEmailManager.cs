using BokurApi.Models.Bokur;

namespace BokurApi.Managers.Emails.Postmark
{
    public class MockedEmailManager : IEmailManager
    {
        public async Task SendEmailAsync(BokurEmail email)
        {
            await Task.Delay(50);
        }
    }
}
