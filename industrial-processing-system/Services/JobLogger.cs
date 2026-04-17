using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IndustrialProcessingSystem
{
    public sealed class JobLogger
    {
        private readonly string _logPath;
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);

        public JobLogger(string logPath = "job_log.txt")
        {
            _logPath = logPath;
        }

        public async Task LogAsync(Job job, int result, string status)
        {
            string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{status}] {job.Id}, {result}";

            await _writeLock.WaitAsync();
            try
            {
                await File.AppendAllTextAsync(_logPath, line + Environment.NewLine);
            }
            finally
            {
                _writeLock.Release();
            }
        }
    }
}
