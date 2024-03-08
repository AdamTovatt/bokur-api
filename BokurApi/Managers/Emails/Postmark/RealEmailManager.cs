using BokurApi.Models.Bokur;

namespace BokurApi.Managers.Emails.Postmark
{
    public class RealEmailManager : IEmailManager
    {
        public Task SendEmailAsync(BokurEmail email)
        {
            throw new NotImplementedException();
        }
    }
}
