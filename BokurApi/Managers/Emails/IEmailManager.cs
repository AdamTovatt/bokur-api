using BokurApi.Models.Bokur;

namespace BokurApi.Managers.Emails
{
    public interface IEmailManager
    {
        public Task SendEmailAsync(BokurEmail email);
    }
}
