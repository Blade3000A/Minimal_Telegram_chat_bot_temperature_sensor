using Microsoft.Extensions.Configuration;
using arduino_DHT22.Services;

namespace arduino_DHT22
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Load configuration from appsettings.json
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            string telegramToken    = config["Telegram:BotToken"]
                ?? throw new InvalidOperationException("Missing Telegram:BotToken in appsettings.json");
            string connectionString = config["Database:ConnectionString"]
                ?? throw new InvalidOperationException("Missing Database:ConnectionString in appsettings.json");
            string portName         = config["Serial:PortName"] ?? "/dev/ttyACM0";
            int    baudRate         = int.TryParse(config["Serial:BaudRate"], out var br) ? br : 9600;

            // Wire up services
            var db          = new DatabaseService(connectionString);
            var serialReader = new ArduinoSerialReader(portName, baudRate, db);
            var telegramBot  = new TelegramBotService(telegramToken, db);

            using var cts = new CancellationTokenSource();

            // Start services
            serialReader.Start();
            await telegramBot.StartAsync(cts.Token);

            Console.WriteLine("System running. Press Escape to quit.");
            while (Console.ReadKey(true).Key != ConsoleKey.Escape) ;

            cts.Cancel();
            serialReader.Stop();

            Console.WriteLine("Shutdown complete.");
        }
    }
}
