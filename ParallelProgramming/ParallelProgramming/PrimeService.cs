using ParallelProgramming.ViewModels;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ParallelProgramming
{
    class PrimeService
    {
        public IEnumerable<int> GetPrimeNumbers(int min, int max)
        {
            // dont add debug-writelines to async-code, because it synchronizes the threads
            // same as Console.WriteLine
            // Debug.WriteLine("GetPrimeNumbers(start): " + Thread.CurrentThread.ManagedThreadId);
           
            // Debug.WriteLine("GetPrimeNumbers-Task(start): " + Thread.CurrentThread.ManagedThreadId);
            var primeList = new List<int>();
            for (int i = min; i <= max; i++)
            {
                if (IsPrime(i))
                {
                    primeList.Add(i);
                }

            }
            // thread context may differ from start
            // Debug.WriteLine("GetPrimeNumbers-Task(end): " + Thread.CurrentThread.ManagedThreadId);
            return primeList;

        }
        public async Task<IEnumerable<int>> GetPrimeNumbersAsync(int min, int max)
        {
            // dont add debug-writelines to code, because it synchronizes all threads
            // same as Console.WriteLine
            // Debug.WriteLine("GetPrimeNumbers(start): " + Thread.CurrentThread.ManagedThreadId);
            var res = await Task.Run<List<int>>(() =>
            {
                // Debug.WriteLine("GetPrimeNumbers-Task(start): " + Thread.CurrentThread.ManagedThreadId);
                var primeList = new List<int>();
                for (int i = min; i <= max; i++)
                {
                    if (IsPrime(i))
                    {
                        primeList.Add(i);
                    }

                }
                // thread context may differ from start
                // Debug.WriteLine("GetPrimeNumbers-Task(end): " + Thread.CurrentThread.ManagedThreadId);
                return primeList;
            });
            // same thread context as start
            //Debug.WriteLine("GetPrimeNumbers(end): " + Thread.CurrentThread.ManagedThreadId);
            return res;
        }
        public async Task<IEnumerable<int>> GetPrimeNumbersAsync2(int min, int max)
        {
            var primeList = new List<int>();
            var taskList = new List<Task>();
            for (int i = min; i <= max; i++)
            {
                var task = Task.Run(() =>
                {
                    if (IsPrime(i))
                    {
                        primeList.Add(i); // not thread safe (wrong!)
                    }
                });
                taskList.Add(task);
            }
            await Task.WhenAll(taskList);
            return primeList;

        }
        public IEnumerable<int> GetPrimeNumbersParallel(int min, int max)
        {
            // wrong solution with list
            // var primeList = new List<int>();
            
            // correct solution when using synchronised Bag
            var primeList = new ConcurrentBag<int>();
            Parallel.For(min, max + 1, (i) =>
               {
                   // thread context may vary
                   if (IsPrime(i))
                   {
                       primeList.Add(i);
                   }

               });
            return primeList;
        }

        private Task<bool> IsPrimeAsync(int number)
        {
            // NO!!! Don't do that!
            return Task.Run(() =>
            {
                return IsPrime(number);
            });
        }

        private bool IsPrime(int number)
        {
            int factor = number / 2;
            if (factor == 0)
                return true;
            while (number % factor != 0)
            {
                factor--;
            }
            if (factor == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

    }
}
