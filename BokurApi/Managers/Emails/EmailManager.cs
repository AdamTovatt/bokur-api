using BokurApi.Managers.Emails.Postmark;

namespace BokurApi.Managers.Emails
{
    public class EmailManager
    {
        private static IEmailManager? _instance;
        public static IEmailManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    if (GlobalSettings.MocketEnvironment)
                        _instance = new MockedEmailManager();
                    else
                        _instance = new RealEmailManager();
                }

                return _instance;
            }
        }
    }
}
