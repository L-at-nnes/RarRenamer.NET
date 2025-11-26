using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RarRenamer.Models;
using RarRenamer.Services;
using MessageBox = System.Windows.MessageBox;
using System.Threading;

namespace RarRenamer;

public partial class MainWindow : Window
{
    private ObservableCollection<RarFileItem> _rarFiles = new ObservableCollection<RarFileItem>();
    private LogManager _logManager;
    private string _selectedFolder = string.Empty;
    private bool _isScanning = false;
    private CancellationTokenSource? _cancellationTokenSource;

    public MainWindow()
    {
        InitializeComponent();
        dgResults.ItemsSource = _rarFiles;
        _logManager = new LogManager("rename_log.json");
        
        _selectedFolder = AppDomain.CurrentDomain.BaseDirectory;
        txtFolder.Text = _selectedFolder;
    }

    private void DataGridRow_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is DataGridRow row && row.Item is RarFileItem item)
        {
            if (item.CanRename)
            {
                item.IsSelected = !item.IsSelected;
                e.Handled = true;
            }
        }
    }

    private void BtnBrowse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select folder containing RAR files",
            ShowNewFolderButton = false,
            SelectedPath = _selectedFolder
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            _selectedFolder = dialog.SelectedPath;
            txtFolder.Text = _selectedFolder;
            lblStatus.Content = $"Folder selected: {_selectedFolder}";
        }
    }

    private void TxtPrefixSuffix_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_rarFiles.Count == 0) return;

        string prefix = txtPrefix.Text;
        string suffix = txtSuffix.Text;

        foreach (var item in _rarFiles)
        {
            if (!string.IsNullOrEmpty(item.FolderName))
            {
                item.NewName = $"{prefix}{item.FolderName}{suffix}.rar";
            }
        }
    }

    private async void BtnScan_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_selectedFolder))
        {
            MessageBox.Show("Please select a folder.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_isScanning)
        {
            MessageBox.Show("Scan in progress...", "Warning", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            _isScanning = true;
            _cancellationTokenSource = new CancellationTokenSource();
            _rarFiles.Clear();

            btnBrowse.IsEnabled = false;
            btnScan.IsEnabled = false;
            btnCancel.IsEnabled = true;
            btnCancel.Visibility = Visibility.Visible;
            btnRename.IsEnabled = false;
            btnUndo.IsEnabled = false;
            btnSelectAll.IsEnabled = false;
            btnDeselectAll.IsEnabled = false;
            txtPrefix.IsEnabled = false;
            txtSuffix.IsEnabled = false;

            var rarFilePaths = Directory.GetFiles(_selectedFolder, "*.rar", SearchOption.TopDirectoryOnly);
            int totalFiles = rarFilePaths.Length;

            if (totalFiles == 0)
            {
                MessageBox.Show("No RAR files found in this folder.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int optimalParallelism = DriveDetector.GetOptimalParallelism(_selectedFolder);
            lblStatus.Content = $"Scanning {totalFiles} RAR files (parallelism: {optimalParallelism})...";
            progressBar.Visibility = Visibility.Visible;
            progressBar.Maximum = totalFiles;
            progressBar.Value = 0;

            string prefix = txtPrefix.Text;
            string suffix = txtSuffix.Text;

            int processedCount = 0;
            var buffer = new List<RarFileItem>();
            int batchSize = 50;
            var bufferLock = new object();

            await Parallel.ForEachAsync(
                rarFilePaths,
                new ParallelOptions 
                { 
                    MaxDegreeOfParallelism = optimalParallelism,
                    CancellationToken = _cancellationTokenSource.Token
                },
                async (filePath, cancellationToken) =>
                {
                    var fileName = Path.GetFileName(filePath);
                    var scanResult = await RarScanner.ScanArchiveAsync(filePath);

                    var item = new RarFileItem
                    {
                        CurrentName = fileName,
                        FullPath = filePath,
                        FolderName = scanResult.FolderName,
                        Status = scanResult.Status,
                        IsSelected = !string.IsNullOrEmpty(scanResult.FolderName),
                        CanRename = !string.IsNullOrEmpty(scanResult.FolderName)
                    };

                    if (!string.IsNullOrEmpty(scanResult.FolderName))
                    {
                        item.NewName = $"{prefix}{scanResult.FolderName}{suffix}.rar";
                    }

                    List<RarFileItem>? itemsToAdd = null;
                    int currentCount = 0;
                    
                    lock (bufferLock)
                    {
                        buffer.Add(item);
                        processedCount++;
                        currentCount = processedCount;

                        if (buffer.Count >= batchSize)
                        {
                            itemsToAdd = buffer.ToList();
                            buffer.Clear();
                        }
                    }

                    if (itemsToAdd != null)
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            foreach (var i in itemsToAdd)
                                _rarFiles.Add(i);
                            
                            progressBar.Value = currentCount;
                            lblStatus.Content = $"Scanned: {currentCount}/{totalFiles}";
                        });
                    }
                }
            );

            if (buffer.Count > 0)
            {
                var itemsToAdd = buffer.ToList();
                await Dispatcher.InvokeAsync(() =>
                {
                    foreach (var i in itemsToAdd)
                        _rarFiles.Add(i);
                    
                    progressBar.Value = processedCount;
                });
            }

            lblStatus.Content = $"Scan complete: {_rarFiles.Count(r => r.CanRename)}/{totalFiles} files can be renamed";
            progressBar.Visibility = Visibility.Collapsed;
        }
        catch (OperationCanceledException)
        {
            lblStatus.Content = "Scan cancelled by user";
            progressBar.Visibility = Visibility.Collapsed;
            MessageBox.Show("Scan was cancelled.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error during scan: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            lblStatus.Content = "Error during scan";
        }
        finally
        {
            _isScanning = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            
            btnBrowse.IsEnabled = true;
            btnScan.IsEnabled = true;
            btnCancel.IsEnabled = false;
            btnCancel.Visibility = Visibility.Collapsed;
            btnRename.IsEnabled = true;
            btnUndo.IsEnabled = true;
            btnSelectAll.IsEnabled = true;
            btnDeselectAll.IsEnabled = true;
            txtPrefix.IsEnabled = true;
            txtSuffix.IsEnabled = true;
            progressBar.Visibility = Visibility.Collapsed;
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
        {
            _cancellationTokenSource.Cancel();
            btnCancel.IsEnabled = false;
            lblStatus.Content = "Cancelling scan...";
        }
    }

    private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var item in _rarFiles.Where(r => r.CanRename))
        {
            item.IsSelected = true;
        }
    }

    private void BtnDeselectAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var item in _rarFiles)
        {
            item.IsSelected = false;
        }
    }

    private void BtnRename_Click(object sender, RoutedEventArgs e)
    {
        var selectedItems = _rarFiles.Where(r => r.IsSelected && r.CanRename).ToList();

        if (selectedItems.Count == 0)
        {
            MessageBox.Show("No files selected for renaming.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = MessageBox.Show(
            $"Rename {selectedItems.Count} file(s)?",
            "Confirmation",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question
        );

        if (result != MessageBoxResult.Yes) return;

        try
        {
            int successCount = 0;
            int errorCount = 0;
            var logEntries = new List<LogEntry>();

            foreach (var item in selectedItems)
            {
                try
                {
                    string oldPath = item.FullPath;
                    string directory = Path.GetDirectoryName(oldPath) ?? string.Empty;
                    string newPath = Path.Combine(directory, item.NewName);

                    if (File.Exists(newPath))
                    {
                        item.Status = "❌ Target file already exists";
                        errorCount++;

                        logEntries.Add(new LogEntry
                        {
                            Timestamp = DateTime.Now,
                            OldPath = oldPath,
                            NewPath = newPath,
                            OldName = item.CurrentName,
                            NewName = item.NewName,
                            Success = false,
                            Error = "Target file already exists"
                        });
                        continue;
                    }

                    File.Move(oldPath, newPath);

                    item.CurrentName = item.NewName;
                    item.FullPath = newPath;
                    item.Status = "✅ Renamed";
                    item.IsSelected = false;
                    successCount++;

                    logEntries.Add(new LogEntry
                    {
                        Timestamp = DateTime.Now,
                        OldPath = oldPath,
                        NewPath = newPath,
                        OldName = Path.GetFileName(oldPath),
                        NewName = item.NewName,
                        Success = true
                    });
                }
                catch (Exception ex)
                {
                    item.Status = $"❌ Error: {ex.Message}";
                    errorCount++;

                    logEntries.Add(new LogEntry
                    {
                        Timestamp = DateTime.Now,
                        OldPath = item.FullPath,
                        NewPath = Path.Combine(Path.GetDirectoryName(item.FullPath) ?? string.Empty, item.NewName),
                        OldName = item.CurrentName,
                        NewName = item.NewName,
                        Success = false,
                        Error = ex.Message
                    });
                }
            }

            _logManager.SaveLogs(logEntries);

            string message = $"Rename complete:\n✅ {successCount} success\n❌ {errorCount} error(s)";
            MessageBox.Show(message, "Result", MessageBoxButton.OK, MessageBoxImage.Information);
            lblStatus.Content = $"Rename complete: {successCount} success, {errorCount} error(s)";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error during rename: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnUndo_Click(object sender, RoutedEventArgs e)
    {
        var logs = _logManager.GetSuccessfulLogs();

        if (logs.Count == 0)
        {
            MessageBox.Show("No operations to undo.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var undoWindow = new UndoWindow(_logManager);
        undoWindow.Owner = this;
        undoWindow.ShowDialog();

        if (undoWindow.DialogResult == true)
        {
            RefreshFileList();
        }
    }

    private void RefreshFileList()
    {
        string prefix = txtPrefix.Text;
        string suffix = txtSuffix.Text;

        for (int i = _rarFiles.Count - 1; i >= 0; i--)
        {
            var item = _rarFiles[i];
            
            if (!File.Exists(item.FullPath))
            {
                _rarFiles.RemoveAt(i);
                continue;
            }

            item.CurrentName = Path.GetFileName(item.FullPath);
            
            if (!string.IsNullOrEmpty(item.FolderName))
            {
                item.NewName = $"{prefix}{item.FolderName}{suffix}.rar";
            }
        }

        lblStatus.Content = $"File list refreshed: {_rarFiles.Count} files";
    }
}