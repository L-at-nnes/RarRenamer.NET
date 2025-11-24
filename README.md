# RAR Renamer

A WPF application built with .NET 8 to rename RAR archives based on their internal folder structure, with advanced features for selection, logging, and custom prefixes/suffixes.

**⚡ 60x faster than the PowerShell version!**

## Features

### 🚀 Performance
- **Ultra-fast scanning**: Scan 3000 files in ~30 seconds (vs 30 minutes in PowerShell)
- **Instant undo**: Restore operations in <5 seconds (vs 4-5 minutes)
- **Real-time prefix/suffix updates**: No rescanning needed
- **Asynchronous operations**: UI stays responsive during all operations

### ✅ File Management
- Individual checkbox for each file in the grid
- Select All / Deselect All buttons for quick selection
- Single-click on row to toggle selection
- Precise control over which files to rename

### 📝 Logging & Rollback System
- **Automatic Logging**: All rename operations logged to `rename_log.json`
- **Detailed Log Entries**: Timestamp, old/new paths, success status, error messages
- **Selective Undo**: Choose which operations to undo with checkboxes
- **Persistent Log**: Works across application restarts

### 🎨 Prefix & Suffix System
- **Prefix Input**: Add text before the folder name
- **Suffix Input**: Add text after the folder name
- **Direct Concatenation**: Text added exactly as typed (no automatic dashes or spaces)
- **Instant Updates**: Real-time preview without rescanning

## Requirements

- **Windows 10/11** (Windows 7 not supported in this version)
- **.NET 8 Runtime** ([Download](https://dotnet.microsoft.com/download/dotnet/8.0))
- **No external dependencies** (uses SharpCompress library, no 7-Zip needed)

## Installation

### Download Pre-built Binary
1. Download the latest release
2. Extract the ZIP file
3. Run `RarRenamer.exe`

### Build from Source
```bash
git clone <repository-url>
cd RarRenamer
dotnet build -c Release
dotnet run
```

## Usage

1. **Select folder**: Click "Browse" to choose a folder containing RAR files
2. **Scan archives**: Click "Scan Archives"
3. **Configure prefix/suffix** (optional):
   - Enter prefix (e.g., "P-" or "MyApp ")
   - Enter suffix (e.g., "-v2" or " Portable")
   - Text is concatenated exactly as typed
4. **Select files**: 
   - Click on rows to toggle selection
   - Use "Select All" or "Deselect All" buttons
5. **Rename**: Click "Rename Selected"
6. **Undo if needed**: Click "Undo Operations" to revert changes

## Examples

**Without prefix/suffix:**
```
Archive: test_v1.2.3.rar
Contains folder: "MyApp"
Result: MyApp.rar
```

**With suffix " Portable" (with leading space):**
```
Archive: test_v1.2.3.rar
Contains folder: "MyApp"
Suffix: " Portable"
Result: MyApp Portable.rar
```

**With prefix "P-" and suffix "-x64":**
```
Archive: test_v1.2.3.rar
Contains folder: "MyApp"
Prefix: "P-"
Suffix: "-x64"
Result: P-MyApp-x64.rar
```

## How It Works

1. **Scans** each RAR file using SharpCompress library
2. **Analyzes** the archive structure to find the first top-level folder
3. **Applies** optional prefix/suffix to the folder name
4. **Determines** if renaming is needed
5. **Renames** the RAR file to match the pattern

## Log File Format

The `rename_log.json` file stores all operations:

```json
[
  {
    "Timestamp": "2025-11-24T14:30:45",
    "OldPath": "D:\\Archives\\app123.rar",
    "NewPath": "D:\\Archives\\MyApp-Portable.rar",
    "OldName": "app123.rar",
    "NewName": "MyApp-Portable.rar",
    "Success": true
  }
]
```

## Technologies

- **.NET 8** - Modern framework
- **WPF** - Native Windows UI
- **SharpCompress 0.41.0** - RAR archive reading
- **Newtonsoft.Json 13.0.4** - JSON logging

## Building & Publishing

### Build Debug
```bash
dotnet build
```

### Build Release
```bash
dotnet build -c Release
```

### Publish Self-Contained (~156 MB, no dependencies)
```bash
dotnet publish -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o publish/self-contained
```

### Publish Framework-Dependent (~2 MB, requires .NET 8)
```bash
dotnet publish -c Release -r win-x64 --self-contained false \
  -p:PublishSingleFile=true \
  -o publish/framework-dependent
```

## Troubleshooting

### Common Issues

**"No RAR files found"**
- Ensure the selected folder contains .rar files
- Check file permissions

**"Password protected" or "Corrupted archive"**
- Archives are detected but cannot be scanned
- These files will be marked and skipped

**Undo doesn't work**
- Check if `rename_log.json` exists in the application folder
- Verify files haven't been manually moved/renamed
- Ensure the log file has valid JSON format

## Performance Comparison

| Operation | PowerShell | C# WPF | Improvement |
|-----------|-----------|---------|-------------|
| Scan 3000 files | 30 minutes | ~30 seconds | **60x faster** |
| Undo 100 files | 4-5 minutes | <5 seconds | **50x faster** |
| Prefix/Suffix update | Full rescan | Instant | **∞x faster** |

## License

Free to use and modify.

## Version History

- **v3.0** (2025-11-24): Complete rewrite in C# WPF .NET 8 with 60x performance improvement
- **v2.2** (2025-11-24): PowerShell - Automatic UI mode detection
- **v2.1** (2025-11-19): PowerShell - Windows 7 compatibility
- **v2.0** (2025-11-18): PowerShell - Added checkboxes, logging/rollback
- **v1.0**: PowerShell - Initial release
