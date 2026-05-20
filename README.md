# Minimal_Telegram_chat_bot_temperature_sensor

A full-stack IoT monitoring system that reads temperature and humidity from a DHT22 sensor connected to an Arduino Uno, stores the data in a local SQLite database, and lets you query the latest reading from anywhere via a Telegram chat bot.

---

## What it does

1. An **Arduino Uno** reads temperature and humidity from a DHT22 sensor every 60 seconds and sends the data over USB serial.
2. A **C# application** running on Ubuntu listens on the serial port, parses the sensor data, and saves each reading to a SQLite database.
3. A **Telegram bot** integrated into the same application waits for any incoming message and replies with the most recent sensor reading pulled from the database.

---

## System requirements

- Ubuntu Linux (tested on Ubuntu 24.04)
- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- Arduino Uno connected via USB (`/dev/ttyACM0`)
- Arduino IDE to flash the sketch
- A Telegram bot token (create one via [@BotFather](https://t.me/BotFather))
- User must be in the `dialout` group to access the serial port:
  ```bash
  sudo usermod -aG dialout $USER
  # then log out and back in
  ```

---

## NuGet packages

| Package | Version | Purpose |
|---|---|---|
| `Microsoft.Data.Sqlite` | 9.0.8 | SQLite database access |
| `Microsoft.Extensions.Configuration` | 8.0.0 | Reading `appsettings.json` |
| `Microsoft.Extensions.Configuration.Json` | 8.0.0 | JSON config provider |
| `System.IO.Ports` | 8.0.0 | Serial port communication (Linux-compatible) |
| `Telegram.Bot` | 22.6.2 | Telegram Bot API client |

Install all packages by running:
```bash
dotnet restore
```

---

## Arduino library

The sketch uses **TinyDHT** — a lightweight integer-based DHT library. Install it via the Arduino IDE Library Manager:

`Tools → Manage Libraries → search "TinyDHT" → Install`

---

## Configuration

Before running, open `appsettings.json` and fill in your values:

```json
{
  "Telegram": {
    "BotToken": "YOUR_TELEGRAM_BOT_TOKEN_HERE"
  },
  "Database": {
    "ConnectionString": "Data Source=DHT22.db"
  },
  "Serial": {
    "PortName": "/dev/ttyACM0",
    "BaudRate": "9600"
  }
}
```

> ⚠️ **Never commit your real token to Git.** Keep the placeholder in the repository and store your real token only on the machine running the app.

---

## How to run

```bash
# Flash the Arduino sketch first via Arduino IDE,
# then start the C# application:

cd arduino_DHT22
dotnet run
```

Press `Escape` to shut down gracefully.

---

## Project structure

```
arduino_DHT22/
├── Program.cs                       # Entry point — wires services and starts the app
├── appsettings.json                 # Configuration (token, DB path, serial port)
├── arduino_DHT22.csproj             # Project file and NuGet references
│
├── Models/
│   └── SensorReading.cs            # Data model for a single humidity + temperature sample
│
└── Services/
    ├── ArduinoSerialReader.cs       # Opens the serial port, parses sensor output, triggers DB save
    ├── DatabaseService.cs           # SQLite: saves readings and retrieves the latest one
    └── TelegramBotService.cs        # Telegram bot: listens for messages, replies with latest reading

sketch_aug26a/
└── sketch_aug26a.ino               # Arduino sketch — reads DHT22 and sends data over Serial
```

### Class descriptions

**`Program`** — the entry point. Reads configuration from `appsettings.json`, instantiates all services, starts them, and waits for the user to press Escape before shutting down cleanly.

**`SensorReading`** — a simple model holding `Humidity`, `Temperature`, and `Timestamp`. Its `ToString()` method formats the message sent back to the Telegram user.

**`ArduinoSerialReader`** — opens the serial port to the Arduino, listens for incoming lines using the `DataReceived` event, parses humidity and temperature values with a regex, and calls `DatabaseService.SaveReading()`. Implements `IDisposable` for clean shutdown.

**`DatabaseService`** — handles all SQLite interactions. `SaveReading()` inserts a new row with the current timestamp. `GetLatestReading()` queries the most recent row ordered by `rowid` and returns a `SensorReading` object.

**`TelegramBotService`** — initialises the Telegram bot client, registers event handlers for incoming messages, and responds to every message with the latest reading from `DatabaseService`. Unrecognised message types are silently ignored.

---

## How it works — two independent workflows

The application runs two completely separate workflows simultaneously. They share the database but are otherwise independent: one writes, one reads.

---

### Workflow 1 — sensor reading pipeline

Runs continuously in the background, triggered by the Arduino every 60 seconds.

```
  ┌──────────┐   I²C    ┌──────────────┐   USB Serial (/dev/ttyACM0)   ┌──────────────────────┐   INSERT   ┌───────────┐
  │  DHT22   │ ───────► │ Arduino Uno  │ ────────────────────────────► │ ArduinoSerialReader  │ ─────────► │  SQLite   │
  │  sensor  │          │ reads every  │                               │ parse humidity &     │           │  DHT22.db │
  └──────────┘          │ 60 seconds   │                               │ temperature          │           └───────────┘
                        └──────────────┘                               └──────────────────────┘

  Raw serial line:   "Humidity: 58 %    Temperature: 24 *C"
  Parsed values:     humidity = 58, temperature = 24
  Saved as:          INSERT INTO Data (humidity, temperature) VALUES (58, 24)
```

---

### Workflow 2 — Telegram query & reply

Triggered on demand, any time a user sends a message to the bot.

```
                             ┌──────────────────────┐   SELECT latest row   ┌──────────────┐
  User sends                 │  TelegramBotService  │ ────────────────────► │  SQLite      │
  any message                │  OnMessage() fires   │                       │  DHT22.db    │
      │                      │  calls               │ ◄──────────────────── │              │
      │    HTTPS              │  ReplyWithLatestReading()  SensorReading     └──────────────┘
      ▼                      └──────────────────────┘
  ┌──────────┐   update   ┌──────────────────────┐
  │ Telegram │ ─────────► │  TelegramBotService  │
  │ Bot API  │            └──────────────────────┘
  │          │ ◄───────────────────────────────────
  └──────────┘   SendMessage()

  Reply sent to user:
  "🌡 Temperature: 24 °C
   💧 Humidity: 58%
   🕐 2025-11-10 19:22:00"
```

---

## Database schema

The application expects a `Data` table in `DHT22.db`:

```sql
CREATE TABLE Data (
    rowid       INTEGER PRIMARY KEY AUTOINCREMENT,
    Humidity    INTEGER NOT NULL,
    Temperature INTEGER NOT NULL,
    Timestamp   DATETIME DEFAULT CURRENT_TIMESTAMP
);
```

---

## Wiring

```
DHT22 Pin 1 (VCC)  → Arduino 5V
DHT22 Pin 2 (DATA) → Arduino Digital Pin 2  (+ 10KΩ pull-up resistor to 5V)
DHT22 Pin 4 (GND)  → Arduino GND
Arduino             → Host PC via USB
```

---

## License

See [LICENSE](LICENSE) for details.
