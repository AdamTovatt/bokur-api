using System.Text;
using System.Text.Json;

namespace BokurApi.RateLimiting
{
    public class ClientStatistics : Queue<DateTime>
    {
        public ClientStatistics() : base() { }
        public ClientStatistics(int maxRequests) : base(maxRequests) { }
        public ClientStatistics(int maxRequests, DateTime initialValue) : base(maxRequests)
        {
            Enqueue(initialValue);
        }

        public ClientStatistics(IEnumerable<DateTime> values) : base(values) { }

        public byte[] ToByteArray()
        {
            return Encoding.Default.GetBytes(JsonSerializer.Serialize(new List<DateTime>(this)));
        }

        public static ClientStatistics FromByteArray(byte[]? bytes)
        {
            if (bytes == null)
                return new ClientStatistics();

            IEnumerable<DateTime>? requests = JsonSerializer.Deserialize<List<DateTime>>(Encoding.Default.GetString(bytes));

            if (requests == null)
                return new ClientStatistics();

            return new ClientStatistics(requests);
        }
    }
}
