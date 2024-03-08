namespace BokurApi.Models.Bokur
{
    public class BokurAccount
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Email { get; set; }

        public BokurAccount(int id, string name, string? email)
        {
            Id = id;
            Name = name;
            Email = email;
        }
    }
}
