using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IndustrialProcessingSystem
{
    public sealed class ProcessingSystem
    {
        private readonly JobQueue _queue;
        private readonly SemaphoreSlim _threadPool;
        private readonly ConcurrentDictionary<Guid, byte> _seenIds = new ConcurrentDictionary<Guid, byte>();
        private readonly ConcurrentBag<CompletedJobRecord> _completedJobs = new ConcurrentBag<CompletedJobRecord>();
        private readonly ReportGenerator _reportGen;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly Task _dispatchTask;
        private readonly Timer _reportTimer;

        public event EventHandler<JobEventArgs>? JobCompleted;
        public event EventHandler<JobEventArgs>? JobFailed;

        public ProcessingSystem(SystemConfig config)
        {
            _queue = new JobQueue(config.MaxQueueSize);
            _threadPool = new SemaphoreSlim(config.WorkerCount, config.WorkerCount);
            _reportGen = new ReportGenerator(_completedJobs, "reports");
            _dispatchTask = Task.Run(DispatchLoopAsync);
            _reportTimer = new Timer(
                _ => Task.Run(TryGenerateReportAsync),
                null,
                TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(1));

            foreach (var job in config.InitialJobs)
                Submit(job);
        }

        public JobHandle? Submit(Job job)
        {
            if (!_seenIds.TryAdd(job.Id, 0))
                return null;

            var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var entry = new QueueEntry(job, tcs, DateTime.UtcNow);

            if (!_queue.TryEnqueue(entry))
            {
                _seenIds.TryRemove(job.Id, out _);
                return null;
            }

            return new JobHandle(job.Id, tcs.Task);
        }

        public IEnumerable<Job> GetTopJobs(int n) => _queue.GetTop(n);

        public Job? GetJob(Guid id)
        {
            return _queue.Find(id)
                ?? _completedJobs.FirstOrDefault(r => r.Job.Id == id)?.Job;
        }

        public async Task ShutdownAsync()
        {
            _cts.Cancel();
            _reportTimer.Dispose();
            await _dispatchTask;
        }

        private async Task DispatchLoopAsync()
        {
            while (true)
            {
                var entry = await _queue.DequeueAsync(_cts.Token);
                if (entry == null) break;

                int requiredThreads = GetRequiredThreads(entry.Job);
                bool acquired = await TryAcquireThreadsAsync(requiredThreads, _cts.Token);

                if (!acquired) break;

                _ = Task.Run(() => ExecuteWithRetryAsync(entry, requiredThreads));
            }
        }

        private async Task<bool> TryAcquireThreadsAsync(int n, CancellationToken ct)
        {
            for (int i = 0; i < n; i++)
            {
                try
                {
                    await _threadPool.WaitAsync(ct);
                }
                catch (OperationCanceledException)
                {
                    if (i > 0) _threadPool.Release(i);
                    return false;
                }
            }
            return true;
        }

        private async Task ExecuteWithRetryAsync(QueueEntry entry, int acquiredThreads)
        {
            const int maxAttempts = 3;

            try
            {
                for (int attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    // Deadline se racuna od trenutka submita; ovde racunamo preostalo vreme.
                    TimeSpan remaining = entry.Deadline - DateTime.UtcNow;

                    if (remaining <= TimeSpan.Zero)
                    {
                        // Ako deadline istekne pre starta attempt-a, retry-ujemo 
                        // Emitujemo i brojimo samo finalni neuspeh
                        if (attempt == maxAttempts)
                        {
                            var timeoutEx = new TimeoutException("Job deadline expired before execution.");
                            _completedJobs.Add(new CompletedJobRecord(
                                entry.Job,
                                -1,
                                false,
                                DateTime.UtcNow,
                                DateTime.UtcNow - entry.SubmittedAt));
                            JobFailed?.Invoke(this, new JobEventArgs(entry.Job, -1, isAborted: true, timeoutEx));
                            entry.Tcs.TrySetException(
                                new OperationCanceledException("Job aborted after 3 failures."));
                            return;
                        }

                        continue;
                    }

                    using var timeoutCts = new CancellationTokenSource(remaining);
                    var sw = Stopwatch.StartNew();

                    try
                    {
                        int result = await JobExecutor.RunAsync(
                            entry.Job, acquiredThreads, timeoutCts.Token);
                        sw.Stop();

                        _completedJobs.Add(new CompletedJobRecord(
                            entry.Job, result, true, DateTime.UtcNow, sw.Elapsed));

                        entry.Tcs.TrySetResult(result);
                        JobCompleted?.Invoke(this, new JobEventArgs(entry.Job, result, isAborted: false));
                        return;
                    }
                    catch (Exception ex)
                    {
                        sw.Stop();
                        if (attempt == maxAttempts)
                        {
                            _completedJobs.Add(new CompletedJobRecord(
                                entry.Job, -1, false, DateTime.UtcNow, sw.Elapsed));
                            JobFailed?.Invoke(this, new JobEventArgs(entry.Job, -1, isAborted: true, ex));
                            entry.Tcs.TrySetException(
                                new OperationCanceledException("Job aborted after 3 failures."));
                            return;
                        }
                    }
                }
            }
            finally
            {
                _threadPool.Release(acquiredThreads);
            }
        }

        private static int GetRequiredThreads(Job job) =>
            job.Type == JobType.Prime
                ? PayloadParser.ParsePrime(job.Payload).threads
                : 1;

        private async Task TryGenerateReportAsync()
        {
            try { await _reportGen.GenerateAsync(); }
            catch (Exception ex) { Console.WriteLine($"[Report error] {ex.Message}"); }
        }
    }
}