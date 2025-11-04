namespace HotshotLogistics.Core.Repositories;

using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Base repository implementation using native ADO.NET.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public abstract class BaseRepository<T> : IBaseRepository<T> where T : class
{
    private static readonly Regex SafeIdentifierRegex = new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseRepository{T}"/> class.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    protected BaseRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new ArgumentNullException(nameof(configuration), "Connection string 'DefaultConnection' is required");
    }

    /// <summary>
    /// Gets the connection string for database operations.
    /// </summary>
    protected string ConnectionString => _connectionString;

    /// <summary>
    /// Gets the table name for the entity.
    /// </summary>
    /// <returns>The table name.</returns>
    protected abstract string GetTableName();

    /// <summary>
    /// Gets the primary key column name.
    /// </summary>
    /// <returns>The primary key column name.</returns>
    protected abstract string GetPrimaryKeyColumnName();

    /// <summary>
    /// Maps a data reader to an entity.
    /// </summary>
    /// <param name="reader">The data reader.</param>
    /// <returns>The mapped entity.</returns>
    protected abstract T MapReaderToEntity(SqlDataReader reader);

    /// <summary>
    /// Gets the parameters for insert operation.
    /// </summary>
    /// <param name="entity">The entity to insert.</param>
    /// <returns>The SQL parameters.</returns>
    protected abstract SqlParameter[] GetInsertParameters(T entity);

    /// <summary>
    /// Gets the parameters for update operation.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <returns>The SQL parameters.</returns>
    protected abstract SqlParameter[] GetUpdateParameters(T entity);

    /// <inheritdoc/>
    public async Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default)
    {
        var tableName = FormatIdentifier(GetTableName());
        var primaryKeyColumn = FormatIdentifier(GetPrimaryKeyColumnName());
        var commandText = $"SELECT * FROM {tableName} WHERE {primaryKeyColumn} = @Id";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(commandText, connection);
        command.Parameters.AddWithValue("@Id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapReaderToEntity(reader);
        }

        return null;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<T>> GetAllAsync()
    {
        var tableName = FormatIdentifier(GetTableName());
        var commandText = $"SELECT * FROM {tableName}";

        var entities = new List<T>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(commandText, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            entities.Add(MapReaderToEntity(reader));
        }

        return entities;
    }

    /// <inheritdoc/>
    public async Task<T> AddAsync(T entity)
    {
        const string sqlTemplate = "INSERT INTO {0} ({1}) OUTPUT INSERTED.* VALUES ({2})";

        var tableName = FormatIdentifier(GetTableName());
        var parameters = GetInsertParameters(entity);
        var columnNames = string.Join(", ", parameters.Select(p => FormatIdentifier(p.ParameterName.TrimStart('@'))));
        var valuePlaceholders = string.Join(", ", parameters.Select(p => p.ParameterName));

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(string.Format(CultureInfo.InvariantCulture, sqlTemplate, tableName, columnNames, valuePlaceholders), connection);
        command.Parameters.AddRange(parameters);

        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapReaderToEntity(reader);
        }

        throw new InvalidOperationException("Failed to insert entity");
    }

    /// <inheritdoc/>
    public async Task<T> UpdateAsync(T entity)
    {
        const string sqlTemplate = "UPDATE {0} SET {1} OUTPUT INSERTED.* WHERE {2} = @Id";

        var tableName = FormatIdentifier(GetTableName());
        var parameters = GetUpdateParameters(entity);
        var primaryKeyColumn = FormatIdentifier(GetPrimaryKeyColumnName());
        var setClause = string.Join(", ", parameters
            .Where(p => !string.Equals(FormatIdentifier(p.ParameterName.TrimStart('@')), primaryKeyColumn, StringComparison.OrdinalIgnoreCase))
            .Select(p => $"{FormatIdentifier(p.ParameterName.TrimStart('@'))} = {p.ParameterName}"));

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(string.Format(CultureInfo.InvariantCulture, sqlTemplate, tableName, setClause, primaryKeyColumn), connection);
        command.Parameters.AddRange(parameters);

        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapReaderToEntity(reader);
        }

        throw new InvalidOperationException("Failed to update entity");
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(object id)
    {
        var tableName = FormatIdentifier(GetTableName());
        var primaryKeyColumn = FormatIdentifier(GetPrimaryKeyColumnName());
        var commandText = $"DELETE FROM {tableName} WHERE {primaryKeyColumn} = @Id";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(commandText, connection);
        command.Parameters.AddWithValue("@Id", id);

        var rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(object id)
    {
        var tableName = FormatIdentifier(GetTableName());
        var primaryKeyColumn = FormatIdentifier(GetPrimaryKeyColumnName());
        var commandText = $"SELECT COUNT(1) FROM {tableName} WHERE {primaryKeyColumn} = @Id";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(commandText, connection);
        command.Parameters.AddWithValue("@Id", id);

        var scalar = await command.ExecuteScalarAsync();
        var count = scalar is null || scalar == DBNull.Value
            ? 0
            : Convert.ToInt32(scalar, CultureInfo.InvariantCulture);

        return count > 0;
    }

    /// <summary>
    /// Executes a custom SQL query and returns entities.
    /// </summary>
    /// <param name="sql">The SQL query.</param>
    /// <param name="parameters">The query parameters.</param>
    /// <returns>A list of entities.</returns>
    protected async Task<IEnumerable<T>> ExecuteQueryAsync(string sql, SqlParameter[]? parameters = null)
    {
        var entities = new List<T>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(sql, connection);
        if (parameters != null)
        {
            command.Parameters.AddRange(parameters);
        }

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            entities.Add(MapReaderToEntity(reader));
        }

        return entities;
    }

    /// <summary>
    /// Executes a custom SQL command that doesn't return entities.
    /// </summary>
    /// <param name="sql">The SQL command.</param>
    /// <param name="parameters">The command parameters.</param>
    /// <returns>The number of affected rows.</returns>
    protected async Task<int> ExecuteNonQueryAsync(string sql, SqlParameter[]? parameters = null)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(sql, connection);
        if (parameters != null)
        {
            command.Parameters.AddRange(parameters);
        }

        return await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Executes a custom SQL query that returns a scalar value.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="sql">The SQL query.</param>
    /// <param name="parameters">The query parameters.</param>
    /// <returns>The scalar result.</returns>
    protected async Task<TResult> ExecuteScalarAsync<TResult>(string sql, SqlParameter[]? parameters = null)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(sql, connection);
        if (parameters != null)
        {
            command.Parameters.AddRange(parameters);
        }

        var result = await command.ExecuteScalarAsync();
        if (result is null || result == DBNull.Value)
        {
            return default!;
        }

        if (result is TResult typedResult)
        {
            return typedResult;
        }

        return (TResult)Convert.ChangeType(result, typeof(TResult), CultureInfo.InvariantCulture);
    }

    private static string FormatIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException("Identifier cannot be null or whitespace.", nameof(identifier));
        }

        if (!SafeIdentifierRegex.IsMatch(identifier))
        {
            throw new ArgumentException($"Identifier '{identifier}' contains invalid characters.", nameof(identifier));
        }

        return $"[{identifier}]";
    }
}
