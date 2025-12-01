using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RarRenamer.Models;
using RarRenamer.Services;
using MessageBox = System.Windows.MessageBox;

namespace RarRenamer;

public partial class UndoWindow : Window
{
    private LogManager _logManager;
    private ObservableCollection<LogEntryViewModel> _logs = new ObservableCollection<LogEntryViewModel>();
    private bool _isSyncingSelection = false;

    public UndoWindow(LogManager logManager)
    {
        InitializeComponent();
        _logManager = logManager;
        LoadLogs();
        dgLogs.ItemsSource = _logs;
    }

    private void LoadLogs()
    {
        var logs = _logManager.GetSuccessfulLogs();
        _logs.Clear();

        foreach (var log in logs.OrderByDescending(l => l.Timestamp))
        {
            _logs.Add(new LogEntryViewModel(log));
        }
    }

    private void DgLogs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSyncingSelection) return;

        _isSyncingSelection = true;
        try
        {
            // Synchroniser la sélection DataGrid avec IsSelected
            foreach (LogEntryViewModel item in e.AddedItems)
            {
                item.IsSelected = true;
            }
            
            foreach (LogEntryViewModel item in e.RemovedItems)
            {
                item.IsSelected = false;
            }
        }
        finally
        {
            _isSyncingSelection = false;
        }
    }

    private void DataGridRow_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Cette méthode n'est plus utilisée - comportement standard de la checkbox
    }

    private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var log in _logs)
        {
            log.IsSelected = true;
        }
    }

    private void BtnDeselectAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var log in _logs)
        {
            log.IsSelected = false;
        }
    }

    private void BtnUndoSelected_Click(object sender, RoutedEventArgs e)
    {
        var selectedLogs = _logs.Where(l => l.IsSelected).ToList();

        if (selectedLogs.Count == 0)
        {
            MessageBox.Show("No operations selected.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = MessageBox.Show(
            $"Undo {selectedLogs.Count} operation(s)?",
            "Confirmation",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question
        );

        if (result != MessageBoxResult.Yes) return;

        try
        {
            int successCount = 0;
            int errorCount = 0;
            var logsToRemove = new List<LogEntry>();

            foreach (var logVM in selectedLogs)
            {
                var log = logVM.LogEntry;

                try
                {
                    if (!File.Exists(log.NewPath))
                    {
                        MessageBox.Show(
                            $"File '{log.NewName}' does not exist.\nPath: {log.NewPath}",
                            "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                        errorCount++;
                        continue;
                    }

                    if (File.Exists(log.OldPath))
                    {
                        MessageBox.Show(
                            $"File '{log.OldName}' already exists.\nCannot restore.",
                            "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                        errorCount++;
                        continue;
                    }

                    File.Move(log.NewPath, log.OldPath);
                    logsToRemove.Add(log);
                    successCount++;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error undoing '{log.NewName}':\n{ex.Message}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                    errorCount++;
                }
            }

            if (logsToRemove.Count > 0)
            {
                _logManager.RemoveLogs(logsToRemove);
            }

            string message = $"Undo complete:\n? {successCount} operation(s) undone\n? {errorCount} error(s)";
            MessageBox.Show(message, "Result", MessageBoxButton.OK, MessageBoxImage.Information);

            if (successCount > 0)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                LoadLogs();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error during undo: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

public class LogEntryViewModel : INotifyPropertyChanged
{
    private bool _isSelected;
    public LogEntry LogEntry { get; set; }

    public LogEntryViewModel(LogEntry logEntry)
    {
        LogEntry = logEntry;
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }
    }

    public DateTime Timestamp => LogEntry.Timestamp;
    public string OldName => LogEntry.OldName;
    public string NewName => LogEntry.NewName;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
