using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ECommerce.Infrastructure.Utilities
{
    [SupportedOSPlatform("windows")]
    public static class JsonStorageHelper
    {
        public static async Task<Dictionary<string, object>> GetJsonDataAsync(OleDbConnection connection, string tableName, string jsonColumn, string keyColumn, object keyValue)
        {
            var command = new OleDbCommand($"SELECT {jsonColumn} FROM {tableName} WHERE {keyColumn} = @KeyValue", connection);
            command.Parameters.AddWithValue("@KeyValue", keyValue);
            
            var result = await command.ExecuteScalarAsync();
            
            if (result != null && result != DBNull.Value)
            {
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(result.ToString());
            }
            
            return new Dictionary<string, object>();
        }
        
        public static async Task<bool> SaveJsonDataAsync(OleDbConnection connection, string tableName, string jsonColumn, string keyColumn, object keyValue, Dictionary<string, object> data)
        {
            // Check if record exists
            var checkCommand = new OleDbCommand($"SELECT COUNT(*) FROM {tableName} WHERE {keyColumn} = @KeyValue", connection);
            checkCommand.Parameters.AddWithValue("@KeyValue", keyValue);
            
            int count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
            
            string json = JsonConvert.SerializeObject(data);
            
            if (count > 0)
            {
                // Update existing record
                var updateCommand = new OleDbCommand($"UPDATE {tableName} SET {jsonColumn} = @Json WHERE {keyColumn} = @KeyValue", connection);
                updateCommand.Parameters.AddWithValue("@Json", json);
                updateCommand.Parameters.AddWithValue("@KeyValue", keyValue);
                
                int rowsAffected = await updateCommand.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            else
            {
                // Insert new record
                var insertCommand = new OleDbCommand($"INSERT INTO {tableName} ({keyColumn}, {jsonColumn}) VALUES (@KeyValue, @Json)", connection);
                insertCommand.Parameters.AddWithValue("@KeyValue", keyValue);
                insertCommand.Parameters.AddWithValue("@Json", json);
                
                int rowsAffected = await insertCommand.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
        }

        public static Dictionary<string, object> LoadJsonData(OleDbCommand command)
        {
            var result = command.ExecuteScalar();
            
            if (result != null && result != DBNull.Value)
            {
                var jsonStr = result.ToString();
                if (!string.IsNullOrEmpty(jsonStr))
                {
                    return JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonStr) 
                        ?? new Dictionary<string, object>();
                }
            }
            
            return new Dictionary<string, object>();
        }

        public static void SaveJsonData(Dictionary<string, object> data, OleDbCommand checkCommand, OleDbCommand updateCommand, OleDbCommand insertCommand)
        {
            string json = JsonConvert.SerializeObject(data);
            int count = 0;

            var result = checkCommand.ExecuteScalar();
            if (result != null && result != DBNull.Value)
            {
                count = System.Convert.ToInt32(result);
            }

            if (count > 0)
            {
                // Update existing record
                updateCommand.Parameters.AddWithValue("@Json", json);
                updateCommand.ExecuteNonQuery();
            }
            else
            {
                // Insert new record
                insertCommand.Parameters.AddWithValue("@Json", json);
                insertCommand.ExecuteNonQuery();
            }
        }
    }
}