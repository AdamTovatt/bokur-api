namespace InvoiceGenerator.Models
{
    public class GenerationException : Exception
    {
        public GenerationException(string message) : base(message) { }
    }
}
