using System;

namespace FibonacciApp
{
    class Program
    {
        static void Main(string[] args)
        {
            int terms = int.Parse(Console.ReadLine());

            for (int i = 0; i < terms; i++)
            {
                Console.WriteLine($""Term {i + 1}: {Fibonacci(i)}"");
            }
        }

        static int Fibonacci(int n)
        {
            if (n <= 1)
                return n;
            var fibSequence = new[] { 0, 1 };
            for (int i = 2; i <= n; i++)
                fibSequence[i] = fibSequence[i - 1] + fibSequence[i - 2];
            return fibSequence[n];
        }
    }
}
