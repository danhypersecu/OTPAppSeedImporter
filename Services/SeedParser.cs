using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using OTPAppSeedImporter.Models;

namespace OTPAppSeedImporter.Services;

public class SeedParser
{
    private static readonly Regex TokenRegex = new(@"^(\d{12}|\d{13}|\d{16})$");
    private static readonly Regex PubkeyRegex = new(@"^[A-Fa-f0-9]{40}$");

    public async Task<(List<SeedEntry> ValidEntries, List<string> Errors)> ParseFileAsync(string filePath)
    {
        var validEntries = new List<SeedEntry>();
        var errors = new List<string>();

        try
        {
            if (!File.Exists(filePath))
            {
                errors.Add($"File not found: {filePath}");
                return (validEntries, errors);
            }

            var lines = await File.ReadAllLinesAsync(filePath);
            
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                var lineNumber = i + 1;

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = line.Split(',');
                if (parts.Length != 2)
                {
                    errors.Add($"Line {lineNumber}: Invalid format. Expected 'token,pubkey'");
                    continue;
                }

                var token = parts[0].Trim();
                var pubkey = parts[1].Trim();

                if (!TokenRegex.IsMatch(token))
                {
                    errors.Add($"Line {lineNumber}: Invalid token format '{token}'. Must be 12, 13, or 16 digits.");
                    continue;
                }

                if (!PubkeyRegex.IsMatch(pubkey))
                {
                    errors.Add($"Line {lineNumber}: Invalid pubkey format '{pubkey}'. Must be 40 hex characters.");
                    continue;
                }

                validEntries.Add(new SeedEntry { Token = token, Pubkey = pubkey });
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Error reading file: {ex.Message}");
        }

        return (validEntries, errors);
    }
} 