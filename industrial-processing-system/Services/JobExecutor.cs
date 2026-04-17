using System;
using System.Threading;
using System.Threading.Tasks;

namespace IndustrialProcessingSystem
{
    internal static class JobExecutor
    {
        private static readonly Random _rng = new Random();

        public static Task<int> RunAsync(Job job, int threadCount, CancellationToken ct)
        {
            return job.Type switch
            {
                JobType.Prime => RunPrimeAsync(job.Payload, threadCount, ct),
                JobType.IO    => RunIOAsync(job.Payload, ct),
                _             => throw new InvalidOperationException($"Unknown job type: {job.Type}")
            };
        }

        private static Task<int> RunPrimeAsync(string payload, int threadCount, CancellationToken ct)
        {
            var (limit, _) = PayloadParser.ParsePrime(payload);

            return Task.Run(() =>
            {
                int count = 0;
                var opts = new ParallelOptions
                {
                    MaxDegreeOfParallelism = threadCount,
                    CancellationToken = ct
                };

                Parallel.For(2, limit + 1, opts, (n, _) =>
                {
                    if (IsPrime(n))
                        Interlocked.Increment(ref count);
                });

                return count;
            }, ct);
        }

        private static async Task<int> RunIOAsync(string payload, CancellationToken ct)
        {
            int delayMs = PayloadParser.ParseIO(payload);
            await Task.Delay(delayMs, ct);
            lock (_rng)
            {
                return _rng.Next(0, 101);
            }
        }

        private static bool IsPrime(int n)
        {
            if (n < 2) return false;
            if (n == 2) return true;
            if (n % 2 == 0) return false;
            for (int i = 3; (long)i * i <= n; i += 2)
                if (n % i == 0) return false;
            return true;
        }
    }
}
