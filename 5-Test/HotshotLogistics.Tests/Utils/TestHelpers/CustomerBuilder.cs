namespace HotshotLogistics.Tests.TestHelpers
{
    using System;
    using System.Collections.Generic;
    using HotshotLogistics.Domain.Entities;
    using HotshotLogistics.Domain.ValueObjects;

    /// <summary>
    /// Test builder to create concrete Customer instances for unit tests.
    /// Centralizes defaults so tests only change fields they care about.
    /// </summary>
    public class CustomerBuilder
    {
        private readonly Customer customer;

        private CustomerBuilder()
        {
            customer = new Customer
            {
                Id = Guid.NewGuid().ToString(),
                CompanyName = "Test Company",
                IsActive = true,
                CreditLimit = 10000m,
                Contacts = new List<Contact>(),
                BillingAddress = new Address(),
                CreatedAt = DateTime.UtcNow
            };
        }

        public static CustomerBuilder New() => new CustomerBuilder();

        public CustomerBuilder WithId(string id) { customer.Id = id; return this; }
        public CustomerBuilder WithCompanyName(string name) { customer.CompanyName = name; return this; }
        public CustomerBuilder WithIsActive(bool isActive) { customer.IsActive = isActive; return this; }
        public CustomerBuilder WithCreditLimit(decimal limit) { customer.CreditLimit = limit; return this; }
        public CustomerBuilder WithContact(Contact contact) { customer.Contacts.Add(contact); return this; }
        public CustomerBuilder WithContacts(IEnumerable<Contact> contacts) { customer.Contacts = new List<Contact>(contacts); return this; }
        public CustomerBuilder WithBillingAddress(Address address) { customer.BillingAddress = address; return this; }

        public Customer Build() => customer;
    }
}
