using System;
using System.IO;
using System.Linq;

namespace SpeechToTextTray.Core.Helpers
{
    /// <summary>
    /// Manager for temporary audio files
    /// </summary>
    public class TempFileManager
    {
        private readonly string _tempDirectory;
        private const int MaxTempFiles = 10; // Keep only last 10 recordings

        public TempFileManager()
        {
            _tempDirectory = Path.Combine(
                Path.GetTempPath(),
                "SpeechToTextTray"
            );

            // Ensure directory exists
            Directory.CreateDirectory(_tempDirectory);
        }

        /// <summary>
        /// Get the temp directory path
        /// </summary>
        public string TempDirectory => _tempDirectory;

        /// <summary>
        /// Create a new temporary WAV file path with unique name
        /// </summary>
        public string CreateTempFilePath(string extension = ".wav")
        {
            string fileName = $"recording_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}{extension}";
            return Path.Combine(_tempDirectory, fileName);
        }

        /// <summary>
        /// Delete a specific temp file
        /// </summary>
        public bool DeleteFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting temp file: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Clean up old temporary files, keeping only the most recent ones
        /// </summary>
        public int CleanupOldFiles()
        {
            try
            {
                var files = Directory.GetFiles(_tempDirectory, "recording_*.*")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .ToList();

                int deletedCount = 0;

                // Keep only the most recent files
                foreach (var file in files.Skip(MaxTempFiles))
                {
                    try
                    {
                        file.Delete();
                        deletedCount++;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error deleting {file.Name}: {ex.Message}");
                    }
                }

                return deletedCount;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during cleanup: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Delete all temporary files
        /// </summary>
        public int CleanupAllFiles()
        {
            try
            {
                var files = Directory.GetFiles(_tempDirectory, "recording_*.*");
                int deletedCount = 0;

                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                        deletedCount++;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error deleting {file}: {ex.Message}");
                    }
                }

                return deletedCount;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during cleanup: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Get the total size of all temp files in bytes
        /// </summary>
        public long GetTotalSize()
        {
            try
            {
                return Directory.GetFiles(_tempDirectory, "recording_*.*")
                    .Select(f => new FileInfo(f))
                    .Sum(f => f.Length);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Get count of temp files
        /// </summary>
        public int GetFileCount()
        {
            try
            {
                return Directory.GetFiles(_tempDirectory, "recording_*.*").Length;
            }
            catch
            {
                return 0;
            }
        }
    }
}
