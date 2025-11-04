// <copyright file="PaymentRepository.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Data.Repositories;

using System.Data;
using System.Data.Common;
using HotshotLogistics.Core.Enums;
using HotshotLogistics.Core.Repositories;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Domain.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

/// <summary>
/// Repository for payment operations.
/// </summary>
internal class PaymentRepository : BaseRepository<Payment>, IPaymentRepository, IBaseRepository<Payment>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentRepository"/> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="logger">The logger.</param>
    public PaymentRepository(IConfiguration configuration, ILogger<PaymentRepository> logger)
        : base(configuration)
    {
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Payment>> GetByInvoiceIdAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT Id, InvoiceId, PaymentDate, Amount, PaymentMethod, TransactionId, ProcessorResponse, Status, CreatedAt, UpdatedAt
            FROM Payments
            WHERE InvoiceId = @InvoiceId
            ORDER BY PaymentDate DESC";

        var parameters = new[]
        {
            new SqlParameter("@InvoiceId", SqlDbType.NVarChar, 50) { Value = invoiceId },
        };

        return await ExecuteQueryAsync(sql, parameters);
    }

    /// <inheritdoc/>
    public async Task<Payment?> GetByTransactionIdAsync(string transactionId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT Id, InvoiceId, PaymentDate, Amount, PaymentMethod, TransactionId, ProcessorResponse, Status, CreatedAt, UpdatedAt
            FROM Payments
            WHERE TransactionId = @TransactionId";

        var parameters = new[]
        {
            new SqlParameter("@TransactionId", SqlDbType.NVarChar, 100) { Value = transactionId },
        };

        return (await ExecuteQueryAsync(sql, parameters)).FirstOrDefault();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Payment>> GetByStatusAsync(PaymentStatus status, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT Id, InvoiceId, PaymentDate, Amount, PaymentMethod, TransactionId, ProcessorResponse, Status, CreatedAt, UpdatedAt
            FROM Payments
            WHERE Status = @Status
            ORDER BY PaymentDate DESC";

        var parameters = new[]
        {
            new SqlParameter("@Status", SqlDbType.Int) { Value = (int)status },
        };

        return await ExecuteQueryAsync(sql, parameters);
    }

    /// <inheritdoc/>
    public async Task<bool> UpdatePaymentStatusAsync(string paymentId, PaymentStatus status, string? processorResponse = null, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE Payments
            SET Status = @Status,
                ProcessorResponse = @ProcessorResponse,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id";

        var parameters = new[]
        {
            new SqlParameter("@Id", SqlDbType.NVarChar, 50) { Value = paymentId },
            new SqlParameter("@Status", SqlDbType.Int) { Value = (int)status },
            new SqlParameter("@ProcessorResponse", SqlDbType.NVarChar, -1) { Value = (object?)processorResponse ?? DBNull.Value },
            new SqlParameter("@UpdatedAt", SqlDbType.DateTime2) { Value = DateTime.UtcNow },
        };

        var result = await ExecuteNonQueryAsync(sql, parameters);
        return result > 0;
    }

    /// <inheritdoc/>
    protected override string GetTableName() => "Payments";

    /// <inheritdoc/>
    protected override string GetPrimaryKeyColumnName() => "Id";

    /// <inheritdoc/>
    protected override Payment MapReaderToEntity(SqlDataReader reader)
    {
        return new Payment
        {
            Id = reader.GetString(reader.GetOrdinal("Id")),
            InvoiceId = reader.GetString(reader.GetOrdinal("InvoiceId")),
            PaymentDate = reader.GetDateTime(reader.GetOrdinal("PaymentDate")),
            Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
            PaymentMethod = (PaymentMethodType)reader.GetInt32(reader.GetOrdinal("PaymentMethod")),
            TransactionId = reader.GetString(reader.GetOrdinal("TransactionId")),
            ProcessorResponse = reader.IsDBNull(reader.GetOrdinal("ProcessorResponse")) ? string.Empty : reader.GetString(reader.GetOrdinal("ProcessorResponse")),
            Status = (PaymentStatus)reader.GetInt32(reader.GetOrdinal("Status")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
        };
    }

    /// <inheritdoc/>
    protected override SqlParameter[] GetInsertParameters(Payment entity)
    {
        return new[]
        {
            new SqlParameter("@Id", SqlDbType.NVarChar, 50) { Value = entity.Id },
            new SqlParameter("@InvoiceId", SqlDbType.NVarChar, 50) { Value = entity.InvoiceId },
            new SqlParameter("@PaymentDate", SqlDbType.DateTime2) { Value = entity.PaymentDate },
            new SqlParameter("@Amount", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = entity.Amount },
            new SqlParameter("@PaymentMethod", SqlDbType.Int) { Value = (int)entity.PaymentMethod },
            new SqlParameter("@TransactionId", SqlDbType.NVarChar, 100) { Value = entity.TransactionId },
            new SqlParameter("@ProcessorResponse", SqlDbType.NVarChar, -1) { Value = entity.ProcessorResponse },
            new SqlParameter("@Status", SqlDbType.Int) { Value = (int)entity.Status },
            new SqlParameter("@CreatedAt", SqlDbType.DateTime2) { Value = entity.CreatedAt },
            new SqlParameter("@UpdatedAt", SqlDbType.DateTime2) { Value = entity.UpdatedAt ?? (object)DBNull.Value },
        };
    }

    /// <inheritdoc/>
    protected override SqlParameter[] GetUpdateParameters(Payment entity)
    {
        return new[]
        {
            new SqlParameter("@Id", SqlDbType.NVarChar, 50) { Value = entity.Id },
            new SqlParameter("@InvoiceId", SqlDbType.NVarChar, 50) { Value = entity.InvoiceId },
            new SqlParameter("@PaymentDate", SqlDbType.DateTime2) { Value = entity.PaymentDate },
            new SqlParameter("@Amount", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = entity.Amount },
            new SqlParameter("@PaymentMethod", SqlDbType.Int) { Value = (int)entity.PaymentMethod },
            new SqlParameter("@TransactionId", SqlDbType.NVarChar, 100) { Value = entity.TransactionId },
            new SqlParameter("@ProcessorResponse", SqlDbType.NVarChar, -1) { Value = entity.ProcessorResponse },
            new SqlParameter("@Status", SqlDbType.Int) { Value = (int)entity.Status },
            new SqlParameter("@UpdatedAt", SqlDbType.DateTime2) { Value = entity.UpdatedAt ?? (object)DBNull.Value },
        };
    }
}
