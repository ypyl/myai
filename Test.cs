public class Fibonacci
{
    public int N { get; set; }

    public Fibonacci(int n)
    {
        N = n;
    }

    public int GetFibonacciNumber(int n)
    {
        if (n <= 1)
        {
            return n;
        }
        else
        {
            int a = 0;
            int b = 1;
            int result = 0;

            for (int i = 2; i <= n; i++)
            {
                result = a + b;
                a = b;
                b = result;
            }

            return result;
        }
    }

    public void PrintFibonacciSequence()
    {
        for (int i = 0; i <= N; i++)
        {
            Console.WriteLine(GetFibonacciNumber(i));
        }
    }
}