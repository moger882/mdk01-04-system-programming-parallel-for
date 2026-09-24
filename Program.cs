using System;
using System.Diagnostics;
using System.Threading.Tasks;

public class StudentProgram
{
    public static void Main()
    {
        int[] numbers = new int[100000];
        Random random = new Random(20260619);

        for (int i = 0; i < numbers.Length; i++)
        {
            numbers[i] = random.Next(-10000, 10001);
        }

        // Обычный цикл
        Stopwatch timer = Stopwatch.StartNew();

        long usualSum = 0;
        int usualMin = numbers[0];
        int usualMax = numbers[0];

        for (int i = 0; i < numbers.Length; i++)
        {
            usualSum += numbers[i];

            if (numbers[i] < usualMin)
                usualMin = numbers[i];

            if (numbers[i] > usualMax)
                usualMax = numbers[i];
        }

        timer.Stop();
        double usualTime = timer.Elapsed.TotalMilliseconds;
        double usualAverage = (double)usualSum / numbers.Length;

        // Параллельный цикл
        long parallelSum = 0;
        int parallelMin = numbers[0];
        int parallelMax = numbers[0];
        object locker = new object();

        timer.Restart();

        Parallel.For(0, numbers.Length, i =>
        {
            lock (locker)
            {
                parallelSum += numbers[i];

                if (numbers[i] < parallelMin)
                    parallelMin = numbers[i];

                if (numbers[i] > parallelMax)
                    parallelMax = numbers[i];
            }
        });

        timer.Stop();
        double parallelTime = timer.Elapsed.TotalMilliseconds;
        double parallelAverage = (double)parallelSum / numbers.Length;

        Console.WriteLine("Статистика массива из 100000 элементов");
        Console.WriteLine();

        Console.WriteLine("Обычный цикл:");
        Console.WriteLine("Минимум: " + usualMin);
        Console.WriteLine("Максимум: " + usualMax);
        Console.WriteLine("Сумма: " + usualSum);
        Console.WriteLine("Среднее: " + usualAverage.ToString("F3"));
        Console.WriteLine("Время: " + usualTime.ToString("F3") + " мс");
        Console.WriteLine();

        Console.WriteLine("Parallel.For:");
        Console.WriteLine("Минимум: " + parallelMin);
        Console.WriteLine("Максимум: " + parallelMax);
        Console.WriteLine("Сумма: " + parallelSum);
        Console.WriteLine("Среднее: " + parallelAverage.ToString("F3"));
        Console.WriteLine("Время: " + parallelTime.ToString("F3") + " мс");
        Console.WriteLine();

        bool sameResult = usualSum == parallelSum &&
                          usualMin == parallelMin &&
                          usualMax == parallelMax;

        Console.WriteLine("Результаты совпадают: " + (sameResult ? "да" : "нет"));
        Console.WriteLine("Parallel.For в этом примере может быть медленнее из-за lock.");
    }
}
