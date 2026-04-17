using System;

namespace IndustrialProcessingSystem
{
    public class Job
    {
        public Guid Id { get; }
        public JobType Type { get; }
        public string Payload { get; }
        public int Priority { get; }  // manji broj = veći prioritet

        public Job(JobType type, string payload, int priority)
        {
            Id = Guid.NewGuid();
            Type = type;
            Payload = payload;
            Priority = priority;
        }


        public Job(Guid id, JobType type, string payload, int priority)
        {
            Id = id;
            Type = type;
            Payload = payload;
            Priority = priority;
        }

        public override string ToString()
            => $"Job[{Id:N}] Type={Type} Priority={Priority} Payload={Payload}";
    }
}
