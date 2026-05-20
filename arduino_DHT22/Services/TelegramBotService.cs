using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using arduino_DHT22.Services;

namespace arduino_DHT22.Services
{
    public class TelegramBotService
    {
        private readonly TelegramBotClient _bot;
        private readonly DatabaseService _db;
        private User? _me;

        public TelegramBotService(string token, DatabaseService db)
        {
            _db = db;
            _bot = new TelegramBotClient(token);
        }

        public async Task StartAsync(CancellationToken ct)
        {
            _me = await _bot.GetMe(ct);
            Console.WriteLine($"[Telegram] Bot @{_me.Username} is running.");

            _bot.OnError   += OnError;
            _bot.OnMessage += OnMessage;
        }

        private async Task OnError(Exception exception, HandleErrorSource source)
        {
            Console.WriteLine($"[Telegram] Error ({source}): {exception.Message}");
            await Task.Delay(2000);
        }

        private async Task OnMessage(Message msg, UpdateType type)
        {
            if (msg.Text is not { } text)
            {
                Console.WriteLine($"[Telegram] Received non-text message of type {msg.Type}, ignoring.");
                return;
            }

            Console.WriteLine($"[Telegram] Message from {msg.Chat.Id}: '{text}'");
            await ReplyWithLatestReading(msg.Chat);
        }

        private async Task ReplyWithLatestReading(Chat chat)
        {
            var reading = _db.GetLatestReading();

            string reply = reading is not null
                ? reading.ToString()
                : "⚠️ No sensor data available yet. Make sure the Arduino is connected.";

            await _bot.SendMessage(chat, reply);
        }
    }
}
