using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using OTPAppSeedImporter.Models;

namespace OTPAppSeedImporter.Services;

public class DatabaseManager
{
    public async Task<(int SuccessCount, List<string> Errors)> InsertSeedEntriesAsync(string databasePath, List<SeedEntry> seedEntries)
    {
        var errors = new List<string>();
        var successCount = 0;

        try
        {
            if (!File.Exists(databasePath))
            {
                errors.Add($"Database file not found: {databasePath}");
                return (successCount, errors);
            }

            using var connection = new SqliteConnection($"Data Source={databasePath}");
            await connection.OpenAsync();

            // Check if the required tables exist
            var tableExistsCommand = connection.CreateCommand();
            tableExistsCommand.CommandText = @"
                SELECT COUNT(*) FROM sqlite_master 
                WHERE type='table' AND name IN ('ft_tokeninfo', 'ft_tokenspec')";
            
            var tableCount = Convert.ToInt32(await tableExistsCommand.ExecuteScalarAsync());
            
            if (tableCount < 2)
            {
                errors.Add("Required tables 'ft_tokeninfo' and/or 'ft_tokenspec' do not exist in the database.");
                return (successCount, errors);
            }

            // Get a valid specid from ft_tokenspec table, or create a default one
            var specId = await GetOrCreateDefaultSpecAsync(connection);
            if (specId == null)
            {
                errors.Add("Could not find or create a valid specid in ft_tokenspec table.");
                return (successCount, errors);
            }

            // Prepare the insert command with all required fields
            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO ft_tokeninfo (
                    token, pubkey, authnum, physicaltype, producttype, specid, 
                    importtime, pubkeystate, tknifmid, tknofmid, tkntype, tknstate
                ) VALUES (
                    @token, @pubkey, @authnum, @physicaltype, @producttype, @specid,
                    @importtime, @pubkeystate, @tknifmid, @tknofmid, @tkntype, @tknstate
                )";

            var tokenParam = command.Parameters.Add("@token", SqliteType.Text);
            var pubkeyParam = command.Parameters.Add("@pubkey", SqliteType.Text);
            var authnumParam = command.Parameters.Add("@authnum", SqliteType.Text);
            var physicaltypeParam = command.Parameters.Add("@physicaltype", SqliteType.Integer);
            var producttypeParam = command.Parameters.Add("@producttype", SqliteType.Integer);
            var specidParam = command.Parameters.Add("@specid", SqliteType.Integer);
            var importtimeParam = command.Parameters.Add("@importtime", SqliteType.Integer);
            var pubkeystateParam = command.Parameters.Add("@pubkeystate", SqliteType.Integer);
            var tknifmidParam = command.Parameters.Add("@tknifmid", SqliteType.Integer);
            var tknofmidParam = command.Parameters.Add("@tknofmid", SqliteType.Integer);
            var tkntypeParam = command.Parameters.Add("@tkntype", SqliteType.Integer);
            var tknstateParam = command.Parameters.Add("@tknstate", SqliteType.Integer);

            // Set default values for all entries
            authnumParam.Value = "0";
            physicaltypeParam.Value = 0;
            producttypeParam.Value = 0;
            specidParam.Value = specId;
            importtimeParam.Value = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            pubkeystateParam.Value = 0;
            tknifmidParam.Value = 1;
            tknofmidParam.Value = 1;
            tkntypeParam.Value = 1;
            tknstateParam.Value = 1;

            foreach (var entry in seedEntries)
            {
                try
                {
                    tokenParam.Value = entry.Token;
                    pubkeyParam.Value = entry.Pubkey;

                    await command.ExecuteNonQueryAsync();
                    successCount++;
                }
                catch (SqliteException ex) when (ex.SqliteErrorCode == 19) // SQLITE_CONSTRAINT
                {
                    errors.Add($"Token {entry.Token} already exists in database or violates constraints.");
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to insert token {entry.Token}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Database error: {ex.Message}");
        }

        return (successCount, errors);
    }

    private async Task<int?> GetOrCreateDefaultSpecAsync(SqliteConnection connection)
    {
        try
        {
            // First, try to get an existing specid
            var selectCommand = connection.CreateCommand();
            selectCommand.CommandText = "SELECT specid FROM ft_tokenspec LIMIT 1";
            
            var result = await selectCommand.ExecuteScalarAsync();
            if (result != null)
            {
                return Convert.ToInt32(result);
            }

            // If no spec exists, create a default one
            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = @"
                INSERT INTO ft_tokenspec (name, sn_length, token_interval, checksum, algorithm)
                VALUES ('Default Import Spec', 12, 30, 'sha256', 'totp')";
            
            await insertCommand.ExecuteNonQueryAsync();

            // Get the newly created specid
            var getIdCommand = connection.CreateCommand();
            getIdCommand.CommandText = "SELECT last_insert_rowid()";
            
            return Convert.ToInt32(await getIdCommand.ExecuteScalarAsync());
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<bool> TestConnectionAsync(string databasePath)
    {
        try
        {
            if (!File.Exists(databasePath))
                return false;

            using var connection = new SqliteConnection($"Data Source={databasePath}");
            await connection.OpenAsync();
            
            // Test that required tables exist
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT COUNT(*) FROM sqlite_master 
                WHERE type='table' AND name IN ('ft_tokeninfo', 'ft_tokenspec')";
            
            var tableCount = Convert.ToInt32(await command.ExecuteScalarAsync());
            return tableCount >= 2;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<string>> GetAvailableSpecsAsync(string databasePath)
    {
        var specs = new List<string>();
        
        try
        {
            if (!File.Exists(databasePath))
                return specs;

            using var connection = new SqliteConnection($"Data Source={databasePath}");
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT specid, name FROM ft_tokenspec ORDER BY name";
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var specId = reader.GetInt32(0); // specid is first column
                var name = reader.GetString(1);  // name is second column
                specs.Add($"{specId}: {name}");
            }
        }
        catch
        {
            // Ignore errors and return empty list
        }

        return specs;
    }
} 