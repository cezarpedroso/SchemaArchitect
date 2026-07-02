namespace SchemaArchitect.Core.Services;

/// <summary>
/// Provides Oracle Database to C# type mappings for Schema Architect generation workflows.
/// </summary>
public sealed class OracleTypeMapper : RelationalTypeMapperBase
{
    private static readonly IReadOnlyDictionary<string, string> TypeMappings =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["number"] = "decimal",
            ["numeric"] = "decimal",
            ["decimal"] = "decimal",
            ["dec"] = "decimal",
            ["float"] = "double",
            ["binary_float"] = "float",
            ["binary_double"] = "double",
            ["varchar2"] = "string",
            ["nvarchar2"] = "string",
            ["char"] = "string",
            ["nchar"] = "string",
            ["clob"] = "string",
            ["nclob"] = "string",
            ["long"] = "string",
            ["xmltype"] = "string",
            ["json"] = "string",
            ["date"] = "DateTime",
            ["timestamp"] = "DateTime",
            ["timestamp with time zone"] = "DateTime",
            ["timestamp with local time zone"] = "DateTime",
            ["interval year to month"] = "TimeSpan",
            ["interval day to second"] = "TimeSpan",
            ["raw"] = "byte[]",
            ["long raw"] = "byte[]",
            ["blob"] = "byte[]",
            ["bfile"] = "byte[]",
            ["rowid"] = "string",
            ["urowid"] = "string",
        };

    /// <summary>
    /// Initializes a new instance of the <see cref="OracleTypeMapper"/> class.
    /// </summary>
    public OracleTypeMapper()
        : base(TypeMappings, "Oracle")
    {
    }

    /// <inheritdoc />
    public override string MapToCSharpType(
        string sqlType,
        bool isNullable,
        int? maxLength,
        byte? precision,
        byte? scale)
    {
        var normalizedType = NormalizeSqlType(sqlType);
        if (normalizedType is not ("number" or "numeric" or "decimal" or "dec") ||
            precision is null ||
            scale is > 0)
        {
            return base.MapToCSharpType(sqlType, isNullable, maxLength, precision, scale);
        }

        var csharpType = precision.Value switch
        {
            <= 4 => "short",
            <= 9 => "int",
            <= 18 => "long",
            _ => "decimal",
        };

        return isNullable && csharpType != "decimal"
            ? $"{csharpType}?"
            : isNullable
                ? "decimal?"
                : csharpType;
    }
}
