using System;
using System.IO;

namespace IndustrialProcessingSystem
{
    public static class AppStartupCleanup
    {
        public static void PromptAndCleanup(string logPath = "job_log.txt", string reportsDirectory = "reports")
        {
            Console.Write("Do you want to delete previous run artifacts (log and reports)? [y/N]: ");
            string? input = Console.ReadLine();

            if (!string.Equals(input, "y", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            TryDeleteFile(logPath, "log file");
            TryDeleteDirectory(reportsDirectory, "reports directory");
        }

        private static void TryDeleteFile(string filePath, string label)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"[Cleanup] {label} not found. Skipping.");
                    return;
                }

                File.Delete(filePath);
                Console.WriteLine($"[Cleanup] Deleted {label}: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Cleanup] Could not delete {label}: {ex.Message}");
            }
        }

        private static void TryDeleteDirectory(string directoryPath, string label)
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    Console.WriteLine($"[Cleanup] {label} not found. Skipping.");
                    return;
                }

                Directory.Delete(directoryPath, recursive: true);
                Console.WriteLine($"[Cleanup] Deleted {label}: {directoryPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Cleanup] Could not delete {label}: {ex.Message}");
            }
        }
    }
}
