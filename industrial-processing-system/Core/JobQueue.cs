using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IndustrialProcessingSystem
{
    internal sealed class JobQueue
    {
        private readonly SortedSet<QueueEntry> _set;
        private readonly object _lock = new object();
        private readonly SemaphoreSlim _signal = new SemaphoreSlim(0);
        private readonly int _maxSize;

        public JobQueue(int maxSize)
        {
            _maxSize = maxSize;
            _set = new SortedSet<QueueEntry>(QueueEntry.PriorityComparer);
        }

        public bool TryEnqueue(QueueEntry entry)
        {
            lock (_lock)
            {
                if (_set.Count >= _maxSize)
                    return false;
                _set.Add(entry);
            }
            _signal.Release();
            return true;
        }

        public async Task<QueueEntry?> DequeueAsync(CancellationToken ct)
        {
            try
            {
                await _signal.WaitAsync(ct);
            }
            catch (OperationCanceledException)
            {
                return null;
            }

            lock (_lock)
            {
                if (_set.Count == 0) return null;
                var min = _set.Min!;
                _set.Remove(min);
                return min;
            }
        }

        public List<Job> GetTop(int n)
        {
            lock (_lock)
            {
                return _set.Take(n).Select(e => e.Job).ToList();
            }
        }

        public Job? Find(Guid id)
        {
            lock (_lock)
            {
                return _set.FirstOrDefault(e => e.Job.Id == id)?.Job;
            }
        }
    }
}
