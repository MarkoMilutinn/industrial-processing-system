using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IndustrialProcessingSystem
{
    internal sealed class QueueEntry
    {
        public Job Job { get; }
        public TaskCompletionSource<int> Tcs { get; }
        public DateTime SubmittedAt { get; }
        public DateTime Deadline { get; }

        public QueueEntry(Job job, TaskCompletionSource<int> tcs, DateTime submittedAt)
        {
            Job = job;
            Tcs = tcs;
            SubmittedAt = submittedAt;
            Deadline = submittedAt.AddSeconds(2);
        }

        public static readonly IComparer<QueueEntry> PriorityComparer =
            Comparer<QueueEntry>.Create((a, b) =>
            {
                int c = a.Job.Priority.CompareTo(b.Job.Priority);
                if (c != 0) return c;
                c = a.SubmittedAt.CompareTo(b.SubmittedAt);
                if (c != 0) return c;
                return a.Job.Id.CompareTo(b.Job.Id);
            });
    }

    internal sealed class CompletedJobRecord
    {
        public Job Job { get; }
        public int Result { get; }
        public bool Success { get; }
        public DateTime CompletedAt { get; }
        public TimeSpan ExecutionTime { get; }

        public CompletedJobRecord(Job job, int result, bool success,
            DateTime completedAt, TimeSpan executionTime)
        {
            Job = job;
            Result = result;
            Success = success;
            CompletedAt = completedAt;
            ExecutionTime = executionTime;
        }
    }

    public sealed class JobEventArgs : EventArgs
    {
        public Job Job { get; }
        public int Result { get; }
        public bool IsAborted { get; }
        public Exception? Error { get; }

        public JobEventArgs(Job job, int result, bool isAborted, Exception? error = null)
        {
            Job = job;
            Result = result;
            IsAborted = isAborted;
            Error = error;
        }
    }
}