using System;
using System.Threading;

namespace MemoryCacheSample
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                CacheSample cacher = new CacheSample();

                cacher.GetFileContents();
                cacher.GetSQLContents();

                Environment.Exit(0);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Thread.Sleep(3000);
                Environment.Exit(1);
            }

        }
    }
}
