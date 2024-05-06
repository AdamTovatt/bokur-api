using InvoiceGenerator.Models;
using InvoiceGenerator.Models.Configuration;
using InvoiceGenerator.Models.Data;

namespace InvoiceGenerator.Manager
{
    public class InvoiceManager
    {
        public GeneralInformation Configuration { get; set; }

        public InvoiceManager(GeneralInformation configuration)
        {
            Configuration = configuration;
        }

        public Invoice CreateInvoice(InvoiceInformation instance, TimeExport timeExport)
        {
            return new Invoice(instance, Configuration, timeExport);
        }
    }
}
