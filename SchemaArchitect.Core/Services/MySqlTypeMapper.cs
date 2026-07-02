namespace SchemaArchitect.Core.Services;

/// <summary>
/// Provides MySQL to C# type mappings for Schema Architect generation workflows.
/// </summary>
public sealed class MySqlTypeMapper : RelationalTypeMapperBase
{
    private static readonly IReadOnlyDictionary<string, string> TypeMappings =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["bit"] = "bool",
            ["bool"] = "bool",
            ["boolean"] = "bool",
            ["tinyint"] = "byte",
            ["smallint"] = "short",
            ["mediumint"] = "int",
            ["int"] = "int",
            ["integer"] = "int",
            ["bigint"] = "long",
            ["decimal"] = "decimal",
            ["dec"] = "decimal",
            ["numeric"] = "decimal",
            ["fixed"] = "decimal",
            ["float"] = "float",
            ["double"] = "double",
            ["double precision"] = "double",
            ["real"] = "double",
            ["date"] = "DateTime",
            ["datetime"] = "DateTime",
            ["timestamp"] = "DateTime",
            ["time"] = "TimeSpan",
            ["year"] = "int",
            ["char"] = "string",
            ["varchar"] = "string",
            ["tinytext"] = "string",
            ["text"] = "string",
            ["mediumtext"] = "string",
            ["longtext"] = "string",
            ["json"] = "string",
            ["enum"] = "string",
            ["set"] = "string",
            ["binary"] = "byte[]",
            ["varbinary"] = "byte[]",
            ["tinyblob"] = "byte[]",
            ["blob"] = "byte[]",
            ["mediumblob"] = "byte[]",
            ["longblob"] = "byte[]",
        };

    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlTypeMapper"/> class.
    /// </summary>
    public MySqlTypeMapper()
        : base(TypeMappings, "MySQL")
    {
    }

    /// <inheritdoc />
    protected override string NormalizeSqlType(string sqlType)
    {
        return base.NormalizeSqlType(sqlType)
            .Replace(" unsigned", string.Empty, StringComparison.Ordinal)
            .Replace(" zerofill", string.Empty, StringComparison.Ordinal);
    }
}
