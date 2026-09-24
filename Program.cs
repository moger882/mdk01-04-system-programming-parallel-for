using System;
using System.Diagnostics;
using System.Threading.Tasks;

internal static class Program
{
    private const int ArraySize = 100000;
    private const int MinValue = -10000;
    private const int MaxValueExclusive = 10001;
    private const int Seed = 20260619;

    private static int Main()
    {
        int[] numbers = CreateArray(ArraySize, Seed, MinValue, MaxValueExclusive);
        CalculateSequential(numbers);
        CalculateParallel(numbers);

        TimedResult sequential = Measure(delegate { return CalculateSequential(numbers); });
        TimedResult parallel = Measure(delegate { return CalculateParallel(numbers); });

        Console.WriteLine("Статистика массива из 100 000 элементов");
        Console.WriteLine("Диапазон значений: {0}..{1}; seed: {2}", MinValue, MaxValueExclusive - 1, Seed);
        Console.WriteLine();
        PrintResult("Обычный цикл", sequential);
        PrintResult("Parallel.For", parallel);

        bool resultsMatch = sequential.Statistics.Equals(parallel.Statistics);
        Console.WriteLine("Результаты совпадают: {0}", resultsMatch ? "да" : "нет");
        if (!resultsMatch) return 1;

        double ratio = parallel.Milliseconds == 0 ? 0 : sequential.Milliseconds / parallel.Milliseconds;
        Console.WriteLine("Отношение времени (обычный / Parallel.For): {0:F2}", ratio);
        Console.WriteLine("Примечание: Parallel.For может быть медленнее из-за накладных расходов.");
        return 0;
    }

    private static int[] CreateArray(int size, int seed, int minValue, int maxValue)
    {
        var random = new Random(seed);
        var data = new int[size];
        for (int i = 0; i < data.Length; i++) data[i] = random.Next(minValue, maxValue);
        return data;
    }

    private static Statistics CalculateSequential(int[] data)
    {
        long sum = 0;
        int min = int.MaxValue;
        int max = int.MinValue;
        for (int i = 0; i < data.Length; i++)
        {
            int value = data[i];
            sum += value;
            if (value < min) min = value;
            if (value > max) max = value;
        }
        return new Statistics(data.Length, sum, min, max);
    }

    private static Statistics CalculateParallel(int[] data)
    {
        object mergeLock = new object();
        long totalSum = 0;
        int globalMin = int.MaxValue;
        int globalMax = int.MinValue;

        Parallel.For(
            0,
            data.Length,
            delegate { return new LocalStatistics(0, int.MaxValue, int.MinValue); },
            delegate(int index, ParallelLoopState state, LocalStatistics local)
            {
                int value = data[index];
                local.Sum += value;
                if (value < local.Min) local.Min = value;
                if (value > local.Max) local.Max = value;
                return local;
            },
            delegate(LocalStatistics local)
            {
                lock (mergeLock)
                {
                    totalSum += local.Sum;
                    if (local.Min < globalMin) globalMin = local.Min;
                    if (local.Max > globalMax) globalMax = local.Max;
                }
            });

        return new Statistics(data.Length, totalSum, globalMin, globalMax);
    }

    private static TimedResult Measure(Func<Statistics> action)
    {
        var stopwatch = Stopwatch.StartNew();
        Statistics result = action();
        stopwatch.Stop();
        return new TimedResult(result, stopwatch.Elapsed.TotalMilliseconds);
    }

    private static void PrintResult(string title, TimedResult result)
    {
        Statistics stats = result.Statistics;
        Console.WriteLine(title);
        Console.WriteLine("  Минимум: {0}", stats.Min);
        Console.WriteLine("  Максимум: {0}", stats.Max);
        Console.WriteLine("  Сумма: {0}", stats.Sum);
        Console.WriteLine("  Среднее: {0:F3}", stats.Average);
        Console.WriteLine("  Время: {0:F3} мс", result.Milliseconds);
        Console.WriteLine();
    }

    private struct Statistics : IEquatable<Statistics>
    {
        public Statistics(int count, long sum, int min, int max)
        { Count = count; Sum = sum; Min = min; Max = max; }
        public readonly int Count;
        public readonly long Sum;
        public readonly int Min;
        public readonly int Max;
        public double Average { get { return Count == 0 ? 0 : (double)Sum / Count; } }
        public bool Equals(Statistics other)
        { return Count == other.Count && Sum == other.Sum && Min == other.Min && Max == other.Max; }
    }

    private struct LocalStatistics
    {
        public LocalStatistics(long sum, int min, int max)
        { Sum = sum; Min = min; Max = max; }
        public long Sum;
        public int Min;
        public int Max;
    }

    private struct TimedResult
    {
        public TimedResult(Statistics statistics, double milliseconds)
        { Statistics = statistics; Milliseconds = milliseconds; }
        public readonly Statistics Statistics;
        public readonly double Milliseconds;
    }
}
