namespace InvoiceGenerator.Models.Data
{
    public class Time
    {
        public string? Name { get; set; }
        public string? Project { get; set; }
        public string? Client { get; set; }
        public int Amount { get { return (int)Math.Round(amount); } }
        public string? Description { get; set; }

        public decimal RawAmount { get { return amount; } }
        private decimal amount;

        public Time() { }

        public Time(string? name, string? project, string? client, decimal amount)
        {
            Name = name;
            Project = project;
            Client = client;
            this.amount = amount;
        }

        public static List<Time> CollapseTimes(List<Time> times)
        {
            Dictionary<string, Time> result = new Dictionary<string, Time>();

            foreach (Time time in times)
            {
                if (time.Name == null)
                    throw new Exception("Found a time without a name, it is not possible to know who logged this time");

                if (!result.ContainsKey(time.Name))
                    result[time.Name] = new Time(time.Name, time.Project, time.Client, 0);

                result[time.Name].amount += time.amount;
            }

            return result.Values.ToList();
        }
    }
}
