using Serilog;
using System;
using System.Diagnostics;

namespace Jerry.Hook
{
    internal struct ExecTimeStats
    {
        internal TimeSpan Total = TimeSpan.Zero;
        internal TimeSpan Longest = TimeSpan.Zero;
        internal uint DataCount = 0;
        public ExecTimeStats()
        { }
        internal readonly TimeSpan Average => DataCount == 0 ? TimeSpan.Zero : Total.Divide(DataCount);

        public void Add(TimeSpan currentMeasurement)
        {
            DataCount++;
            Total += currentMeasurement;
            if (Longest < currentMeasurement)
                Longest = currentMeasurement;
        }
    }
    public class DataCollector
    {
        private readonly HookType hookType;
        private readonly ushort sessionSampleSize;
        private readonly TimeSpan abnormalExecTime;
        private ExecTimeStats SessionStats = new();
        private ExecTimeStats TotalStats = new();

        private bool collectedEnoughData => (TotalStats.DataCount % sessionSampleSize) == 0;

        public DataCollector(HookType hookType)
        {
            sessionSampleSize = hookType == HookType.MouseHook ? (ushort)1000 : (ushort)100;
            //1ms = 10.000 ticks
            abnormalExecTime = hookType == HookType.MouseHook ? TimeSpan.FromTicks(100_000) : TimeSpan.FromTicks(100_000);
            this.hookType = hookType;
        }


        public void Collect(TimeSpan currentMeasurement)
        {
            if (currentMeasurement > abnormalExecTime)
                Log.Debug("The {type} filter function execution[{count}] exceeded the expected time limit: {ms,3}ms = {Elapsed:000} ticks",
                    hookType,
                    TotalStats.DataCount,
                    currentMeasurement.Milliseconds,
                    currentMeasurement.Ticks);

            TotalStats.Add(currentMeasurement);
            SessionStats.Add(currentMeasurement);

            if (collectedEnoughData)
            {
                Log.Debug("{type} filter function stats ( Average execution time: {Elapsed:00000} ticks, sample size:{size}, worst: {longest} ticks)",
                    hookType,
                    SessionStats.Average.Ticks,
                    SessionStats.DataCount,
                    SessionStats.Longest.Ticks);

                SessionStats = new ExecTimeStats();
            }
        }

        public void LogStats()
        {
            if (TotalStats.DataCount == 0)
                return;
            var average = TotalStats.Average;
            Log.Debug("{type} session stats:  function called {C} times, avg execution time: {ms,3} ms = {Elapsed:00000} ticks; worst: {sum} ms",
                hookType,
                TotalStats.DataCount,
                average.Milliseconds,
                average.Ticks,
                TotalStats.Longest.Milliseconds);
        }
    }

    public class PerformanceStopwatch : IDisposable
    {
        private DataCollector DataCollector { get; }
        private Stopwatch Stopwatch { get; }

        public PerformanceStopwatch(DataCollector collector)
        {
            DataCollector = collector;
            Stopwatch = new();
            Stopwatch.Restart();
        }

        public void Dispose()
        {
            Stopwatch.Stop();
            DataCollector.Collect(Stopwatch.Elapsed);
        }
    }
}
