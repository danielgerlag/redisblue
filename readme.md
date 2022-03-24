# RedisBlue

RedisBlue is a .NET library that transforms Redis into a queryable document store. 
It does this by providing create/read/update/delete operations that automatically index your data into sorted sets within Redis.  Then you can use Linq to query your data.

## Prerequisites 

- Redis >= 6.2
- .NET >= 5.0

## Installation

```powershell
dotnet add package RedisBlue
```

## Getting Started

Add `RedisBlue` and `StackExchange.Redis` to your usings.

```c#
using RedisBlue;
using StackExchange.Redis;
```

Decorate any fields on your data class that you wish to query on with with the `Index` attribute.
You will also need to decorate one field with a `PartitionKey` attribute, this value will determine the cluster slot when your Redis deployment has clustering enabled.
You will also need to decorate one field with a `Key` attribute, this value must be unique per partition key. The combination of the key and partition key forms a unique identity for an item.

Numeric types support both range (greater than / less than) and equality (equal / not equal) queries, where non-numeric types only support equality queries.

```c#
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
```

An `IndexedCollection` defines a named container of items that can be stored and queried.
Create an instance of `IndexedCollection` with the static `Create` method, providing a Redis database and a name for your collection.

```c#
var multiplexer = await ConnectionMultiplexer.ConnectAsync("localhost:6379,abortConnect=False");

var collection = IndexedCollection.Create(multiplexer.GetDatabase(), "People");
```

Use the `CreateItem` method on your collection to stores some items.

```c#
await collection.CreateItem(new Person()
{
    Partition = "tenant-1",
    Id = Guid.NewGuid().ToString(),
    Name = "Bob",
    Age = 30
});

await collection.CreateItem(new Person()
{
    Partition = "tenant-1",
    Id = Guid.NewGuid().ToString(),
    Name = "Alice",
    Age = 32
});
```

Use the `AsQueryable` method on your collection to access the Linq query API.  The `AsQueryable` method requires you to specify the partition in which to execute the query, cross-partition queries are not possible.

```c#
var query = collection
    .AsQueryable<Person>("tenant-1")
    .Where(p => p.Age > 30 && p.Age < 39)
    .OrderBy(p => p.Age);

await foreach (var item in query)
{
    Console.WriteLine(item);
}
```

## Supported expressions

The following expressions are supported
- Where
- OrderBy
- OrderByDescending
- Member access of child objects, dictionaries and collections by index
- And
- Or
- Not
- Equal
- Not Equal
- Greater Than
- Greater Than or equal
- Less Than
- Less Than or equal

The following expressions are on the roadmap
- Min
- Max
- Count
- First
- Last