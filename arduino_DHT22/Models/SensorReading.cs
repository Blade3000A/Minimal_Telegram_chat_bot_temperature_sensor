namespace arduino_DHT22.Models
{
    public class SensorReading
    {
        public int Humidity { get; set; }
        public int Temperature { get; set; }
        public DateTime Timestamp { get; set; }

        public override string ToString()
            => $"🌡 Temperature: {Temperature} °C\n💧 Humidity: {Humidity}%\n🕐 {Timestamp:yyyy-MM-dd HH:mm:ss}";
    }
}
