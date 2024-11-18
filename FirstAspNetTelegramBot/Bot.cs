using Telegram.Bot;

namespace FirstAspNetTelegramBot
{
    public class Bot
    {
        private static TelegramBotClient client { get; set; }

        public static TelegramBotClient GetTelegramBot()
        {
            if (client != null)
            {
                return client;
            }
            client = new TelegramBotClient("7772065367:AAFropgz3DuxYr35RucG8BrRvsX-O7IMSk4");
            return client;
        }
    }
}
