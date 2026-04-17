using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IndustrialProcessingSystem;

AppStartupCleanup.PromptAndCleanup("job_log.txt", "reports");

SystemConfig config;
try
{
    config = SystemConfig.Load("SystemConfig.xml");
}
catch (Exception ex)
{
    Console.WriteLine($"[Fatal] Failed to load config: {ex.Message}");
    return;
}

var system = new ProcessingSystem(config);
var logger = new JobLogger("job_log.txt");

system.JobCompleted += async (_, e) =>
{
    try
    {
        Console.WriteLine($"[COMPLETED] {e.Job.Type} {e.Job.Id:N} result={e.Result}");
        await logger.LogAsync(e.Job, e.Result, "COMPLETED");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Log error] {ex.Message}");
    }
};

system.JobFailed += async (_, e) =>
{
    try
    {
        string status = e.IsAborted ? "ABORT" : "FAILED";
        Console.WriteLine($"[{status}] {e.Job.Type} {e.Job.Id:N} error={e.Error?.GetType().Name}");
        await logger.LogAsync(e.Job, -1, status);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Log error] {ex.Message}");
    }
};

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
    Console.WriteLine("\n[Main] Shutdown signal received...");
};

var producers = Enumerable
    .Range(0, config.WorkerCount)
    .Select(i => Task.Run(() => RunProducer(i, system, cts.Token)))
    .ToArray();

Console.WriteLine($"[Main] System running. {config.WorkerCount} producer threads active. Press Ctrl+C to stop.");

await Task.WhenAll(producers);
await system.ShutdownAsync();
Console.WriteLine("[Main] System shut down cleanly.");

static void RunProducer(int id, ProcessingSystem system, CancellationToken ct)
{
    var rng = new Random();

    while (!ct.IsCancellationRequested)
    {
        try
        {
            var type = rng.Next(2) == 0 ? JobType.Prime : JobType.IO;

            string payload = type == JobType.Prime
                ? $"numbers:{rng.Next(1_000, 50_001)},threads:{rng.Next(1, 5)}"
                : $"delay:{rng.Next(100, 3_001)}";

            var job    = new Job(type, payload, rng.Next(1, 6));
            var handle = system.Submit(job);

            if (handle == null)
                Console.WriteLine($"[Producer-{id}] Job rejected (queue full or duplicate)");

            Thread.Sleep(rng.Next(300, 1_500));
        }
        catch (OperationCanceledException)
        {
            break;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Producer-{id} error] {ex.Message}");
        }
    }

    Console.WriteLine($"[Producer-{id}] Stopped.");
}
