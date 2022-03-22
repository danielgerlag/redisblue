using StackExchange.Redis;
using System;
using RedisBlue;
using System.Collections.Generic;
using System.Threading.Tasks;
using RedisBlue.Models;

namespace TestApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var multiplexer = ConnectionMultiplexer.ConnectAsync("localhost:6379,abortConnect=False").Result;

            var collection = IndexedCollection.Create(multiplexer.GetDatabase(), "MyData");

            //collection.CreateItem(new MyData()
            //{
            //    Id = "3",
            //    Partition = "abc",
            //    Name = "name3",
            //    Value1 = 200,
            //    SubData = new SubData()
            //    {
            //        MyInt = 99,
            //        MyStr = "foo"
            //    },
            //    Dict = new Dictionary<string, object>
            //    {
            //        ["k1"] = "foo",
            //        ["k2"] = 9
            //    },
            //    List1 = new List<int> { 1, 2, 3 },
            //    List2 = new List<string> { "abc", "def" },
            //    List3 = new List<SubData> { new SubData("s1", 1), new SubData("s2", 2) }
            //}).Wait();

            //collection.DeleteItem<MyData>("abc", "2").Wait();
            //var res = collection.ReadItem<MyData>("abc", "1").Result;

            var o1 = new ComparisonOperand()
            {
                Left = new MemberOperand() { Path = "Value1" },
                Operator = ComparisonOperator.LessThan,
                Right = new ConstantOperand() { Value = 500 }
            };

            var o2 = new ComparisonOperand()
            {
                Left = new MemberOperand() { Path = "Value1" },
                Operator = ComparisonOperator.GreaterThan,
                Right = new ConstantOperand() { Value = 50 }
            };

            var k = await collection.Query("abc", new LogicalOperand()
            {
                Operator = LogicalOperator.And,
                Operands = new List<Operand>() { o1, o2 }
            });

            Console.WriteLine(k);
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

        [Index]
        public Dictionary<string, object> Dict { get; set; }

        [Index]
        public List<int> List1 { get; set; }

        [Index]
        public List<string> List2 { get; set; }

        [Index]
        public List<SubData> List3 { get; set; }
    }

    class SubData
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
