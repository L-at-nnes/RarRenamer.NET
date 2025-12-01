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

        public static async Task<ScanResult> ScanArchiveAsync(string filePath, int timeoutSeconds = 60)
        {
            if (_sevenZipPath == null)
            {
                return new ScanResult
                {
                    Status = "? 7-Zip not found",
                    IsCorrupted = true
                };
            }

            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            
            try
            {
                return await Task.Run(async () =>
                {
                    var result = new ScanResult();
                    Process? process = null;

                    try
                    {
                        process = new Process
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
                        
                        // Lecture optimisée ligne par ligne au lieu de tout charger en mémoire
                        var outputLines = new List<string>();
                        string? line;
                        while ((line = await process.StandardOutput.ReadLineAsync()) != null)
                        {
                            outputLines.Add(line);
                        }
                        
                        string error = await process.StandardError.ReadToEndAsync();
                        
                        await process.WaitForExitAsync(cancellationTokenSource.Token);

                        if (process.ExitCode != 0)
                        {
                            if (error.Contains("password", StringComparison.OrdinalIgnoreCase) || 
                                error.Contains("encrypted", StringComparison.OrdinalIgnoreCase) ||
                                error.Contains("wrong password", StringComparison.OrdinalIgnoreCase))
                            {
                                result.Status = "?? Password protected";
                                result.IsPasswordProtected = true;
                            }
                            else if (error.Contains("CRC Failed", StringComparison.OrdinalIgnoreCase) ||
                                     error.Contains("Data Error", StringComparison.OrdinalIgnoreCase))
                            {
                                result.Status = "? Corrupted (CRC/Data error)";
                                result.IsCorrupted = true;
                            }
                            else
                            {
                                result.Status = "? Archive error";
                                result.IsCorrupted = true;
                            }
                            return result;
                        }

                        string? currentPath = null;
                        bool? isFolder = null;

                        foreach (var outputLine in outputLines)
                        {
                            var trimmed = outputLine.Trim();

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
                    finally
                    {
                        if (process != null && !process.HasExited)
                        {
                            try
                            {
                                process.Kill(true);
                            }
                            catch { }
                        }
                        process?.Dispose();
                    }

                    return result;
                }, cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                return new ScanResult
                {
                    Status = $"?? Timeout (>{timeoutSeconds}s)",
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
