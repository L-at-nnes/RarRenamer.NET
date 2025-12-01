# v3.4.1 - UX Improvements & Bug Fixes

## ?? What's New

### Multi-Selection Made Easy
- ? **Ctrl+Click**: Select/deselect individual files
- ? **Shift+Click**: Select ranges of files
- ? Works in all tabs: Scan, Queue, and Undo Operations
- ? **No more clicking 15 minutes** to deselect hundreds of files!

### Resizable Interface
- ? **GridSplitter**: Drag the separator to resize log and file list panels
- ? Adjust heights to your preference
- ? More flexible workspace

### Quick File Access
- ? **Double-click any file** to open it in Windows Explorer
- ? Works in both Scan and Queue tabs
- ? Instant file location

### Performance & Stability
- ? **Logging optimized**: Limited to 500 lines max (reduces RAM usage)
- ? **Log bumping fixed**: Clean logs during scan (no more repeated entries)
- ? **Full virtualization**: Smooth scrolling even with 3000+ files
- ? **Queue persistence verified**: Queue correctly loads on startup

---

## ?? Downloads

Choose the version that fits your needs:

### Self-Contained (~156 MB) - Recommended
- No .NET installation required
- Just needs 7-Zip
- Download: `RarRenamer.exe` from assets below

### Framework-Dependent (~1.6 MB)
- Requires .NET 8 Runtime
- Smaller download size
- Download: `RarRenamer.exe` (Framework-Dependent) from assets below

---

## ?? Installation

1. Download your preferred version from the assets below
2. Install 7-Zip if not already installed ([Download](https://www.7-zip.org/))
3. Run `RarRenamer.exe`

**Windows 7 users**: Also install [Visual C++ Redistributable](https://aka.ms/vs/17/release/vc_redist.x64.exe)

---

## ?? Technical Details

### Changes
- Added `SelectionMode="Extended"` to all DataGrids
- Implemented `SelectionChanged` event handlers for Ctrl/Shift support
- Added GridSplitter with `ResizeBehavior="PreviousAndNext"`
- Implemented double-click file opening via `explorer.exe /select`
- Limited log to 500 lines with automatic trimming
- Removed excessive logging during scan operations
- Enhanced DataGrid virtualization settings

### Bug Fixes
- Fixed log "bumping" issue (repeated entries)
- Fixed queue persistence loading
- Improved RAM usage during long scans
- Enhanced scrolling performance

---

## ?? Credits

Thanks to user feedback for reporting the selection exhaustion issue and suggesting improvements!

---

**Full Changelog**: https://github.com/L-at-nnes/RarRenamer.NET/compare/v3.4.0...v3.4.1
