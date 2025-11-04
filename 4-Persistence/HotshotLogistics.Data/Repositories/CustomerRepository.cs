namespace HotshotLogistics.Data.Repositories;

using System.Data;
using System.Linq;
using HotshotLogistics.Contracts.Repositories;
using HotshotLogistics.Core.Enums;
using HotshotLogistics.Core.Repositories;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Domain.ValueObjects;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Repository for customer data access using native ADO.NET.
/// </summary>
internal class CustomerRepository : BaseRepository<Customer>, ICustomerRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CustomerRepository"/> class.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    public CustomerRepository(IConfiguration configuration)
        : base(configuration)
    {
    }

    /// <inheritdoc/>
    public async Task<Customer?> GetByTaxIdAsync(string taxId)
    {
        const string sql = "SELECT * FROM Customers WHERE TaxId = @TaxId";

        var parameters = new[] { new SqlParameter("@TaxId", SqlDbType.NVarChar) { Value = taxId } };
        var customers = await ExecuteQueryAsync(sql, parameters);

        return customers.Cast<Customer>().FirstOrDefault();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Customer>> GetByCreditLimitRangeAsync(decimal minLimit, decimal maxLimit)
    {
        const string sql = "SELECT * FROM Customers WHERE CreditLimit BETWEEN @MinLimit AND @MaxLimit AND IsActive = 1";

        var parameters = new[]
        {
            new SqlParameter("@MinLimit", SqlDbType.Decimal) { Value = minLimit },
            new SqlParameter("@MaxLimit", SqlDbType.Decimal) { Value = maxLimit },
        };

        return await ExecuteQueryAsync(sql, parameters);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Customer>> GetActiveCustomersAsync()
    {
        const string sql = "SELECT * FROM Customers WHERE IsActive = 1 ORDER BY CompanyName";

        return await ExecuteQueryAsync(sql);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Customer>> GetOverdueCustomersAsync()
    {
        const string sql = @"
            SELECT DISTINCT c.*
            FROM Customers c
            INNER JOIN Invoices i ON c.Id = i.CustomerId
            WHERE i.Status IN (4, 5) AND i.DueDate < @CurrentDate
            ORDER BY c.CompanyName";

        var parameters = new[] { new SqlParameter("@CurrentDate", SqlDbType.DateTime2) { Value = DateTime.UtcNow } };

        return await ExecuteQueryAsync(sql, parameters);
    }

    /// <inheritdoc/>
    public async Task<decimal> GetTotalCreditLimitAsync()
    {
        const string sql = "SELECT SUM(CreditLimit) FROM Customers WHERE IsActive = 1";

        var result = await ExecuteScalarAsync<decimal?>(sql);
        return result ?? 0;
    }

    /// <inheritdoc/>
    public async Task<int> GetCustomerCountAsync()
    {
        const string sql = "SELECT COUNT(1) FROM Customers WHERE IsActive = 1";

        return await ExecuteScalarAsync<int>(sql);
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateCreditLimitAsync(string customerId, decimal newLimit)
    {
        const string sql = "UPDATE Customers SET CreditLimit = @CreditLimit, UpdatedAt = @UpdatedAt WHERE Id = @CustomerId";

        var parameters = new[]
        {
            new SqlParameter("@CustomerId", SqlDbType.NVarChar) { Value = customerId },
            new SqlParameter("@CreditLimit", SqlDbType.Decimal) { Value = newLimit },
            new SqlParameter("@UpdatedAt", SqlDbType.DateTime2) { Value = DateTime.UtcNow },
        };

        var rowsAffected = await ExecuteNonQueryAsync(sql, parameters);
        return rowsAffected > 0;
    }

    /// <inheritdoc/>
    public async Task<bool> DeactivateCustomerAsync(string customerId)
    {
        const string sql = "UPDATE Customers SET IsActive = 0, UpdatedAt = @UpdatedAt WHERE Id = @CustomerId";

        var parameters = new[]
        {
            new SqlParameter("@CustomerId", SqlDbType.NVarChar) { Value = customerId },
            new SqlParameter("@UpdatedAt", SqlDbType.DateTime2) { Value = DateTime.UtcNow },
        };

        var rowsAffected = await ExecuteNonQueryAsync(sql, parameters);
        return rowsAffected > 0;
    }

    /// <inheritdoc/>
    public async Task<bool> ReactivateCustomerAsync(string customerId)
    {
        const string sql = "UPDATE Customers SET IsActive = 1, UpdatedAt = @UpdatedAt WHERE Id = @CustomerId";

        var parameters = new[]
        {
            new SqlParameter("@CustomerId", SqlDbType.NVarChar) { Value = customerId },
            new SqlParameter("@UpdatedAt", SqlDbType.DateTime2) { Value = DateTime.UtcNow },
        };

        var rowsAffected = await ExecuteNonQueryAsync(sql, parameters);
        return rowsAffected > 0;
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateCreditTermsAsync(string customerId, CreditTerms creditTerms)
    {
        const string sql = @"UPDATE Customers SET
            PaymentTermsDays = @PaymentTermsDays,
            CreditStatus = @CreditStatus,
            CreditApprovedDate = @CreditApprovedDate,
            CreditExpiryDate = @CreditExpiryDate,
            UpdatedAt = @UpdatedAt
            WHERE Id = @CustomerId";

        var parameters = new[]
        {
            new SqlParameter("@CustomerId", SqlDbType.NVarChar) { Value = customerId },
            new SqlParameter("@PaymentTermsDays", SqlDbType.Int) { Value = creditTerms.PaymentTermsDays },
            new SqlParameter("@CreditStatus", SqlDbType.Int) { Value = (int)creditTerms.Status },
            new SqlParameter("@CreditApprovedDate", SqlDbType.DateTime2) { Value = (object?)creditTerms.ApprovedDate ?? DBNull.Value },
            new SqlParameter("@CreditExpiryDate", SqlDbType.DateTime2) { Value = (object?)creditTerms.ExpiryDate ?? DBNull.Value },
            new SqlParameter("@UpdatedAt", SqlDbType.DateTime2) { Value = DateTime.UtcNow },
        };

        var rowsAffected = await ExecuteNonQueryAsync(sql, parameters);
        return rowsAffected > 0;
    }

    // Explicit interface implementations to bridge concrete/interface types
    async Task<Customer?> ICustomerRepository.GetByIdAsync(object id, CancellationToken cancellationToken)
    {
        return await GetByIdAsync(id, cancellationToken);
    }

    async Task<IEnumerable<Customer>> ICustomerRepository.GetAllAsync()
    {
        return (await GetAllAsync()).Cast<Customer>();
    }

    async Task<Customer> ICustomerRepository.AddAsync(Customer entity)
    {
        var customer = entity as Customer ?? throw new ArgumentException("Entity must be Customer", nameof(entity));
        return await AddAsync(customer);
    }

    async Task<Customer> ICustomerRepository.UpdateAsync(Customer entity)
    {
        var customer = entity as Customer ?? throw new ArgumentException("Entity must be Customer", nameof(entity));
        return await UpdateAsync(customer);
    }

    async Task<bool> ICustomerRepository.DeleteAsync(object id)
    {
        return await DeleteAsync(id);
    }

    async Task<bool> ICustomerRepository.ExistsAsync(object id)
    {
        return await ExistsAsync(id);
    }

    /// <inheritdoc/>
    protected override string GetTableName() => "Customers";

    /// <inheritdoc/>
    protected override string GetPrimaryKeyColumnName() => "Id";

    /// <inheritdoc/>
    protected override Customer MapReaderToEntity(SqlDataReader reader)
    {
        return new Customer
        {
            Id = reader.GetString(reader.GetOrdinal("Id")),
            CompanyName = reader.GetString(reader.GetOrdinal("CompanyName")),
            TaxId = reader.IsDBNull(reader.GetOrdinal("TaxId")) ? null : reader.GetString(reader.GetOrdinal("TaxId")),
            Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email")),
            Phone = reader.IsDBNull(reader.GetOrdinal("Phone")) ? null : reader.GetString(reader.GetOrdinal("Phone")),
            BillingAddress = new Address
            {
                Street = reader.GetString(reader.GetOrdinal("BillingAddress")),
                City = reader.GetString(reader.GetOrdinal("City")),
                State = reader.GetString(reader.GetOrdinal("State")),
                ZipCode = reader.GetString(reader.GetOrdinal("ZipCode")),
                Country = reader.GetString(reader.GetOrdinal("Country")),
                Latitude = (double)reader.GetDecimal(reader.GetOrdinal("Latitude")),
                Longitude = (double)reader.GetDecimal(reader.GetOrdinal("Longitude")),
            },
            Contacts = new List<Contact>(), // Initialize empty list
            CreditTerms = new CreditTerms
            {
                PaymentTermsDays = reader.GetInt32(reader.GetOrdinal("PaymentTermsDays")),
                Status = (CreditStatus)reader.GetInt32(reader.GetOrdinal("CreditStatus")),
                ApprovedDate = reader.IsDBNull(reader.GetOrdinal("CreditApprovedDate"))
                    ? DateTime.UtcNow.AddDays(-30) // Default to 30 days ago instead of DateTime.MinValue
                    : reader.GetDateTime(reader.GetOrdinal("CreditApprovedDate")),
                ExpiryDate = reader.IsDBNull(reader.GetOrdinal("CreditExpiryDate")) ? null : reader.GetDateTime(reader.GetOrdinal("CreditExpiryDate")),
            },
            CreditLimit = reader.GetDecimal(reader.GetOrdinal("CreditLimit")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
        };
    }

    /// <inheritdoc/>
    protected override SqlParameter[] GetInsertParameters(Customer entity)
    {
        return new[]
        {
            new SqlParameter("@Id", SqlDbType.NVarChar) { Value = entity.Id },
            new SqlParameter("@CompanyName", SqlDbType.NVarChar) { Value = entity.CompanyName },
            new SqlParameter("@TaxId", SqlDbType.NVarChar) { Value = (object?)entity.TaxId ?? DBNull.Value },
            new SqlParameter("@Email", SqlDbType.NVarChar) { Value = (object?)entity.Email ?? DBNull.Value },
            new SqlParameter("@Phone", SqlDbType.NVarChar) { Value = (object?)entity.Phone ?? DBNull.Value },
            new SqlParameter("@BillingAddress", SqlDbType.NVarChar) { Value = FormatAddress(entity.BillingAddress) },
            new SqlParameter("@City", SqlDbType.NVarChar) { Value = entity.BillingAddress.City },
            new SqlParameter("@State", SqlDbType.NVarChar) { Value = entity.BillingAddress.State },
            new SqlParameter("@ZipCode", SqlDbType.NVarChar) { Value = entity.BillingAddress.ZipCode },
            new SqlParameter("@Country", SqlDbType.NVarChar) { Value = entity.BillingAddress.Country },
            new SqlParameter("@Latitude", SqlDbType.Decimal) { Value = entity.BillingAddress.Latitude },
            new SqlParameter("@Longitude", SqlDbType.Decimal) { Value = entity.BillingAddress.Longitude },
            new SqlParameter("@PaymentTermsDays", SqlDbType.Int) { Value = entity.CreditTerms.PaymentTermsDays },
            new SqlParameter("@CreditStatus", SqlDbType.Int) { Value = (int)entity.CreditTerms.Status },
            new SqlParameter("@CreditApprovedDate", SqlDbType.DateTime2) { Value = (object?)entity.CreditTerms.ApprovedDate ?? DBNull.Value },
            new SqlParameter("@CreditExpiryDate", SqlDbType.DateTime2) { Value = (object?)entity.CreditTerms.ExpiryDate ?? DBNull.Value },
            new SqlParameter("@CreditLimit", SqlDbType.Decimal) { Value = entity.CreditLimit },
            new SqlParameter("@IsActive", SqlDbType.Bit) { Value = entity.IsActive },
        };
    }

    /// <inheritdoc/>
    protected override SqlParameter[] GetUpdateParameters(Customer entity)
    {
        return new[]
        {
            new SqlParameter("@Id", SqlDbType.NVarChar) { Value = entity.Id },
            new SqlParameter("@CompanyName", SqlDbType.NVarChar) { Value = entity.CompanyName },
            new SqlParameter("@TaxId", SqlDbType.NVarChar) { Value = (object?)entity.TaxId ?? DBNull.Value },
            new SqlParameter("@Email", SqlDbType.NVarChar) { Value = (object?)entity.Email ?? DBNull.Value },
            new SqlParameter("@Phone", SqlDbType.NVarChar) { Value = (object?)entity.Phone ?? DBNull.Value },
            new SqlParameter("@BillingAddress", SqlDbType.NVarChar) { Value = FormatAddress(entity.BillingAddress) },
            new SqlParameter("@City", SqlDbType.NVarChar) { Value = entity.BillingAddress.City },
            new SqlParameter("@State", SqlDbType.NVarChar) { Value = entity.BillingAddress.State },
            new SqlParameter("@ZipCode", SqlDbType.NVarChar) { Value = entity.BillingAddress.ZipCode },
            new SqlParameter("@Country", SqlDbType.NVarChar) { Value = entity.BillingAddress.Country },
            new SqlParameter("@Latitude", SqlDbType.Decimal) { Value = entity.BillingAddress.Latitude },
            new SqlParameter("@Longitude", SqlDbType.Decimal) { Value = entity.BillingAddress.Longitude },
            new SqlParameter("@PaymentTermsDays", SqlDbType.Int) { Value = entity.CreditTerms.PaymentTermsDays },
            new SqlParameter("@CreditStatus", SqlDbType.Int) { Value = (int)entity.CreditTerms.Status },
            new SqlParameter("@CreditApprovedDate", SqlDbType.DateTime2) { Value = (object?)entity.CreditTerms.ApprovedDate ?? DBNull.Value },
            new SqlParameter("@CreditExpiryDate", SqlDbType.DateTime2) { Value = (object?)entity.CreditTerms.ExpiryDate ?? DBNull.Value },
            new SqlParameter("@CreditLimit", SqlDbType.Decimal) { Value = entity.CreditLimit },
            new SqlParameter("@IsActive", SqlDbType.Bit) { Value = entity.IsActive },
        };
    }

    private static string FormatAddress(Address? address)
    {
        if (address is null)
        {
            return string.Empty;
        }

        return $"{address.Street}, {address.City}, {address.State} {address.ZipCode}";
    }
}
