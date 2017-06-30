using CHV.Infrastructure.MessageBus.RabbitMq;
using System;
using System.Threading.Tasks;

namespace RabbitMq.SubscriberConsoleTests
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Press any key to start...");
            Console.ReadKey();

            //TestSubscribe();
            TestRespond();

            Console.ReadKey();
        }

        static void TestSubscribe()
        {
            var uri = "amqp://test:123456@localhost:32771/test";
            var bus = new PubSubClientBus(uri, "test_fanout", "fanout", "test_queue");

            bus.Subscribe<string>(async (msg) =>
            {
                await Console.Out.WriteLineAsync(msg);
                await Task.Delay(100);
            });
        }

        static void TestRespond()
        {
            var uri = "amqp://test:123456@localhost:32771/test";
            var bus = new RespondClientBus(uri, queueName: "test_queue1");

            bus.RespondAsync<string, string>(async (msg) =>
            {
                await Console.Out.WriteLineAsync(msg);
                await Console.Out.WriteLineAsync($"Respond to {msg}");
                return $"Respond to {msg}";
            }).GetAwaiter().GetResult();
        }
    }
}