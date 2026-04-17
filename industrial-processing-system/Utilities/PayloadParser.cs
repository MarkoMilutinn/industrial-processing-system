using System;
using System.Collections.Generic;

namespace IndustrialProcessingSystem
{

    public static class PayloadParser
    {
        public static Dictionary<string, string> Parse(string payload)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var part in payload.Split(','))
            {
                var kv = part.Trim().Split(':', 2);
                if (kv.Length == 2)
                    result[kv[0].Trim()] = kv[1].Trim();
            }

            return result;
        }

        public static int GetInt(Dictionary<string, string> parsed, string key)
        {
            return int.Parse(parsed[key].Replace("_", ""));
        }

        public static (int limit, int threads) ParsePrime(string payload)
        {
            var p = Parse(payload);
            int limit   = GetInt(p, "numbers");
            int threads = GetInt(p, "threads");

            threads = Math.Clamp(threads, 1, 8);

            return (limit, threads);
        }

        public static int ParseIO(string payload)
        {
            var p = Parse(payload);
            return GetInt(p, "delay");
        }
    }
}
