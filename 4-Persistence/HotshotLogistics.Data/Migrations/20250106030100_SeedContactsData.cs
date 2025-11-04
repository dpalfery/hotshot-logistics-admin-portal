#pragma warning disable SA1649
using FluentMigrator;

namespace HotshotLogistics.Data.Migrations;

/// <summary>
/// Seeds initial contacts data for existing customers.
/// </summary>
[Migration(20250106030100)]
public class SeedContactsData : Migration
{
    /// <summary>
    /// Applies the migration to seed contacts data.
    /// </summary>
    public override void Up()
    {
        // Seed primary contacts for the existing customers (cust-1, cust-2)
        var existingCustomerIds = new[] { "cust-1", "cust-2" };

        for (int i = 0; i < existingCustomerIds.Length; i++)
        {
            var customerId = existingCustomerIds[i];
            var contactIndex = i + 1;

            Insert.IntoTable("Contacts")
                .Row(new
                {
                    CustomerId = customerId,
                    Name = $"Primary Contact {contactIndex}",
                    Email = $"contact{contactIndex:D3}@seedtest.com",
                    Phone = $"555-30{contactIndex:D2}",
                    Title = "Operations Manager",
                    IsPrimary = true,
                    CreatedAt = DateTime.UtcNow,
                });

            // Add a secondary contact for the first customer
            if (i == 0)
            {
                Insert.IntoTable("Contacts")
                    .Row(new
                    {
                        CustomerId = customerId,
                        Name = $"Secondary Contact {contactIndex}",
                        Email = $"secondary{contactIndex:D3}@seedtest.com",
                        Phone = $"555-31{contactIndex:D2}",
                        Title = "Account Manager",
                        IsPrimary = false,
                        CreatedAt = DateTime.UtcNow,
                    });
            }
        }
    }

    /// <summary>
    /// Reverts the migration by removing seeded contacts data.
    /// </summary>
    public override void Down()
    {
        Delete.FromTable("Contacts").AllRows();
    }
}
