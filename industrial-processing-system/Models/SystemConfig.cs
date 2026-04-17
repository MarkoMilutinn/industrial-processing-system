using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace IndustrialProcessingSystem
{

    public class SystemConfig
    {
        public int WorkerCount { get; private set; }
        public int MaxQueueSize { get; private set; }
        public List<Job> InitialJobs { get; private set; }

        private SystemConfig() { }

        public static SystemConfig Load(string xmlPath)
        {
            var doc = XElement.Load(xmlPath);

            var config = new SystemConfig
            {
                WorkerCount  = int.Parse(doc.Element("WorkerCount")!.Value),
                MaxQueueSize = int.Parse(doc.Element("MaxQueueSize")!.Value),
                InitialJobs  = new List<Job>()
            };

            foreach (var jobEl in doc.Element("Jobs")!.Elements("Job"))
            {
                var type     = (JobType)Enum.Parse(typeof(JobType), jobEl.Attribute("Type")!.Value, ignoreCase: true);
                var payload  = jobEl.Attribute("Payload")!.Value;
                var priority = int.Parse(jobEl.Attribute("Priority")!.Value);

                config.InitialJobs.Add(new Job(type, payload, priority));
            }

            return config;
        }
    }
}
