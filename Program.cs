using System.Diagnostics;

const int ArraySize = 100_000;
const int MinValue = -10_000;
const int MaxValue = 10_001;
const int Seed = 20260619;

int[] numbers = CreateArray(ArraySize, Seed, MinValue, MaxValue);

// Прогрев JIT уменьшает влияние первой компиляции методов на сравнение.
_ = CalculateSequential(numbers);
_ = CalculateParallel(numbers);

(Statistics sequential, double sequentialMs) = Measure(() => CalculateSequential(numbers));
(Statistics parallel, double parallelMs) = Measure(() => CalculateParallel(numbers));

Console.WriteLine("Статистика массива из 100 000 элементов");
Console.WriteLine($"Диапазон значений: {MinValue}..{MaxValue - 1}; seed: {Seed}");
Console.WriteLine();
PrintResult("Обычный цикл", sequential, sequentialMs);
PrintResult("Parallel.For", parallel, parallelMs);

bool resultsMatch = sequential == parallel;
Console.WriteLine($"Результаты совпадают: {(resultsMatch ? "да" : "нет")}");

if (!resultsMatch)
{
    Environment.ExitCode = 1;
    return;
}

double ratio = parallelMs == 0 ? 0 : sequentialMs / parallelMs;
Console.WriteLine($"Отношение времени (обычный / Parallel.For): {ratio:F2}");
Console.WriteLine("Примечание: на небольшом объёме данных Parallel.For может быть медленнее из-за накладных расходов.");

static int[] CreateArray(int size, int seed, int minValue, int maxValue)
{
    var random = new Random(seed);
    var data = new int[size];

    for (int i = 0; i < data.Length; i++)
    {
        data[i] = random.Next(minValue, maxValue);
    }

    return data;
}

static Statistics CalculateSequential(int[] data)
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

static Statistics CalculateParallel(int[] data)
{
    object mergeLock = new();
    long totalSum = 0;
    int globalMin = int.MaxValue;
    int globalMax = int.MinValue;

    Parallel.For(
        0,
        data.Length,
        () => new LocalStatistics(0, int.MaxValue, int.MinValue),
        (index, _, local) =>
        {
            int value = data[index];
            local.Sum += value;
            if (value < local.Min) local.Min = value;
            if (value > local.Max) local.Max = value;
            return local;
        },
        local =>
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

static (Statistics Result, double Milliseconds) Measure(Func<Statistics> action)
{
    var stopwatch = Stopwatch.StartNew();
    Statistics result = action();
    stopwatch.Stop();
    return (result, stopwatch.Elapsed.TotalMilliseconds);
}

static void PrintResult(string title, Statistics stats, double milliseconds)
{
    Console.WriteLine(title);
    Console.WriteLine($"  Минимум: {stats.Min}");
    Console.WriteLine($"  Максимум: {stats.Max}");
    Console.WriteLine($"  Сумма: {stats.Sum}");
    Console.WriteLine($"  Среднее: {stats.Average:F3}");
    Console.WriteLine($"  Время: {milliseconds:F3} мс");
    Console.WriteLine();
}

readonly record struct Statistics(int Count, long Sum, int Min, int Max)
{
    public double Average => Count == 0 ? 0 : (double)Sum / Count;
}

struct LocalStatistics(long sum, int min, int max)
{
    public long Sum = sum;
    public int Min = min;
    public int Max = max;
}
