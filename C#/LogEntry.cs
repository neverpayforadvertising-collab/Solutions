using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

/// Thread-Safe Real-Time Log Aggregator
/// Supports top-N queries within a sliding time window
/// </summary>
public class LogEntry
{
    public DateTime Timestamp;
    public string Level;
    public string Message;
}

public class LogAggregator
{
    private readonly ConcurrentQueue<LogEntry> _logs = new(); // Thread-safe log storage
    private readonly ConcurrentDictionary<string, int> _levelCounts = new(); // Frequency map

    /// <summary>
    /// Add log to aggregator (thread-safe)
    /// </summary>
    public void AddLog(string level, string message)
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = level,
            Message = message
        };

        _logs.Enqueue(entry); // Add to queue
        _levelCounts.AddOrUpdate(level, 1, (_, v) => v + 1); // Update frequency
    }

    /// <summary>
    /// Returns top-N log levels by frequency in the last 'window' duration
    /// Implements sliding time window by removing old logs
    /// </summary>
    public List<(string Level, int Count)> GetTopN(int n, TimeSpan window)
    {
        var cutoff = DateTime.UtcNow - window;

        // Remove logs older than window from queue and frequency map
        while (_logs.TryPeek(out var log) && log.Timestamp < cutoff)
        {
            if (_logs.TryDequeue(out var removed))
            {
                _levelCounts.AddOrUpdate(
                    removed.Level,
                    0,
                    (_, v) => Math.Max(0, v - 1)
                );
            }
        }

        // Return top N log levels
        return _levelCounts
            .Where(kv => kv.Value > 0)
            .OrderByDescending(kv => kv.Value)
            .Take(n)
            .Select(kv => (kv.Key, kv.Value))
            .ToList();
    }
}

// ===== TEST =====
class Program
{
    static void Main()
    {
        var aggregator = new LogAggregator();

        // Simulate log stream
        aggregator.AddLog("ERROR", "Failed DB call");
        aggregator.AddLog("INFO", "User logged in");
        aggregator.AddLog("ERROR", "Timeout");
        aggregator.AddLog("WARN", "Memory spike");

        var result = aggregator.GetTopN(2, TimeSpan.FromMinutes(5));

        foreach (var (level, count) in result)
        {
            Console.WriteLine($"{level}: {count}");
        }
    }
}