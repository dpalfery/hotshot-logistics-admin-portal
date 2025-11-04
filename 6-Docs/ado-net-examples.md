# ADO.NET Examples

Detailed implementation examples for ADO.NET data access patterns in Hotshot Logistics.

## Connection Management

```csharp
// Correct: Proper async connection management
public async Task<IEnumerable<Customer>> GetAllAsync()
{
    var customers = new List<Customer>();

    await using var connection = new SqlConnection(_connectionString);
    await connection.OpenAsync();

    var command = connection.CreateCommand();
    command.CommandText = "SELECT Id, Name, Email FROM Customers WHERE IsActive = @IsActive";
    command.Parameters.AddWithValue("@IsActive", true);

    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        customers.Add(new Customer
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            Email = reader.GetString(2)
        });
    }

    return customers;
}
```

## Error Handling

```csharp
// Correct: Proper error handling with specific exceptions
public async Task<Customer> GetByIdAsync(int id)
{
    try
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, Email FROM Customers WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", id);

        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Customer
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Email = reader.GetString(2)
            };
        }

        throw new KeyNotFoundException($"Customer with ID {id} not found");
    }
    catch (SqlException ex)
    {
        _logger.LogError(ex, "Database error while retrieving customer {CustomerId}", id);
        throw new DataAccessException("Failed to retrieve customer", ex);
    }
}
```

## Migration Structure

```csharp
// Example migration file in 4-Persistence/HotshotLogistics.Data/Migrations/
[Migration(20250121000000)]
public class CreateCustomersTable : Migration
{
    public override void Up()
    {
        Create.Table("Customers")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("Name").AsString(255).NotNullable()
            .WithColumn("Email").AsString(255).NotNullable().Unique()
            .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime);
    }

    public override void Down()
    {
        Delete.Table("Customers");
    }
}
```

## Mock Implementation Example

```csharp
// For unit testing application services
public class MockCustomerRepository : CustomerRepository
{
    private readonly List<Customer> _customers = new();

    public Task<Customer> GetByIdAsync(int id)
    {
        var customer = _customers.FirstOrDefault(c => c.Id == id);
        return Task.FromResult(customer ?? throw new KeyNotFoundException());
    }

    // Other methods...
}