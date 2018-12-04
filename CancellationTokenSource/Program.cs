using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CancellationTokenSourceDemo
{
    class Program
    {
        /// <summary>
        /// 使用协作模型来取消异步操作或长时间运行的的同步操作涉及的两个对象
        /// 1、CancellationTokenSource
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            //若要处理可能的取消操作 该示例实例化CancellationTokenSource对象，它生成的取消标记传递到TaskFactory对象
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;

            Random rnd = new Random();
            object lockObj = new object();

            List<Task<int[]>> tasks = new List<Task<int[]>>();
            TaskFactory factory = new TaskFactory(token);
            for (int taskCtr = 0; taskCtr <= 10; taskCtr++)
            {
                int iteration = taskCtr + 1;
                tasks.Add(factory.StartNew(() =>
                {
                    int value;
                    int[] values = new int[10];
                    for (int ctr = 1; ctr <= 10; ctr++)
                    {
                        lock (lockObj)
                        {
                            value = rnd.Next(0, 101);
                        }
                        if (value == 0)
                        {
                            source.Cancel();
                            Console.WriteLine("cancelling at task {0}", iteration);
                            break;
                        }
                        values[ctr - 1] = value;
                    }
                    return values;
                }, token));
            }
            try
            {
                Task<double> ftask = factory.ContinueWhenAll(tasks.ToArray(), (results) =>
                {
                    Console.WriteLine("Calculating overrall mean...");
                    long sum = 0;
                    int n = 0;
                    foreach (var t in results)
                    {
                        foreach (var r in t.Result)
                        {
                            sum += r;
                            n++;
                        }
                    }
                    return sum / (double)n;
                }, token);

                Console.WriteLine("The mean is {0}", ftask.Result);
            }
            catch (AggregateException ae)
            {
                foreach (var e in ae.InnerExceptions)
                {
                    if (e is TaskCanceledException)
                    {
                        Console.WriteLine("Unable to compute mean:{0}", ((TaskCanceledException)e).Message);
                    }
                    else
                    {
                        Console.WriteLine("Exception : "+e.GetType().Name);
                    }
                }
            }
            finally
            {
                source.Dispose();
            }
            Console.Read();
        }
    }
}
