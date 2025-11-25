# RarRenamer.NET

> **Modern .NET 8 WPF rewrite of [RarRenamer](https://github.com/L-at-nnes/RarRenamer) - 60x faster performance**

A high-performance WPF application built with .NET 8 to rename RAR archives based on their internal folder structure.

⚡ **This is the actively maintained version.** The original PowerShell version is now deprecated.

---

## 🚀 Why RarRenamer.NET?

- **60x Faster** - Scan 3000 files in ~1 minute (vs 30 minutes in PowerShell)
- **Modern UI** - Dark theme WPF interface with responsive design
- **Single-Click Selection** - No double-clicking needed
- **Instant Updates** - Real-time prefix/suffix without rescanning
- **Native Performance** - Uses 7-Zip CLI for ultra-fast scanning
- **Cancellable Operations** - Stop scans anytime with Cancel button
- **Smart Drive Detection** - Automatically optimizes for SSD/HDD

---

## ⚡ Performance

### Scan Speed (2300-3000 RAR files, 500 GB total)

| Storage Type | Scan Time | Improvement vs v3.0 |
|--------------|-----------|---------------------|
| **NVMe SSD** | 30-60 seconds | 10x faster |
| **SATA SSD** | 1-2 minutes | 8x faster |
| **HDD 7200 RPM** | 3-8 minutes | 12x faster |
| **HDD 5400 RPM** | 5-12 minutes | **20x faster** |

**Note:** v3.1.0 uses 7-Zip CLI instead of SharpCompress library, resulting in dramatic performance improvements.

### Undo & Prefix/Suffix Testing

| Operation | Time | Notes |
|-----------|------|-------|
| Undo 100 files | < 1 second | No rescan needed |
| Change prefix/suffix after scan | **Instant** | List refreshes instantly |
| Test different suffix | **0 seconds** | No need to rescan! |

**Major UX improvement:** After undo or rename operations, you can test different prefix/suffix combinations instantly without rescanning!

---

## Features

### 🚀 Performance
- **Ultra-fast scanning**: Uses 7-Zip CLI for 10-20x speed improvement
- **Instant undo**: Restore operations in <1 second
- **Real-time prefix/suffix updates**: No rescanning needed
- **Asynchronous operations**: UI stays responsive during all operations
- **Smart parallelism**: Automatically adjusts to drive type (SSD: high threads, HDD: low threads)
- **Timeout protection**: 30-second timeout per file prevents hanging
- **Batch UI updates**: Updates in groups of 50 for better performance

### ✅ File Management
- Individual checkbox for each file in the grid
- Select All / Deselect All buttons for quick selection
- Single-click on row to toggle selection
- **Cancel button** to stop scans anytime
- **Selections preserved** after undo operations
- Precise control over which files to rename

### 📝 Logging & Rollback System
- **Automatic Logging**: All rename operations logged to `rename_log.json`
- **Detailed Log Entries**: Timestamp, old/new paths, success status, error messages
- **Selective Undo**: Choose which operations to undo with checkboxes
- **Persistent Log**: Works across application restarts
- **Smart Refresh**: After undo, list refreshes instantly without rescanning

### 🎨 Prefix & Suffix System
- **Prefix Input**: Add text before the folder name
- **Suffix Input**: Add text after the folder name
- **Direct Concatenation**: Text added exactly as typed (no automatic dashes or spaces)
- **Instant Testing**: After scanning once, test different prefixes/suffixes instantly!

---

## Requirements

### Essential
- **Windows 10/11** (Recommended) or **Windows 7** (with additional setup)
- **7-Zip** installed ([Download](https://www.7-zip.org/)) - **Required for scanning**
- **.NET 8 Runtime** ([Download](https://dotnet.microsoft.com/download/dotnet/8.0)) - Only for framework-dependent version

### Windows 7 (Requires Additional Steps)
If you want to run on Windows 7, you'll need:

1. **Install 7-Zip** (required for all versions)
2. **Install .NET 8 Runtime** (if using framework-dependent version)
3. **Install Visual C++ Redistributable 2015-2022**
   - [Download x64](https://aka.ms/vs/17/release/vc_redist.x64.exe)
   - Required for api-ms-win-crt runtime

**Note:** Windows 10/11 includes these dependencies by default (except 7-Zip).

---

## 📥 Installation

### Option 1: Self-Contained (Recommended - No .NET Required)

**⬇️ [Download from MediaFire (~65 MB)](https://www.mediafire.com/file/VOTRE_LIEN/RarRenamer.NET-v3.1.0-self-contained.zip/file)**

1. Download the ZIP file from MediaFire
2. Extract `RarRenamer.exe`
3. **Install 7-Zip** from [7-zip.org](https://www.7-zip.org/) if not already installed
4. Run the application
5. **No .NET Runtime needed!** ✨

> **Note:** Due to GitHub's 25 MB file size limit for releases, the self-contained version is hosted on MediaFire.

---

### Option 2: Framework-Dependent (Smaller - Requires .NET 8)

**⬇️ Download from [Releases](https://github.com/L-at-nnes/RarRenamer.NET/releases) (~1 MB)**

1. **Install 7-Zip** from [7-zip.org](https://www.7-zip.org/)
2. Install [.NET 8 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) if not already installed
3. Download `RarRenamer.NET-v3.1.0-framework-dependent.zip` from GitHub Releases
4. Extract and run `RarRenamer.exe`

**Windows 7 Users:** Also install [Visual C++ Redistributable 2015-2022](https://aka.ms/vs/17/release/vc_redist.x64.exe)

---

### Option 3: Build from Source

```bash
git clone https://github.com/L-at-nnes/RarRenamer.NET.git
cd RarRenamer.NET/RarRenamer
dotnet build -c Release
dotnet run
```

---

## Usage

1. **Install 7-Zip** if not already installed ([Download](https://www.7-zip.org/))
2. **Select folder**: Click "Browse" to choose a folder containing RAR files
3. **Scan archives**: Click "Scan Archives"
   - The app detects your drive type (SSD/HDD) and optimizes automatically
   - Status shows parallelism level (e.g., "parallelism: 96" for SSD)
   - **Cancel anytime**: Click red "Cancel Scan" button if needed
4. **Configure prefix/suffix** (optional):
   - Enter prefix (e.g., "P-" or "MyApp ")
   - Enter suffix (e.g., "-v2" or " Portable")
   - **Test instantly**: After first scan, changing prefix/suffix is instant!
5. **Select files**: 
   - Click on rows to toggle selection
   - Use "Select All" or "Deselect All" buttons
6. **Rename**: Click "Rename Selected"
7. **Undo if needed**: 
   - Click "Undo Operations" to revert changes
   - **Selections are preserved!**
   - **Test new suffix instantly** - no rescan needed!

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

1. **Scans** each RAR file using 7-Zip CLI (`7z l -slt`)
2. **Analyzes** the output to find the first top-level folder
3. **Applies** optional prefix/suffix to the folder name
4. **Determines** if renaming is needed
5. **Renames** the RAR file to match the pattern

**Performance Detail:** Only reads RAR headers via 7-Zip (very fast), not the entire archive content. This means a 10 MB archive scans as fast as a 500 GB archive!

---

## Log File Format

The `rename_log.json` file stores all operations:

```json
[
  {
    "Timestamp": "2025-01-24T14:30:45",
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
- **7-Zip CLI** - Ultra-fast RAR archive scanning (10-20x faster than libraries)
- **Newtonsoft.Json 13.0.4** - Efficient JSON logging
- **System.Management** - WMI drive detection for SSD/HDD optimization
- **Async/Await** - Non-blocking UI operations
- **Parallel.ForEachAsync** - Smart parallelism with automatic resource management

---

## Performance Comparison

| Operation | PowerShell (Legacy) | v3.0.1 (SharpCompress) | v3.1.0 (7-Zip CLI) | Improvement |
|-----------|---------------------|------------------------|---------------------|-------------|
| Scan 2300 files (HDD) | Hangs/crashes | **58 minutes** | **3-8 minutes** | **12-20x faster** |
| Scan 3000 files (SSD) | 30 minutes | 5-10 minutes | **30-90 seconds** | **20-60x faster** |
| Undo 100 files | 4-5 minutes | <5 seconds | <1 second | **300x faster** |
| Test new suffix | Full rescan | Full rescan | **Instant** | **∞x faster** |
| UI Responsiveness | Blocked | Responsive | Responsive | **100% better** |
| Selections after undo | Lost | Lost | **Preserved** | **New feature** |

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

### Publish Self-Contained (~156 MB, no dependencies except 7-Zip)
```bash
dotnet publish -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o publish/self-contained
```

### Publish Framework-Dependent (~2 MB, requires .NET 8 and 7-Zip)
```bash
dotnet publish -c Release -r win-x64 --self-contained false \
  -p:PublishSingleFile=true \
  -o publish/framework-dependent
```

---

## Troubleshooting

### Common Issues

**"7-Zip not found" error**
- Install 7-Zip from [7-zip.org](https://www.7-zip.org/)
- Ensure it's in default location: `C:\Program Files\7-Zip\7z.exe`
- Or add 7-Zip to your system PATH

**"No RAR files found"**
- Ensure the selected folder contains .rar files
- Check file permissions

**"Password protected" or "Corrupted archive"**
- Archives are detected but cannot be scanned
- These files will be marked and skipped

**"Timeout" status**
- File took longer than 30 seconds to scan
- Usually indicates a corrupted or very complex archive
- File is skipped automatically

**Undo doesn't work**
- Check if `rename_log.json` exists in the application folder
- Verify files haven't been manually moved/renamed
- Ensure the log file has valid JSON format

**Scan is slow**
- Ensure 7-Zip is installed (library fallback is much slower)
- Check if antivirus is scanning each file
- HDD will always be slower than SSD (this is normal)

### Windows 7 Specific

**"api-ms-win-crt-runtime missing" error**
- Install [Visual C++ Redistributable 2015-2022](https://aka.ms/vs/17/release/vc_redist.x64.exe)
- Reboot after installation

---

## Migration from PowerShell Version

The original [RarRenamer](https://github.com/L-at-nnes/RarRenamer) PowerShell version is now deprecated in favor of RarRenamer.NET.

**What's changed:**
- ✅ 60x faster performance overall
- ✅ 10-20x faster scanning (7-Zip CLI)
- ✅ Modern WPF UI with dark theme
- ✅ No library dependencies (uses 7-Zip CLI like PowerShell version)
- ✅ Single-click selection
- ✅ Real-time prefix/suffix updates
- ✅ Asynchronous operations (responsive UI)
- ✅ Cancel button for long operations
- ✅ Timeout protection (30s per file)
- ✅ Smart parallelism (SSD/HDD detection)
- ✅ Instant refresh after undo (no rescan)
- ✅ Selections preserved after operations

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
- 7-Zip for ultra-fast RAR reading
- .NET community for the excellent ecosystem

---

## Version History

- **v3.1.0** (2025-01-XX): Major performance overhaul - 7-Zip CLI integration, instant refresh after undo, drive detection
- **v3.0.1** (2025-01-XX): Critical performance fix - Added parallelism control, timeout protection, cancel button
- **v3.0.0** (2025-11-24): Complete rewrite in C# WPF .NET 8 with 60x performance improvement
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
