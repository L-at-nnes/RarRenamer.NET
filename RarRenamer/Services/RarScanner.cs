using System.Diagnostics;
using System.IO;

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
        private static string? _sevenZipPath;

        static RarScanner()
        {
            _sevenZipPath = Find7ZipPath();
        }

        private static string? Find7ZipPath()
        {
            var possiblePaths = new[]
            {
                @"C:\Program Files\7-Zip\7z.exe",
                @"C:\Program Files (x86)\7-Zip\7z.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7-Zip", "7z.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "7-Zip", "7z.exe")
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                    return path;
            }

            var envPath = Environment.GetEnvironmentVariable("PATH");
            if (envPath != null)
            {
                foreach (var dir in envPath.Split(Path.PathSeparator))
                {
                    var fullPath = Path.Combine(dir, "7z.exe");
                    if (File.Exists(fullPath))
                        return fullPath;
                }
            }

            return null;
        }

        public static async Task<ScanResult> ScanArchiveAsync(string filePath)
        {
            if (_sevenZipPath == null)
            {
                return new ScanResult
                {
                    Status = "? 7-Zip not found",
                    IsCorrupted = true
                };
            }

            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            
            try
            {
                return await Task.Run(() =>
                {
                    var result = new ScanResult();

                    try
                    {
                        var process = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = _sevenZipPath,
                                Arguments = $"l -slt \"{filePath}\"",
                                UseShellExecute = false,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                CreateNoWindow = true,
                                StandardOutputEncoding = System.Text.Encoding.UTF8
                            }
                        };

                        process.Start();
                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();
                        process.WaitForExit();

                        if (process.ExitCode != 0)
                        {
                            if (error.Contains("password", StringComparison.OrdinalIgnoreCase) || 
                                error.Contains("encrypted", StringComparison.OrdinalIgnoreCase))
                            {
                                result.Status = "?? Password protected";
                                result.IsPasswordProtected = true;
                            }
                            else
                            {
                                result.Status = "? Corrupted archive";
                                result.IsCorrupted = true;
                            }
                            return result;
                        }

                        var lines = output.Split('\n');
                        string? currentPath = null;
                        bool? isFolder = null;

                        foreach (var line in lines)
                        {
                            var trimmed = line.Trim();

                            if (trimmed.StartsWith("Path = "))
                            {
                                currentPath = trimmed.Substring(7).Trim();
                            }
                            else if (trimmed.StartsWith("Folder = "))
                            {
                                isFolder = trimmed.Substring(9).Trim() == "+";
                            }

                            if (currentPath != null && isFolder == true)
                            {
                                if (!currentPath.Contains('/') && !currentPath.Contains('\\'))
                                {
                                    result.FolderName = currentPath.TrimEnd('/', '\\');
                                    result.Status = "? Ready";
                                    return result;
                                }
                                
                                currentPath = null;
                                isFolder = null;
                            }
                        }

                        result.Status = "?? No root folder";
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
                }, cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                return new ScanResult
                {
                    Status = "?? Timeout (file took too long)",
                    IsCorrupted = true
                };
            }
            finally
            {
                cancellationTokenSource.Dispose();
            }
        }
    }
}
