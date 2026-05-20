using System.IO.Ports;
using System.Text.RegularExpressions;

namespace arduino_DHT22.Services
{
    public class ArduinoSerialReader : IDisposable
    {
        private readonly SerialPort _port;
        private readonly DatabaseService _db;

        // Matches the Arduino serial output format:
        // "Humidity: XX %\tTemperature: YY *C"
        private static readonly Regex _dataRegex = new(@"\d+", RegexOptions.Compiled);

        public ArduinoSerialReader(string portName, int baudRate, DatabaseService db)
        {
            _db = db;
            _port = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
            _port.DataReceived += OnDataReceived;
        }

        public void Start()
        {
            _port.Open();
            Console.WriteLine($"[Serial] Listening on {_port.PortName} at {_port.BaudRate} baud.");
        }

        public void Stop()
        {
            if (_port.IsOpen)
            {
                _port.DataReceived -= OnDataReceived;
                _port.Close();
                Console.WriteLine("[Serial] Port closed.");
            }
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                var line = _port.ReadLine().Trim();
                Console.WriteLine($"[Serial] Raw: {line}");

                var matches = _dataRegex.Matches(line);

                if (matches.Count >= 2 &&
                    int.TryParse(matches[0].Value, out int humidity) &&
                    int.TryParse(matches[1].Value, out int temperature))
                {
                    Console.WriteLine($"[Serial] Parsed → Humidity: {humidity}%, Temperature: {temperature}°C");
                    _db.SaveReading(humidity, temperature);
                }
                else
                {
                    Console.WriteLine($"[Serial] Could not parse sensor data from line: '{line}'");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Serial] Error reading port: {ex.Message}");
            }
        }

        public void Dispose() => Stop();
    }
}
