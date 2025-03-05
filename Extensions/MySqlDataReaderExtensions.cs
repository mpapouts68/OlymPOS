using System;
using MySqlConnector;

namespace OlymPOS.Extensions
{
    public static class MySqlDataReaderExtensions
    {
        public static int GetInt32OrDefault(this MySqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? default : reader.GetInt32(ordinal);
        }

        public static decimal GetDecimalOrDefault(this MySqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? default : reader.GetDecimal(ordinal);
        }

        public static string GetStringOrDefault(this MySqlDataReader reader, string columnName, string defaultValue = "")
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? defaultValue : reader.GetString(ordinal);
        }

        public static bool GetBooleanOrDefault(this MySqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return !reader.IsDBNull(ordinal) && reader.GetBoolean(ordinal);
        }
    }
}
