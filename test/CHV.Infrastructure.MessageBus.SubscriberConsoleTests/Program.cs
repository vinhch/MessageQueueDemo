using CHV.Infrastructure.MessageBus.RabbitMq;
using System;
using System.Threading.Tasks;

namespace CHV.Infrastructure.MessageBus.SubscriberConsoleTests
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Press any key to start...");
            Console.ReadKey();

            var uri = "amqp://test:123456@localhost:32771/test";
            var bus = new RabbitBusClient(uri, "test_fanout", "test_queue");

            bus.Subscribe<string>(async (msg) =>
            {
                await Console.Out.WriteLineAsync(msg);
                await Task.Delay(1000);
            });

            Console.ReadKey();
        }
    }
}