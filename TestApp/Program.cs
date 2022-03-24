using StackExchange.Redis;
using System;
using RedisBlue;
using System.Collections.Generic;
using System.Threading.Tasks;
using RedisBlue.Models;
using System.Linq;

namespace TestApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var multiplexer = await ConnectionMultiplexer.ConnectAsync("localhost:6379,abortConnect=False");
            var collection = IndexedCollection.Create(multiplexer.GetDatabase(), "MyData");

            //await collection.CreateItem(new Person()
            //{
            //    Partition = "tenant-1",
            //    Id = Guid.NewGuid().ToString(),
            //    Name = "Bob",
            //    Age = 30
            //});

            //await collection.CreateItem(new Person()
            //{
            //    Partition = "tenant-1",
            //    Id = Guid.NewGuid().ToString(),
            //    Name = "Alice",
            //    Age = 32
            //});

            //collection.CreateItem(new MyData()
            //{
            //    Id = "2",
            //    Partition = "abc",
            //    Name = "name2",
            //    Value1 = 150,
            //    SubData = new SubData()
            //    {
            //        MyInt = 100,
            //        MyStr = "foo"
            //    },
            //    Dict = new Dictionary<string, object>
            //    {
            //        ["k1"] = "foo",
            //        ["k2"] = 9
            //    },
            //    List1 = new List<int> { 1, 2, 3, 4 },
            //    List2 = new List<string> { "abc", "def" },
            //    List3 = new List<SubData> { new SubData("s1", 1), new SubData("s2", 2) }
            //}).Wait();

            //collection.DeleteItem<MyData>("abc", "2").Wait();
            //var res = collection.ReadItem<MyData>("abc", "1").Result;


            var query = collection
                .AsQueryable<Person>("tenant-1")
                .Where(p => p.Age > 30 && p.Age < 39)
                .OrderBy(p => p.Age);


            //query = query.Where(x => x.SubData.MyInt == 200);
            //query = query.Where(x => !(x.Value1 < 120));

            //query = query.Where(x => x.Value1 == 100 || !(x.Value1 == 150));
            //query = query.Where(x => (x.Value1 == 100 || x.Value1 == 200) && x.Name == "name2");
            //query = query.Where(x => x.SubData.MyInt == 99 && x.Value1 > 120).OrderBy(x => x.Value1);

            //query = query.OrderBy(x => x.Value1);
            //query = query.Where(x => x.Dict["k1"] == 9);
            //query = query.Where(x => x.List1[0] == 1);

            await foreach (var item in query)
            {
                Console.WriteLine(item);
            }
            //IQueryable<MyData> dat;

            //dat.Where()

            Console.WriteLine("done");
            Console.ReadLine();
        }
    }

    record Person
    {
        [Key]
        public string Id { get; set; }

        [PartitionKey]
        public string Partition { get; set; }

        [Index]
        public string Name { get; set; }

        [Index]
        public int Age { get; set; }
    }

        record MyData
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

        [Index]
        public Dictionary<string, object> Dict { get; set; }

        [Index]
        public List<int> List1 { get; set; }

        [Index]
        public List<string> List2 { get; set; }

        [Index]
        public List<SubData> List3 { get; set; }
    }

    record SubData
    {
        public SubData()
        {
        }

        public SubData(string myStr, int myInt)
        {
            MyStr = myStr;
            MyInt = myInt;
        }

        [Index]
        public string MyStr { get; set; }

        [Index]
        public int MyInt { get; set; }
    }
}
