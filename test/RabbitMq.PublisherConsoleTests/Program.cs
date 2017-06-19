using CHV.Infrastructure.MessageBus.RabbitMq;
using System;

namespace RabbitMq.PublisherConsoleTests
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Press any key to start...");
            Console.ReadKey();

            var uri = "amqp://test:123456@localhost:32771/test";
            var bus = new RabbitBusClient(uri, "test_fanout", "fanout", "test_queue");

            var i = 0;

            //var msg = $"Message {i}";
            //Console.WriteLine(msg);
            //bus.Publish(msg);

            while (true)
            {
                i++;

                var msg = $"Message {i}";
                Console.WriteLine(msg);
                bus.Publish(msg);

                //Thread.Sleep(100);
                if (i == 100) break;
            }
        }
    }
}