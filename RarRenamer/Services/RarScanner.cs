using SharpCompress.Archives;
using SharpCompress.Archives.Rar;

namespace RarRenamer.Services
{
    public class ScanResult
    {
        public string FolderName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsPasswordProtected { get; set; }
        public bool IsCorrupted { get; set; }
    }

    public static class RarScanner
    {
        public static async Task<ScanResult> ScanArchiveAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                var result = new ScanResult();

                try
                {
                    using var archive = RarArchive.Open(filePath);

                    var firstFolder = archive.Entries
                        .Where(e => e.IsDirectory)
                        .Where(e => e.Key != null && !e.Key.Contains('/') && !e.Key.Contains('\\'))
                        .FirstOrDefault();

                    if (firstFolder != null && firstFolder.Key != null)
                    {
                        result.FolderName = firstFolder.Key.TrimEnd('/', '\\');
                        result.Status = "? Ready";
                    }
                    else
                    {
                        result.Status = "?? No root folder";
                    }
                }
                catch (Exception ex) when (ex.Message.Contains("password") || ex.Message.Contains("encrypted"))
                {
                    result.Status = "?? Password protected";
                    result.IsPasswordProtected = true;
                }
                catch (Exception)
                {
                    result.Status = "? Corrupted archive";
                    result.IsCorrupted = true;
                }

                return result;
            });
        }
    }
}
