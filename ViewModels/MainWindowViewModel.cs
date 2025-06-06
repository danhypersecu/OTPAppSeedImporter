using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OTPAppSeedImporter.Models;
using OTPAppSeedImporter.Services;

namespace OTPAppSeedImporter.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly SeedParser _seedParser;
    private readonly DatabaseManager _databaseManager;

    [ObservableProperty]
    private string _seedFilePath = string.Empty;

    [ObservableProperty]
    private string _databasePath = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private ObservableCollection<SeedEntry> _parsedSeeds = new();

    public MainWindowViewModel()
    {
        _seedParser = new SeedParser();
        _databaseManager = new DatabaseManager();
    }

    [RelayCommand]
    private async Task BrowseSeedFile()
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow : null);

            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select Seed File",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Text Files")
                    {
                        Patterns = new[] { "*.txt" }
                    },
                    new FilePickerFileType("All Files")
                    {
                        Patterns = new[] { "*.*" }
                    }
                }
            });

            if (files.Count > 0)
            {
                SeedFilePath = files[0].Path.LocalPath;
                StatusMessage = $"Seed file selected: {Path.GetFileName(SeedFilePath)}";
                
                // Auto-parse the file when selected
                await ParseSeedFile();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error selecting file: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task BrowseDatabase()
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow : null);

            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select SQLite Database",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("SQLite Database")
                    {
                        Patterns = new[] { "*.db", "*.sqlite", "*.sqlite3" }
                    },
                    new FilePickerFileType("All Files")
                    {
                        Patterns = new[] { "*.*" }
                    }
                }
            });

            if (files.Count > 0)
            {
                DatabasePath = files[0].Path.LocalPath;
                StatusMessage = $"Database selected: {Path.GetFileName(DatabasePath)}";
                
                // Test database connection
                if (await _databaseManager.TestConnectionAsync(DatabasePath))
                {
                    StatusMessage += " - Connection successful!";
                }
                else
                {
                    StatusMessage += " - Warning: Cannot connect to database!";
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error selecting database: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task Import()
    {
        try
        {
            if (string.IsNullOrEmpty(SeedFilePath))
            {
                StatusMessage = "Please select a seed file first.";
                return;
            }

            if (string.IsNullOrEmpty(DatabasePath))
            {
                StatusMessage = "Please select a database file first.";
                return;
            }

            if (!ParsedSeeds.Any())
            {
                StatusMessage = "No valid entries to import. Please check your seed file.";
                return;
            }

            StatusMessage = "Importing entries...";

            var (successCount, errors) = await _databaseManager.InsertSeedEntriesAsync(DatabasePath, ParsedSeeds.ToList());

            if (errors.Any())
            {
                StatusMessage = $"Import completed with errors. {successCount} entries inserted. Errors: {string.Join("; ", errors.Take(3))}";
                if (errors.Count > 3)
                {
                    StatusMessage += $" and {errors.Count - 3} more...";
                }
            }
            else
            {
                StatusMessage = $"Import successful! {successCount} entries inserted into database.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Import failed: {ex.Message}";
        }
    }

    private async Task ParseSeedFile()
    {
        try
        {
            if (string.IsNullOrEmpty(SeedFilePath))
                return;

            StatusMessage = "Parsing seed file...";
            ParsedSeeds.Clear();

            var (validEntries, errors) = await _seedParser.ParseFileAsync(SeedFilePath);

            foreach (var entry in validEntries)
            {
                ParsedSeeds.Add(entry);
            }

            if (errors.Any())
            {
                StatusMessage = $"Parsed {validEntries.Count} valid entries with {errors.Count} errors. First error: {errors.First()}";
            }
            else
            {
                StatusMessage = $"Successfully parsed {validEntries.Count} entries.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error parsing file: {ex.Message}";
        }
    }
} 