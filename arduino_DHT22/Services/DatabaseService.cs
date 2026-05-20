using Microsoft.Data.Sqlite;
using arduino_DHT22.Models;

namespace arduino_DHT22.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void SaveReading(int humidity, int temperature)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO Data (humidity, temperature)
                    VALUES ($humidity, $temperature)
                ";
                command.Parameters.AddWithValue("$humidity", humidity);
                command.Parameters.AddWithValue("$temperature", temperature);
                command.ExecuteNonQuery();

                command.CommandText = "SELECT last_insert_rowid()";
                var newId = (long)command.ExecuteScalar()!;
                Console.WriteLine($"[DB] Saved reading with ID {newId}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB] Error saving reading: {ex.Message}");
            }
        }

        public SensorReading? GetLatestReading()
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();

                string query = @"
                    SELECT Humidity, Temperature, Timestamp
                    FROM Data
                    ORDER BY rowid DESC
                    LIMIT 1
                ";

                using var command = new SqliteCommand(query, connection);
                using var reader = command.ExecuteReader();

                if (reader.Read())
                {
                    return new SensorReading
                    {
                        Humidity    = reader.GetInt32(reader.GetOrdinal("Humidity")),
                        Temperature = reader.GetInt32(reader.GetOrdinal("Temperature")),
                        Timestamp   = reader.GetDateTime(reader.GetOrdinal("Timestamp"))
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB] Error reading data: {ex.Message}");
            }

            return null;
        }
    }
}
