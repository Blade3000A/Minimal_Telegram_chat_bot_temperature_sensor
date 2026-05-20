// DHT22 Temperature & Humidity Sensor — Arduino Uno
// Reads sensor every 60s and sends data over Serial to the host C# app.
// Library: TinyDHT (integer-based, lightweight)

#include "TinyDHT.h"

#define DHTPIN    2       // DHT22 data pin → Arduino Digital Pin 2
#define DHTTYPE   DHT22   // Sensor type (DHT11 / DHT22 / DHT21)
#define BAUD_RATE 9600
#define READ_INTERVAL_MS 60000  // 60 seconds between readings
#define STARTUP_DELAY_MS  2000  // DHT22 needs ~2s to stabilise after power-on

// Wiring:
//   Pin 1 (VCC)  → +5V
//   Pin 2 (DATA) → DHTPIN (+ 10K pull-up resistor to VCC)
//   Pin 4 (GND)  → GND

DHT dht(DHTPIN, DHTTYPE);

void setup() {
  Serial.begin(BAUD_RATE);
  delay(STARTUP_DELAY_MS);  // Let sensor stabilise
  Serial.println("DHT22 ready.");
  dht.begin();
}

void loop() {
  int8_t  h = dht.readHumidity();
  int16_t t = dht.readTemperature(0);  // 0 = Celsius

  if (t == BAD_TEMP || h == BAD_HUM) {
    Serial.println("ERR: Failed to read from DHT22");
  } else {
    // Format parsed by C# ArduinoSerialReader (do not change structure)
    Serial.print("Humidity: ");
    Serial.print(h);
    Serial.print(" %\t");
    Serial.print("Temperature: ");
    Serial.print(t);
    Serial.println(" *C");
  }

  delay(READ_INTERVAL_MS);
}
