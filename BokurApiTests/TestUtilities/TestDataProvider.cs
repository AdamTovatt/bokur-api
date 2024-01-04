using BokurApi.Models.Bokur;

namespace BokurApiTests.TestUtilities
{
    public static class TestDataProvider
    {
        public static BokurTransaction BokurTransaction1
        {
            get
            {
                return new BokurTransaction(0, "3905739839034", "Test transaction 1", 100, new DateTime(2023, 12, 02), null, null, false);
            }
        }

        public static BokurTransaction BokurTransaction2
        {
            get
            {
                return new BokurTransaction(0, "8972398723293", "Test transaction 2", 21.34m, new DateTime(2023, 11, 14), null, null, false);
            }
        }
    }
}
