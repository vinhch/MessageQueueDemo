using CHV.Infrastructure.MessageBus.RabbitMq;
using System;

namespace CHV.Infrastructure.MessageBus.PublisherConsoleTests
{
    class Program
    {
        static void Main(string[] args)
        {
            //Console.WriteLine("Hello World!");

            var uri = "amqp://test:123456@localhost:32771/test";
            var bus = new RabbitBusClient(uri, "test_fanout", "test_queue");

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
            }
            Console.ReadKey();
        }
    }
}