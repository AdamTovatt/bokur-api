namespace BokurApi.Models.Bokur
{
    public class BokurEmail
    {
        public string Content { get; set; }
        public string Subject { get; set; }
        public string To { get; set; }

        public BokurEmail(string content, string subject, string to)
        {
            Content = content;
            Subject = subject;
            To = to;
        }
    }
}
