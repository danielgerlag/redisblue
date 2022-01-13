using StackExchange.Redis;
using System;
using RedisBlue;

namespace TestApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var multiplexer = ConnectionMultiplexer.ConnectAsync("localhost:6379,abortConnect=False").Result;

            var collection = new IndexedCollection(multiplexer, "MyData");

            collection.ReplaceItem(new MyData()
            {
                Id = "1",
                Partition = "abc",
                Name = "name1",
                Value1 = 7,
                SubData = new SubData()
                {
                    MyInt = 1,
                    MyStr= "foo"
                }
            }).Wait();

            //collection.DeleteItem<MyData>("abc", "1").Wait();

            Console.WriteLine("done");
            Console.ReadLine();
        }
    }

    class MyData
    {
        [Key]
        public string Id { get; set; }

        [PartitionKey]
        public string Partition { get; set; }
        
        [Index]
        public string Name { get; set; }

        [Index]
        public int Value1 { get; set; }

        [Index]
        public SubData SubData { get; set; }
    }

    class SubData
    {
        [Index]
        public string MyStr { get; set; }

        [Index]
        public int MyInt { get; set; }
    }
}
