# MyORMForMySQL

MyORMForMySQL is a implementation of MyORM that uses MySQL as database. 

## Installation

.NET CLI

```bash
dotnet add package Adr.MyORMForMySQL --version 3.0.0
```

Nuget package manager

```bash
PM> Install-Package Adr.MyORMForMySQL -Version 3.0.0
```

packageReference

```bash
<PackageReference Include="Adr.MyORMForMySQL" Version="3.0.0" />
```

## Usage

**Create a instance of MySQLContext:**
```csharp
public class Context : MySQLContext
    {
        
        public Context(MySQLConnectionBuilder builder) : base(new MySQLManager(builder)) { }

        public MySQLCollection<Item> Items { get; set; }
        public MySQLCollection<Order> Orders { get; set; }
    }
```


**Using a instance of MySQLContext:**
```csharp

public class OrderService 
    {
        Data.Context _context;
        public OrderService(Data.Context context)
        {
            _context = context;
        }

        public async Task Add(Order order)
        {
            await _context.Orders.AddAsync(order);
        }

        public async Task<IEnumerable<Order>> GetAll()
        {                        

            return await _context.Orders.OrderBy(d => d.Id).Join(d => d.Item).ToListAsync();
        }

        public async Task<Order?> Find(long id)
        {
            return await _context.Orders.Where(d => d.Id == id).FirstAsync();

        }

        public async Task<IEnumerable<Order>> GetFirst10()
        {
            return await _context.Orders.Take(10);

        }
}
```

**Sample of DI and IS, so we can change database easily:**
```csharp
public class OrderService 
    {
        MyORM.Interfaces.IDBContext _context;

        
        public OrderService(MyORM.Interfaces.IDBContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Order>> GetAll()
        {
            return await _context.Collection<Order>().OrderBy(d => d.Id).Join(d => d.Item).ToListAsync();
        }
}

```

# For web

```csharp

MyORMForMySQL.Objects.MySQLConnectionBuilder myConnBuilder = 
      new MyORMForMySQL.Objects.MySQLConnectionBuilder
           (user: "<user>", password: "<pass>", port: <port>, dataBase: "<database>");

builder.Services.AddScoped<MyORM.Interfaces.IDBContext, erp.Data.Context>(
      options => {
                    return new Data.Context(myConnBuilder);
                 });

new Data.Context(myConnBuilder).UpdateDataBase();

```


## Contributing
Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

## License
[MIT](https://choosealicense.com/licenses/mit/)
