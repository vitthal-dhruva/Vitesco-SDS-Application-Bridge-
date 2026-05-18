using Hiemdall_bridge.Interface;
using System;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;


namespace Hiemdall_bridge
{
    public class ManagedLogger : ILogger
    {
        private string logDirectory;
        private string baseFileName;
        private long maxFileSize; // in bytes
        private object lockObject = new object();
        private int iconNumber;

        private DateTime _lastCleanupDate = DateTime.MinValue;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(24);
        public ManagedLogger()
        {
            logDirectory = ConfigurationManager.AppSettings["LogDirectory"];
            if(logDirectory == null)
            {
                throw new DirectoryNotFoundException($"Log Directory Path not found");
            }
            iconNumber = GetIconNumber();
            baseFileName = $"Station{iconNumber}";
            // Set max file size (10MB default)
            maxFileSize = 3 * 1024 * 1024;
        }

        
        private int GetIconNumber()
        {
            try
            {
                string iconNumberStr = ConfigurationManager.AppSettings["IconNumber"];
                if (int.TryParse(iconNumberStr, out int number))
                {
                    return number;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading IconNumber: {ex.Message}");
            }
            return 1; // Default to 1 if not found or error
        }
        public void WriteLog(string message, string level = "INFO")
        {
            lock (lockObject)
            {
                try
                {
                    string logFilePath = GetCurrentLogFilePath();
                    // ✅ Very fast check (no IO)
                    if (DateTime.Now - _lastCleanupDate > _cleanupInterval)
                    {
                        _lastCleanupDate = DateTime.Now;

                        // Run cleanup in background (no delay to logging)
                        Task.Run(() => DeleteOldLogs());
                    }
                    // Check file size and rotate if needed
                    if (File.Exists(logFilePath) && new FileInfo(logFilePath).Length > maxFileSize)
                    {
                        RotateLogFile(logFilePath);
                    }

                    string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
                    File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
                }
                catch (Exception ex)
                {

                }
            }
        }
        private void DeleteOldLogs()
        {
            try
            {
                if (!Directory.Exists(logDirectory))
                    return;

                var files = Directory.GetFiles(logDirectory, $"{baseFileName}*.log");

                DateTime cutoffDate = DateTime.Now.AddDays(-30);

                foreach (var file in files)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        //File.SetLastWriteTime(file, DateTime.Now.AddDays(-40));
                        if (fileInfo.LastWriteTime < cutoffDate)
                        {
                            fileInfo.Delete();
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteLog($"Error deleting log file {file}: {ex.Message}", "ERROR");
                    }
                }
            }
            catch (Exception ex)
            {
               
            }
        }

        private string GetCurrentLogFilePath()
        {
            string fileName = $"{baseFileName}.log";
            return Path.Combine(logDirectory, fileName);
        }

        private void RotateLogFile(string filePath)
        {
            string directory = Path.GetDirectoryName(filePath);
            string fileNameNoExt = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath);
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmssfff"); // Added time to avoid name collisions

            // Create a historical name (e.g., AppLog_20240520_Old_153022.log)
            string archivePath = Path.Combine(directory, $"{fileNameNoExt}_Old_{timestamp}{extension}");

            try
            {
                // Moves the large file out of the way. 
                // The next WriteLog call will create a brand new file with the original name.
                File.Move(filePath, archivePath);
            }
            catch (IOException)
            {
                // Fallback if the timestamp version already exists
                string uniquePath = Path.Combine(directory, $"{fileNameNoExt}_{Guid.NewGuid().ToString().Substring(0, 4)}{extension}");
                File.Move(filePath, uniquePath);
            }
        }
        public void Info(string message) => WriteLog(message, "INFO");
        public void Error(string message) => WriteLog(message, "ERROR");
        public void Warning(string message) => WriteLog(message, "WARNING");

    }
}
