using CHV.Infrastructure.MessageBus.RabbitMq;
using System;
using System.Threading;

namespace RabbitMq.PublisherConsoleTests
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Press any key to start...");
            Console.ReadKey();

            //TestPublish();
            TestRequest();
        }

        static void TestPublish()
        {
            var uri = "amqp://test:123456@localhost:32771/test";
            var bus = new PubSubClientBus(uri, "test_fanout", "fanout", "test_queue");

            var i = 0;
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

        static void TestRequest()
        {
            var uri = "amqp://test:123456@localhost:32771/test";
            var bus = new RequestClientBus(uri, routingKey: "test_queue1");

            var i = 0;
            while (true)
            {
                i++;

                var msg = $"Request {i}";
                Console.WriteLine(msg);
                var response = bus.RequestAsync<string, string>(msg).GetAwaiter().GetResult();
                Console.WriteLine($"OK: {response}");

                Thread.Sleep(5000);
                if (i == 10) break;
            }
        }
    }
}