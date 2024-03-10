namespace BokurApi.Models.Bokur
{
    public class BokurEmail
    {
        public string Content { get; set; }
        public string Subject { get; set; }

        /// <summary>
        /// An array of email addresses to send the email to
        /// </summary>
        public string[] To { get; set; }

        public BokurEmail(string content, string subject, params string[] to)
        {
            Content = content;
            Subject = subject;
            To = to;
        }
    }
}
