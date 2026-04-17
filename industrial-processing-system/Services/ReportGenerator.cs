using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace IndustrialProcessingSystem
{
    internal sealed class ReportGenerator
    {
        private readonly ConcurrentBag<CompletedJobRecord> _jobs;
        private readonly string _reportDir;
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);
        private int _index = 0;

        public ReportGenerator(ConcurrentBag<CompletedJobRecord> jobs, string reportDir)
        {
            _jobs = jobs;
            _reportDir = reportDir;
            Directory.CreateDirectory(reportDir);
        }

        public async Task GenerateAsync()
        {
            var snapshot = _jobs.ToList();

            var countByType = snapshot
                .GroupBy(r => r.Job.Type)
                .Select(g => new { Type = g.Key, Count = g.Count(r => r.Success) });

            var avgByType = snapshot
                .GroupBy(r => r.Job.Type)
                .Select(g => new
                {
                    Type = g.Key,
                    AvgMs = g.Average(r => r.ExecutionTime.TotalMilliseconds)
                });

            var failedByType = snapshot
                .Where(r => !r.Success)
                .GroupBy(r => r.Job.Type)
                .OrderBy(g => g.Key.ToString())
                .Select(g => new { Type = g.Key, FailCount = g.Count() });

            var xml = new XElement("Report",
                new XAttribute("generated", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                new XAttribute("totalRecords", snapshot.Count),
                new XElement("CompletedByType",
                    countByType.Select(x =>
                        new XElement("Entry",
                            new XAttribute("type", x.Type),
                            new XAttribute("count", x.Count)))),
                new XElement("AverageExecutionMs",
                    avgByType.Select(x =>
                        new XElement("Entry",
                            new XAttribute("type", x.Type),
                            new XAttribute("avgMs", x.AvgMs.ToString("F2"))))),
                new XElement("FailedByType",
                    failedByType.Select(x =>
                        new XElement("Entry",
                            new XAttribute("type", x.Type),
                            new XAttribute("count", x.FailCount))))
            );

            await _writeLock.WaitAsync();
            try
            {
                int slot = _index % 10;
                _index++;
                string path = Path.Combine(_reportDir, $"report_{slot:D2}.xml");
                await File.WriteAllTextAsync(path, xml.ToString());
                Console.WriteLine($"[Report] Written to {path}");
            }
            finally
            {
                _writeLock.Release();
            }
        }
    }
}
