# OTP App Seed Importer

A cross-platform desktop application built with Avalonia UI and .NET 8 for parsing and importing OTP token seed data into SQLite databases.

## Features

- **Cross-Platform**: Runs on Windows, macOS, and Linux
- **Modern UI**: Built with Avalonia UI using the MVVM pattern
- **File Parsing**: Validates and parses seed files with detailed error reporting
- **Database Integration**: Imports data into SQLite databases with parameterized queries
- **Data Preview**: View parsed entries before importing
- **Error Handling**: Comprehensive error messages and validation
- **Auto-Spec Creation**: Automatically creates default token specifications if none exist

## Prerequisites

- .NET 8.0 SDK or later
- SQLite database with `ft_tokeninfo` and `ft_tokenspec` tables

## Getting Started

### 1. Build and Run

```bash
# Clone or download the project
cd OTPAppSeedImporter

# Restore dependencies
dotnet restore

# Build the application
dotnet build

# Run the application
dotnet run
```

### 2. Create Test Database (Optional)

If you want to quickly test the application, you can create a test database:

```bash
# Create a test database with the required schema
sqlite3 test_database.db < create_test_db.sql
```

This will create a `test_database.db` file with the proper tables and a sample token specification.

### 3. Seed File Format

Create a text file where each line contains a token and its associated public key, separated by a comma:

```
862503025416,AE7AB67157F90B1935B8E7979BAE30FB8C6EA5E5
123456789012,1234567890ABCDEF1234567890ABCDEF12345678
987654321098,FEDCBA0987654321FEDCBA0987654321FEDCBA09
```

**Format Requirements:**
- Token: 12, 13, or 16 digits
- Public Key: Exactly 40 hexadecimal characters (A-F, a-f, 0-9)
- Separated by a comma
- One entry per line

### 4. Database Requirements

Your SQLite database must contain these tables:

#### `ft_tokeninfo` table:
```sql
CREATE TABLE "ft_tokeninfo" (
    "tknid" INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    "token" varchar(32) NOT NULL,
    "pubkey" varchar(1024) NOT NULL DEFAULT '',
    "authnum" varchar(32) DEFAULT '0',
    "physicaltype" INTEGER DEFAULT 0,
    "producttype" INTEGER DEFAULT 0,
    "specid" varchar(32) NOT NULL,
    "importtime" INTEGER DEFAULT 0,
    "pubkeystate" INTEGER DEFAULT 0,
    "pubkeyfactor" varchar(128),
    "tknifmid" INTEGER NOT NULL,
    "tknofmid" INTEGER NOT NULL,
    "crust" varchar(32),
    "tknexttype" varchar(128),
    "tkntype" INTEGER NOT NULL,
    "tknstate" INTEGER,
    "tksendnumber" varchar(64),
    "tknvaliddate" INTEGER,
    CONSTRAINT "fk_tkninfo_tknspec_specid" FOREIGN KEY ("specid") REFERENCES "ft_tokenspec" ("specid") ON DELETE CASCADE ON UPDATE CASCADE,
    UNIQUE ("token" ASC)
);
```

#### `ft_tokenspec` table:
```sql
CREATE TABLE ft_tokenspec (
    specid INTEGER PRIMARY KEY AUTOINCREMENT, 
    name TEXT NOT NULL,                       
    sn_length INTEGER NOT NULL,              
    token_interval INTEGER NOT NULL,         
    checksum TEXT NOT NULL,      
    algorithm TEXT NOT NULL                
);
```

**Note**: If no token specifications exist in `ft_tokenspec`, the application will automatically create a default specification with sensible values.

### 5. Using the Application

1. **Select Seed File**: Click "Browse..." next to "Seed File" and select your .txt file
2. **Select Database**: Click "Browse..." next to "Database" and select your SQLite database
3. **Preview Data**: Valid entries will appear in the data grid
4. **Import**: Click "Import to Database" to insert the entries

The application will:
- Validate that both required tables exist
- Use existing token specifications or create a default one
- Insert records with proper foreign key relationships
- Handle duplicate tokens gracefully
- Provide detailed error reporting

## Project Structure

```
/Models
  └── SeedEntry.cs              # Data model for seed entries
/Services
  ├── SeedParser.cs             # File parsing and validation logic
  └── DatabaseManager.cs        # SQLite database operations
/ViewModels
  └── MainWindowViewModel.cs    # MVVM view model with data binding
/Views
  └── MainWindow.axaml         # Main UI layout
/App.axaml                     # Application configuration
/Program.cs                    # Application entry point
```

## Architecture

The application follows the **MVVM (Model-View-ViewModel)** pattern:

- **Models**: Define data structures (`SeedEntry`)
- **Services**: Handle business logic (parsing, database operations)
- **ViewModels**: Manage UI state and commands with data binding
- **Views**: XAML-based user interface

## Dependencies

- **Avalonia UI 11.2.1**: Cross-platform UI framework
- **Microsoft.Data.Sqlite 8.0.0**: SQLite database connectivity
- **CommunityToolkit.Mvvm 8.2.0**: MVVM helpers and source generators

## Database Field Mapping

When importing, the application sets these values:

| Database Field | Value | Description |
|----------------|-------|-------------|
| `token` | From file | The token serial number |
| `pubkey` | From file | The 40-character hex public key |
| `authnum` | "0" | Default authentication number |
| `physicaltype` | 0 | Default physical type |
| `producttype` | 0 | Default product type |
| `specid` | Auto-selected | References existing or newly created spec |
| `importtime` | Current timestamp | Unix timestamp of import |
| `pubkeystate` | 0 | Default public key state |
| `tknifmid` | 1 | Default token interface ID |
| `tknofmid` | 1 | Default token output format ID |
| `tkntype` | 1 | Default token type |
| `tknstate` | 1 | Default token state |

## Error Handling

The application provides comprehensive error handling for:
- Invalid file formats
- Missing or inaccessible files
- Database connection issues
- Missing required tables
- Invalid token/public key formats
- Duplicate token entries
- Foreign key constraint violations
- SQL insertion errors

All errors are displayed in the status bar with detailed messages.

## Sample Files

- `sample_seeds.txt`: Example seed file for testing
- `create_test_db.sql`: SQL script to create a test database
- Use these files to test the application functionality

## Building for Distribution

### Windows
```bash
dotnet publish -c Release -r win-x64 --self-contained
```

### macOS
```bash
dotnet publish -c Release -r osx-x64 --self-contained
```

### Linux
```bash
dotnet publish -c Release -r linux-x64 --self-contained
```

The `--self-contained` flag creates a standalone executable that doesn't require .NET to be installed on the target machine.

## License

This project is provided as-is for educational and development purposes.
