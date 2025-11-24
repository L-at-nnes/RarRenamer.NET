using System.IO;
using Newtonsoft.Json;
using RarRenamer.Models;

namespace RarRenamer.Services
{
    public class LogManager
    {
        private readonly string _logFilePath;
        private List<LogEntry> _logs;

        public LogManager(string logFilePath)
        {
            _logFilePath = logFilePath;
            _logs = LoadLogs();
        }

        private List<LogEntry> LoadLogs()
        {
            if (!File.Exists(_logFilePath))
            {
                return new List<LogEntry>();
            }

            try
            {
                var json = File.ReadAllText(_logFilePath);
                return JsonConvert.DeserializeObject<List<LogEntry>>(json) ?? new List<LogEntry>();
            }
            catch
            {
                return new List<LogEntry>();
            }
        }

        public void SaveLogs(List<LogEntry> newLogs)
        {
            _logs.AddRange(newLogs);
            var json = JsonConvert.SerializeObject(_logs, Formatting.Indented);
            File.WriteAllText(_logFilePath, json);
        }

        public List<LogEntry> GetSuccessfulLogs()
        {
            return _logs.Where(l => l.Success).ToList();
        }

        public void RemoveLogs(List<LogEntry> logsToRemove)
        {
            foreach (var log in logsToRemove)
            {
                _logs.Remove(log);
            }

            if (_logs.Count == 0)
            {
                if (File.Exists(_logFilePath))
                {
                    File.Delete(_logFilePath);
                }
            }
            else
            {
                var json = JsonConvert.SerializeObject(_logs, Formatting.Indented);
                File.WriteAllText(_logFilePath, json);
            }
        }
    }
}
