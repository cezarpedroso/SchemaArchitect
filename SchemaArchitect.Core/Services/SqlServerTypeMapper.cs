namespace SchemaArchitect.Core.Services;

/// <summary>
/// Provides SQL Server to C# type mappings for Schema Architect generation workflows.
/// </summary>
public sealed class SqlServerTypeMapper : RelationalTypeMapperBase
{
    private static readonly IReadOnlyDictionary<string, string> TypeMappings =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["int"] = "int",
            ["bigint"] = "long",
            ["smallint"] = "short",
            ["tinyint"] = "byte",
            ["bit"] = "bool",
            ["decimal"] = "decimal",
            ["numeric"] = "decimal",
            ["money"] = "decimal",
            ["float"] = "double",
            ["real"] = "float",
            ["nvarchar"] = "string",
            ["varchar"] = "string",
            ["nchar"] = "string",
            ["char"] = "string",
            ["text"] = "string",
            ["ntext"] = "string",
            ["datetime"] = "DateTime",
            ["datetime2"] = "DateTime",
            ["date"] = "DateTime",
            ["time"] = "TimeSpan",
            ["datetimeoffset"] = "DateTimeOffset",
            ["uniqueidentifier"] = "Guid",
            ["varbinary"] = "byte[]",
            ["binary"] = "byte[]",
            ["image"] = "byte[]",
            ["rowversion"] = "byte[]",
            ["timestamp"] = "byte[]",
        };

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerTypeMapper"/> class.
    /// </summary>
    public SqlServerTypeMapper()
        : base(TypeMappings, "SQL Server")
    {
    }
}
