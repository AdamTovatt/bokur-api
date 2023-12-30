namespace BokurApi.Models
{
    public class BokurFile
    {
        public string Name { get; set; }
        public byte[] Bytes { get; set; }

        public BokurFile(string name, byte[] bytes)
        {
            Name = name;
            Bytes = bytes;
        }
    }
}
