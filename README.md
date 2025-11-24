# RarRenamer.NET

> **Modern .NET 8 WPF rewrite of [RarRenamer](https://github.com/L-at-nnes/RarRenamer) - 60x faster performance**

A high-performance WPF application built with .NET 8 to rename RAR archives based on their internal folder structure.

⚡ **This is the actively maintained version.** The original PowerShell version is now deprecated.

---

## 🚀 Why RarRenamer.NET?

- **60x Faster** - Scan 3000 files in ~30 seconds (vs 30 minutes in PowerShell)
- **Modern UI** - Dark theme WPF interface with responsive design
- **Single-Click Selection** - No double-clicking needed
- **Instant Updates** - Real-time prefix/suffix without rescanning
- **Native Performance** - Uses SharpCompress library, no external dependencies

---

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

---

## Requirements

- **Windows 10/11**
- **.NET 8 Runtime** ([Download](https://dotnet.microsoft.com/download/dotnet/8.0)) - Only for framework-dependent version
- **No external dependencies** - Uses SharpCompress library, no 7-Zip needed

---

## Installation

### Download Pre-built Binary (Recommended)

#### Self-Contained (No .NET Required)
1. Download `RarRenamer.NET-vX.X.X-self-contained.zip` from [Releases](https://github.com/L-at-nnes/RarRenamer.NET/releases)
2. Extract the ZIP file
3. Run `RarRenamer.exe`
4. **No installation needed!** ✨

#### Framework-Dependent (Smaller Size)
1. Download `RarRenamer.NET-vX.X.X-framework-dependent.zip` from [Releases](https://github.com/L-at-nnes/RarRenamer.NET/releases)
2. Install [.NET 8 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) if not already installed
3. Extract and run `RarRenamer.exe`

### Build from Source
```bash
git clone https://github.com/L-at-nnes/RarRenamer.NET.git
cd RarRenamer.NET/RarRenamer
dotnet build -c Release
dotnet run
```

---

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

---

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

---

## How It Works

1. **Scans** each RAR file using SharpCompress library
2. **Analyzes** the archive structure to find the first top-level folder
3. **Applies** optional prefix/suffix to the folder name
4. **Determines** if renaming is needed
5. **Renames** the RAR file to match the pattern

---

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

---

## Technologies

- **.NET 8** - Modern, high-performance framework
- **WPF** - Native Windows UI with hardware acceleration
- **SharpCompress 0.41.0** - Native RAR archive reading
- **Newtonsoft.Json 13.0.4** - Efficient JSON logging
- **Async/Await** - Non-blocking UI operations

---

## Performance Comparison

| Operation | PowerShell (Legacy) | RarRenamer.NET | Improvement |
|-----------|---------------------|----------------|-------------|
| Scan 3000 files | 30 minutes | ~30 seconds | **60x faster** |
| Undo 100 files | 4-5 minutes | <5 seconds | **50x faster** |
| Prefix/Suffix update | Full rescan | Instant | **∞x faster** |
| UI Responsiveness | Blocked | Always responsive | **100% better** |

---

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

---

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

---

## Migration from PowerShell Version

The original [RarRenamer](https://github.com/L-at-nnes/RarRenamer) PowerShell version is now deprecated in favor of RarRenamer.NET.

**What's changed:**
- ✅ 60x faster performance
- ✅ Modern WPF UI with dark theme
- ✅ No external dependencies (7-Zip not needed)
- ✅ Single-click selection
- ✅ Real-time prefix/suffix updates
- ✅ Asynchronous operations (responsive UI)

**Note:** The PowerShell version will receive no further updates. Please use RarRenamer.NET for the best experience.

---

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the project
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

---

## License

Free to use and modify.

---

## Acknowledgments

- Original PowerShell version: [RarRenamer](https://github.com/L-at-nnes/RarRenamer)
- SharpCompress library for native RAR reading
- .NET community for the excellent ecosystem

---

## Version History

- **v3.0** (2025-11-24): Complete rewrite in C# WPF .NET 8 with 60x performance improvement
- **v2.2** (2025-11-24): PowerShell - Automatic UI mode detection (deprecated)
- **v2.1** (2025-11-19): PowerShell - Windows 7 compatibility (deprecated)
- **v2.0** (2025-11-18): PowerShell - Added checkboxes, logging/rollback (deprecated)
- **v1.0**: PowerShell - Initial release (deprecated)

---

<p align="center">
  <strong>Made with ❤️ using C# and WPF</strong>
</p>

<p align="center">
  <a href="https://github.com/L-at-nnes/RarRenamer">📜 Legacy PowerShell Version</a>
</p>
