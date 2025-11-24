namespace RarRenamer.Models
{
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string OldPath { get; set; } = string.Empty;
        public string NewPath { get; set; } = string.Empty;
        public string OldName { get; set; } = string.Empty;
        public string NewName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? Error { get; set; }
    }
}
