namespace SchemaArchitect.Core.Services;

/// <summary>
/// Provides SQLite to C# type mappings for Schema Architect generation workflows.
/// </summary>
public sealed class SqliteTypeMapper : RelationalTypeMapperBase
{
    private static readonly IReadOnlyDictionary<string, string> TypeMappings =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["integer"] = "long",
            ["int"] = "int",
            ["tinyint"] = "byte",
            ["smallint"] = "short",
            ["mediumint"] = "int",
            ["bigint"] = "long",
            ["unsigned big int"] = "long",
            ["int2"] = "short",
            ["int8"] = "long",
            ["real"] = "double",
            ["double"] = "double",
            ["double precision"] = "double",
            ["float"] = "double",
            ["numeric"] = "decimal",
            ["decimal"] = "decimal",
            ["boolean"] = "bool",
            ["bool"] = "bool",
            ["date"] = "DateTime",
            ["datetime"] = "DateTime",
            ["timestamp"] = "DateTime",
            ["time"] = "TimeSpan",
            ["text"] = "string",
            ["character"] = "string",
            ["varchar"] = "string",
            ["varying character"] = "string",
            ["nchar"] = "string",
            ["native character"] = "string",
            ["nvarchar"] = "string",
            ["clob"] = "string",
            ["blob"] = "byte[]",
            ["binary"] = "byte[]",
            ["varbinary"] = "byte[]",
        };

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteTypeMapper"/> class.
    /// </summary>
    public SqliteTypeMapper()
        : base(TypeMappings, "SQLite")
    {
    }
}
