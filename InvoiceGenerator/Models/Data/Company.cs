namespace InvoiceGenerator.Models.Data
{
    public class Company
    {
        public string? Name { get; set; }
        public Address? Address { get; set; }
        public string? OrganizationNumber { get; set; }
        public Person? Reference { get; set; }
    }
}
