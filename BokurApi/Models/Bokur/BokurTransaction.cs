namespace BokurApi.Models.Bokur
{
    public class BokurTransaction
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Value { get; set; }
        public DateTime TimeOfCreation { get; set; }
        public BokurFile? AssociatedFile { get; set; }
        public BokurAccount? TargetAccount { get; set; }

        public BokurTransaction(int id, string name, decimal value, DateTime timeOfCreation, BokurFile? associatedFile, BokurAccount? targetAccount)
        {
            Id = id;
            Name = name;
            Value = value;
            TimeOfCreation = timeOfCreation;
            AssociatedFile = associatedFile;
            TargetAccount = targetAccount;
        }
    }
}
