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
            int[] fib = new int[n + 1];
            fib[0] = 0;
            fib[1] = 1;
            for (int i = 2; i <= n; i++)
                fib[i] = fib[i - 1] + fib[i - 2];
            return fib[n];
        }
    }
}
