using BokurApi.Models.Bokur;
using PostmarkDotNet;

namespace BokurApi.Managers.Emails.Postmark
{
    public class RealEmailManager : IEmailManager
    {
        public RealEmailManager()
        {
            this.client = new PostmarkClient(Environment.GetEnvironmentVariable("POSTMARK_API_TOKEN")!);
        }

        private readonly PostmarkClient client;

        public async Task SendEmailAsync(BokurEmail email)
        {
            List<PostmarkMessage> messages = new List<PostmarkMessage>();

            foreach (string recipient in email.To)
            {
                PostmarkMessage message = new PostmarkMessage()
                {
                    To = recipient,
                    From = "bokur@sakur.se",
                    TrackOpens = true,
                    Subject = email.Subject,
                    HtmlBody = email.Content,
                    Tag = "new-transaction",
                };

                messages.Add(message);
            }

            IEnumerable<PostmarkResponse> sendResult = await client.SendMessagesAsync(messages);

            foreach (PostmarkResponse result in sendResult)
            {
                if (result.Status != PostmarkStatus.Success)
                    throw new Exception($"Failed to send email to {result.To} with status {result.Status}. Message: {result.Message}");
            }
        }
    }
}
