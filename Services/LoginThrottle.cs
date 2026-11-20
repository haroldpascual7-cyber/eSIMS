using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace SmartSchoolIncidentManagementSystem.Services
{
    /// <summary>
    /// Thread-safe in-memory login brute-force throttle.
    /// Tracks consecutive failure counts in a 5-minute rolling window and enforces a 5-minute lockout.
    /// </summary>
    public static class LoginThrottle
    {
        private static readonly ConcurrentDictionary<string, List<DateTime>> _failures = new(StringComparer.OrdinalIgnoreCase);
        private static readonly object _syncRoot = new();

        public const int MaxFailures = 5;
        public static readonly TimeSpan WindowDuration = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(5);

        public static bool IsLockedOut(string key, out TimeSpan remaining)
        {
            remaining = TimeSpan.Zero;
            if (string.IsNullOrWhiteSpace(key)) return false;

            DateTime now = DateTime.UtcNow;

            lock (_syncRoot)
            {
                if (!_failures.TryGetValue(key, out var attempts) || attempts.Count < MaxFailures)
                {
                    return false;
                }

                // Remove timestamps older than (WindowDuration + LockoutDuration)
                attempts.RemoveAll(t => now - t > (WindowDuration + LockoutDuration));

                if (attempts.Count < MaxFailures)
                {
                    return false;
                }

                // The lockout triggers on the 5th failure and lasts LockoutDuration from that 5th failure
                DateTime lockoutStartTime = attempts[MaxFailures - 1];
                TimeSpan elapsed = now - lockoutStartTime;

                if (elapsed < LockoutDuration)
                {
                    remaining = LockoutDuration - elapsed;
                    return true;
                }

                // Lockout period has elapsed: reset failures for this key
                _failures.TryRemove(key, out _);
                return false;
            }
        }

        public static void RecordFailure(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return;

            DateTime now = DateTime.UtcNow;

            lock (_syncRoot)
            {
                var attempts = _failures.GetOrAdd(key, _ => new List<DateTime>());
                attempts.RemoveAll(t => now - t > (WindowDuration + LockoutDuration));
                attempts.Add(now);
            }
        }

        public static void Reset(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return;

            lock (_syncRoot)
            {
                _failures.TryRemove(key, out _);
            }
        }
    }
}
