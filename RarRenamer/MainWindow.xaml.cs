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
    private ObservableCollection<QueueItem> _queueItems = new ObservableCollection<QueueItem>();
    private LogManager _logManager;
    private QueueManager _queueManager;
    private string _selectedFolder = string.Empty;
    private bool _isScanning = false;
    private bool _isPaused = false;
    private CancellationTokenSource? _cancellationTokenSource;
    private int _autoDetectedThreads = 16;
    private System.Windows.Threading.DispatcherTimer? _threadButtonTimer;
    private int _threadButtonDirection = 0;
    private bool _debugLogEnabled = false;
    private readonly string _debugLogPath = "debug_log.txt";
    private readonly object _logLock = new object();
    private const int MaxLogLines = 500;
    private bool _isSyncingSelection = false;

    public MainWindow()
    {
        InitializeComponent();
        dgResults.ItemsSource = _rarFiles;
        dgQueue.ItemsSource = _queueItems;
        _logManager = new LogManager("rename_log.json");
        _queueManager = new QueueManager("queue.json");
        
        _selectedFolder = AppDomain.CurrentDomain.BaseDirectory;
        txtFolder.Text = _selectedFolder;

        _autoDetectedThreads = DriveDetector.GetOptimalParallelism(_selectedFolder);
        txtThreads.Text = _autoDetectedThreads.ToString();

        // Charger la queue au démarrage
        LoadQueueFromFile();

        // Initialiser le log
        AddLog("Application started");
    }

    private void AddLog(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var logEntry = $"[{timestamp}] {message}";
        
        Dispatcher.InvokeAsync(() =>
        {
            txtLog.AppendText(logEntry + Environment.NewLine);
            
            // Limiter à MaxLogLines pour éviter la consommation RAM
            var lines = txtLog.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            if (lines.Length > MaxLogLines)
            {
                // Garder seulement les dernières MaxLogLines lignes
                txtLog.Text = string.Join(Environment.NewLine, lines.Skip(lines.Length - MaxLogLines));
            }
            
            scrollViewerLog.ScrollToEnd();
        });

        if (_debugLogEnabled)
        {
            lock (_logLock)
            {
                try
                {
                    File.AppendAllText(_debugLogPath, logEntry + Environment.NewLine);
                }
                catch { }
            }
        }
    }

    private void ChkEnableDebugLog_Changed(object sender, RoutedEventArgs e)
    {
        _debugLogEnabled = chkEnableDebugLog.IsChecked == true;
        
        if (_debugLogEnabled)
        {
            try
            {
                File.WriteAllText(_debugLogPath, $"=== Debug Log Started: {DateTime.Now} ===" + Environment.NewLine);
                AddLog($"Debug logging enabled → {_debugLogPath}");
            }
            catch (Exception ex)
            {
                AddLog($"ERROR: Could not create debug log file: {ex.Message}");
                chkEnableDebugLog.IsChecked = false;
                _debugLogEnabled = false;
            }
        }
        else
        {
            AddLog("Debug logging disabled");
        }
    }

    private void DataGridRow_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Cette méthode n'est plus utilisée - comportement standard de la checkbox
    }

    private int? ParseThreadsInput()
    {
        string input = txtThreads.Text.Trim();
        
        if (string.IsNullOrEmpty(input))
        {
            return null;
        }

        if (int.TryParse(input, out int threads) && threads > 0 && threads <= 256)
        {
            return threads;
        }

        return null;
    }

    private void TxtThreads_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
        e.Handled = !int.TryParse(e.Text, out _);
    }

    private void TxtThreads_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (int.TryParse(txtThreads.Text, out int value))
        {
            if (value < 1)
                txtThreads.Text = "1";
            else if (value > 256)
                txtThreads.Text = "256";
        }
    }

    private void BtnThreadsUp_Click(object sender, RoutedEventArgs e)
    {
        IncrementThreads(1);
    }

    private void BtnThreadsDown_Click(object sender, RoutedEventArgs e)
    {
        IncrementThreads(-1);
    }

    private void BtnThreadsUpDown_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn)
        {
            _threadButtonDirection = btn.Content.ToString() == "▲" ? 1 : -1;
            
            _threadButtonTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _threadButtonTimer.Tick += (s, args) => IncrementThreads(_threadButtonDirection);
            _threadButtonTimer.Start();
        }
    }

    private void BtnThreadsUpDown_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        _threadButtonTimer?.Stop();
        _threadButtonTimer = null;
    }

    private void IncrementThreads(int delta)
    {
        if (int.TryParse(txtThreads.Text, out int current))
        {
            int newValue = Math.Clamp(current + delta, 1, 256);
            txtThreads.Text = newValue.ToString();
        }
        else
        {
            txtThreads.Text = _autoDetectedThreads.ToString();
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

            _autoDetectedThreads = DriveDetector.GetOptimalParallelism(_selectedFolder);
            
            if (!int.TryParse(txtThreads.Text, out _))
            {
                txtThreads.Text = _autoDetectedThreads.ToString();
            }
        }
    }

    private void TxtFolder_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Mettre à jour le dossier sélectionné quand l'utilisateur tape/colle
        string path = txtFolder.Text.Trim();
        if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
        {
            _selectedFolder = path;
            _autoDetectedThreads = DriveDetector.GetOptimalParallelism(_selectedFolder);
            
            if (!int.TryParse(txtThreads.Text, out _))
            {
                txtThreads.Text = _autoDetectedThreads.ToString();
            }
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

        if (!Directory.Exists(_selectedFolder))
        {
            MessageBox.Show("The specified folder does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            _isPaused = false;
            _cancellationTokenSource = new CancellationTokenSource();
            _rarFiles.Clear();

            AddLog($"Starting scan in folder: {_selectedFolder}");

            btnBrowse.IsEnabled = false;
            btnScan.IsEnabled = false;
            btnPauseResume.IsEnabled = true;
            btnPauseResume.Visibility = Visibility.Visible;
            btnPauseResume.Content = "Pause Scan";
            btnCancel.IsEnabled = true;
            btnCancel.Visibility = Visibility.Visible;
            btnRename.IsEnabled = false;
            btnUndo.IsEnabled = false;
            btnSelectAll.IsEnabled = false;
            btnDeselectAll.IsEnabled = false;
            txtPrefix.IsEnabled = false;
            txtSuffix.IsEnabled = false;
            txtFolder.IsEnabled = false;
            txtThreads.IsEnabled = false;
            btnThreadsUp.IsEnabled = false;
            btnThreadsDown.IsEnabled = false;

            var rarFilePaths = Directory.GetFiles(_selectedFolder, "*.rar", SearchOption.TopDirectoryOnly);
            int totalFiles = rarFilePaths.Length;

            AddLog($"Found {totalFiles} RAR files");

            if (totalFiles == 0)
            {
                MessageBox.Show("No RAR files found in this folder.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int optimalParallelism = DriveDetector.GetOptimalParallelism(_selectedFolder, ParseThreadsInput());
            AddLog($"Using {optimalParallelism} threads");
            lblStatus.Content = $"Scanning {totalFiles} RAR files (threads: {optimalParallelism})...";
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
                    // Vérifier si en pause
                    while (_isPaused && !cancellationToken.IsCancellationRequested)
                    {
                        await Task.Delay(100, CancellationToken.None);
                    }

                    if (cancellationToken.IsCancellationRequested)
                        return;

                    var fileName = Path.GetFileName(filePath);
                    var scanResult = await RarScanner.ScanArchiveAsync(filePath, timeoutSeconds: 60);

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

            AddLog($"Scan complete: {_rarFiles.Count(r => r.CanRename)}/{totalFiles} files ready to rename");
        }
        catch (OperationCanceledException)
        {
            string statusMsg = _isPaused ? "Scan cancelled while paused" : "Scan cancelled by user";
            lblStatus.Content = statusMsg;
            progressBar.Visibility = Visibility.Collapsed;
            AddLog(statusMsg);
            MessageBox.Show("Scan was cancelled.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            AddLog($"ERROR during scan: {ex.Message}");
            AddLog($"Stack trace: {ex.StackTrace}");
            MessageBox.Show($"Error during scan: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            lblStatus.Content = "Error during scan";
        }
        finally
        {
            _isScanning = false;
            _isPaused = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            
            btnBrowse.IsEnabled = true;
            btnScan.IsEnabled = true;
            btnPauseResume.IsEnabled = false;
            btnPauseResume.Visibility = Visibility.Collapsed;
            btnCancel.IsEnabled = false;
            btnCancel.Visibility = Visibility.Collapsed;
            btnRename.IsEnabled = true;
            btnUndo.IsEnabled = true;
            btnSelectAll.IsEnabled = true;
            btnDeselectAll.IsEnabled = true;
            txtPrefix.IsEnabled = true;
            txtSuffix.IsEnabled = true;
            txtFolder.IsEnabled = true;
            txtThreads.IsEnabled = true;
            btnThreadsUp.IsEnabled = true;
            btnThreadsDown.IsEnabled = true;
            progressBar.Visibility = Visibility.Collapsed;
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
        {
            _cancellationTokenSource.Cancel();
            btnCancel.IsEnabled = false;
            btnPauseResume.IsEnabled = false;
            lblStatus.Content = "Cancelling scan...";
        }
    }

    private void BtnPauseResume_Click(object sender, RoutedEventArgs e)
    {
        if (_isScanning)
        {
            _isPaused = !_isPaused;
            
            if (_isPaused)
            {
                btnPauseResume.Content = "Resume Scan";
                lblStatus.Content = "Scan paused...";
                AddLog("Scan paused by user");
            }
            else
            {
                btnPauseResume.Content = "Pause Scan";
                lblStatus.Content = "Scanning...";
                AddLog("Scan resumed by user");
            }
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
        var selectedItems = _queueItems.Where(q => q.IsSelected).ToList();

        if (selectedItems.Count == 0)
        {
            MessageBox.Show("No files selected for renaming in queue.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = MessageBox.Show(
            $"Rename {selectedItems.Count} file(s) from queue?",
            "Confirmation",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question
        );

        if (result != MessageBoxResult.Yes) return;

        AddLog($"Starting rename operation for {selectedItems.Count} files...");

        try
        {
            int successCount = 0;
            int errorCount = 0;
            var logEntries = new List<LogEntry>();
            var itemsToRemove = new List<QueueItem>();

            foreach (var item in selectedItems)
            {
                try
                {
                    string oldPath = item.FullPath;
                    string directory = Path.GetDirectoryName(oldPath) ?? string.Empty;
                    string newPath = Path.Combine(directory, item.NewName);

                    AddLog($"Renaming: {item.CurrentName} → {item.NewName}");

                    if (!File.Exists(oldPath))
                    {
                        errorCount++;
                        AddLog($"  ERROR: Source file not found: {oldPath}");
                        logEntries.Add(new LogEntry
                        {
                            Timestamp = DateTime.Now,
                            OldPath = oldPath,
                            NewPath = newPath,
                            OldName = item.CurrentName,
                            NewName = item.NewName,
                            Success = false,
                            Error = "Source file no longer exists"
                        });
                        itemsToRemove.Add(item);
                        continue;
                    }

                    if (File.Exists(newPath))
                    {
                        errorCount++;
                        AddLog($"  ERROR: Target already exists: {newPath}");
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

                    successCount++;
                    itemsToRemove.Add(item);
                    AddLog($"  SUCCESS: Renamed to {item.NewName}");

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
                    errorCount++;
                    AddLog($"  ERROR: {ex.Message}");
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

            // Retirer les items renommés avec succès de la queue
            foreach (var item in itemsToRemove)
            {
                _queueItems.Remove(item);
            }

            _logManager.SaveLogs(logEntries);
            SaveQueueToFile();
            UpdateQueueCount();

            AddLog($"Rename complete: {successCount} success, {errorCount} errors");
            string message = $"Rename complete:\n✅ {successCount} success\n❌ {errorCount} error(s)";
            MessageBox.Show(message, "Result", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            AddLog($"CRITICAL ERROR during rename: {ex.Message}");
            AddLog($"Stack trace: {ex.StackTrace}");
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

    // ==================== QUEUE MANAGEMENT ====================

    private void LoadQueueFromFile()
    {
        var items = _queueManager.LoadQueue();
        _queueItems.Clear();
        foreach (var item in items)
        {
            _queueItems.Add(item);
        }
        UpdateQueueCount();
    }

    private void SaveQueueToFile()
    {
        _queueManager.SaveQueue(_queueItems.ToList());
    }

    private void UpdateQueueCount()
    {
        lblQueueCount.Content = $"Queue: {_queueItems.Count} files";
    }

    private void BtnAddToQueue_Click(object sender, RoutedEventArgs e)
    {
        var selectedItems = _rarFiles.Where(r => r.IsSelected && r.CanRename).ToList();

        if (selectedItems.Count == 0)
        {
            MessageBox.Show("No files selected to add to queue.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        AddLog($"Adding {selectedItems.Count} files to queue...");

        int addedCount = 0;
        foreach (var item in selectedItems)
        {
            // Vérifier si déjà dans la queue
            if (_queueItems.Any(q => q.FullPath == item.FullPath))
                continue;

            var queueItem = new QueueItem
            {
                CurrentName = item.CurrentName,
                NewName = item.NewName,
                FullPath = item.FullPath,
                IsSelected = true
            };
            _queueItems.Add(queueItem);
            addedCount++;
        }

        SaveQueueToFile();
        UpdateQueueCount();
        AddLog($"Added {addedCount} files to queue (total: {_queueItems.Count})");
        MessageBox.Show($"{addedCount} file(s) added to queue!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnClearQueue_Click(object sender, RoutedEventArgs e)
    {
        if (_queueItems.Count == 0)
        {
            MessageBox.Show("Queue is already empty.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"Clear all {_queueItems.Count} items from queue?",
            "Confirmation",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question
        );

        if (result == MessageBoxResult.Yes)
        {
            _queueItems.Clear();
            _queueManager.ClearQueue();
            UpdateQueueCount();
        }
    }

    private void BtnLoadQueue_Click(object sender, RoutedEventArgs e)
    {
        LoadQueueFromFile();
        MessageBox.Show($"Queue loaded: {_queueItems.Count} file(s)", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnQueueSelectAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var item in _queueItems)
        {
            item.IsSelected = true;
        }
    }

    private void BtnQueueDeselectAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var item in _queueItems)
        {
            item.IsSelected = false;
        }
    }

    // ==================== SELECTION MANAGEMENT ====================

    private void DgResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSyncingSelection) return;

        _isSyncingSelection = true;
        try
        {
            // Synchroniser la sélection DataGrid avec IsSelected
            foreach (RarFileItem item in e.AddedItems)
            {
                if (item.CanRename)
                    item.IsSelected = true;
            }
            
            foreach (RarFileItem item in e.RemovedItems)
            {
                item.IsSelected = false;
            }
        }
        finally
        {
            _isSyncingSelection = false;
        }
    }

    private void DgQueue_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSyncingSelection) return;

        _isSyncingSelection = true;
        try
        {
            // Synchroniser la sélection DataGrid avec IsSelected
            foreach (QueueItem item in e.AddedItems)
            {
                item.IsSelected = true;
            }
            
            foreach (QueueItem item in e.RemovedItems)
            {
                item.IsSelected = false;
            }
        }
        finally
        {
            _isSyncingSelection = false;
        }
    }

    // ==================== DOUBLE-CLICK TO OPEN FILE ====================

    private void DgResults_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (dgResults.SelectedItem is RarFileItem item)
        {
            OpenFileInExplorer(item.FullPath);
        }
    }

    private void DgQueue_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (dgQueue.SelectedItem is QueueItem item)
        {
            OpenFileInExplorer(item.FullPath);
        }
    }

    private void OpenFileInExplorer(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                // Ouvrir l'explorateur et sélectionner le fichier
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{filePath}\"");
                AddLog($"Opened in explorer: {Path.GetFileName(filePath)}");
            }
            else
            {
                MessageBox.Show($"File not found:\n{filePath}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                AddLog($"ERROR: File not found: {filePath}");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error opening file:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            AddLog($"ERROR opening file: {ex.Message}");
        }
    }
}