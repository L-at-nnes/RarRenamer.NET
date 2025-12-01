using System.IO;
using Newtonsoft.Json;
using RarRenamer.Models;

namespace RarRenamer.Services
{
    public class QueueManager
    {
        private readonly string _queueFilePath;

        public QueueManager(string queueFilePath = "queue.json")
        {
            _queueFilePath = queueFilePath;
        }

        public void SaveQueue(List<QueueItem> items)
        {
            try
            {
                var json = JsonConvert.SerializeObject(items, Formatting.Indented);
                File.WriteAllText(_queueFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving queue: {ex.Message}");
            }
        }

        public List<QueueItem> LoadQueue()
        {
            if (!File.Exists(_queueFilePath))
            {
                return new List<QueueItem>();
            }

            try
            {
                var json = File.ReadAllText(_queueFilePath);
                return JsonConvert.DeserializeObject<List<QueueItem>>(json) ?? new List<QueueItem>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading queue: {ex.Message}");
                return new List<QueueItem>();
            }
        }

        public void ClearQueue()
        {
            try
            {
                if (File.Exists(_queueFilePath))
                {
                    File.Delete(_queueFilePath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing queue: {ex.Message}");
            }
        }

        public bool HasQueue()
        {
            return File.Exists(_queueFilePath) && new FileInfo(_queueFilePath).Length > 0;
        }
    }
}
