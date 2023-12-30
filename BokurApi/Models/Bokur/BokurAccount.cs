namespace BokurApi.Models.Bokur
{
    public class BokurAccount
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public BokurAccount(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
