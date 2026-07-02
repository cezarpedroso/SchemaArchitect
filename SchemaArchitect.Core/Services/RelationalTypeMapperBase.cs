using SchemaArchitect.Core.Interfaces;

namespace SchemaArchitect.Core.Services;

/// <summary>
/// Provides shared SQL-to-C# mapping behavior for relational database type mappers.
/// </summary>
public abstract class RelationalTypeMapperBase : IColumnTypeMapper
{
    private static readonly IReadOnlyDictionary<string, string> CommonTypeMappings =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["bit"] = "bool",
            ["bool"] = "bool",
            ["boolean"] = "bool",
            ["tinyint"] = "byte",
            ["smallint"] = "short",
            ["int2"] = "short",
            ["mediumint"] = "int",
            ["int"] = "int",
            ["integer"] = "int",
            ["int4"] = "int",
            ["bigint"] = "long",
            ["int8"] = "long",
            ["smallserial"] = "short",
            ["serial2"] = "short",
            ["serial"] = "int",
            ["serial4"] = "int",
            ["bigserial"] = "long",
            ["serial8"] = "long",
            ["number"] = "decimal",
            ["numeric"] = "decimal",
            ["decimal"] = "decimal",
            ["dec"] = "decimal",
            ["fixed"] = "decimal",
            ["money"] = "decimal",
            ["smallmoney"] = "decimal",
            ["real"] = "float",
            ["float4"] = "float",
            ["float"] = "double",
            ["double"] = "double",
            ["double precision"] = "double",
            ["float8"] = "double",
            ["binary_float"] = "float",
            ["binary_double"] = "double",
            ["date"] = "DateTime",
            ["datetime"] = "DateTime",
            ["datetime2"] = "DateTime",
            ["smalldatetime"] = "DateTime",
            ["timestamp"] = "DateTime",
            ["timestamp without time zone"] = "DateTime",
            ["timestamp with time zone"] = "DateTime",
            ["timestamp with local time zone"] = "DateTime",
            ["timestamptz"] = "DateTime",
            ["datetimeoffset"] = "DateTimeOffset",
            ["time"] = "TimeSpan",
            ["time without time zone"] = "TimeSpan",
            ["time with time zone"] = "TimeSpan",
            ["interval"] = "TimeSpan",
            ["interval year to month"] = "TimeSpan",
            ["interval day to second"] = "TimeSpan",
            ["year"] = "int",
            ["uniqueidentifier"] = "Guid",
            ["uuid"] = "Guid",
            ["char"] = "string",
            ["character"] = "string",
            ["nchar"] = "string",
            ["national char"] = "string",
            ["national character"] = "string",
            ["varchar"] = "string",
            ["character varying"] = "string",
            ["varying character"] = "string",
            ["nvarchar"] = "string",
            ["national varchar"] = "string",
            ["national character varying"] = "string",
            ["varchar2"] = "string",
            ["nvarchar2"] = "string",
            ["native character"] = "string",
            ["text"] = "string",
            ["ntext"] = "string",
            ["tinytext"] = "string",
            ["mediumtext"] = "string",
            ["longtext"] = "string",
            ["clob"] = "string",
            ["nclob"] = "string",
            ["dbclob"] = "string",
            ["graphic"] = "string",
            ["vargraphic"] = "string",
            ["xml"] = "string",
            ["xmltype"] = "string",
            ["json"] = "string",
            ["jsonb"] = "string",
            ["citext"] = "string",
            ["enum"] = "string",
            ["set"] = "string",
            ["name"] = "string",
            ["rowid"] = "string",
            ["urowid"] = "string",
            ["binary"] = "byte[]",
            ["varbinary"] = "byte[]",
            ["image"] = "byte[]",
            ["raw"] = "byte[]",
            ["long raw"] = "byte[]",
            ["rowversion"] = "byte[]",
            ["bytea"] = "byte[]",
            ["blob"] = "byte[]",
            ["tinyblob"] = "byte[]",
            ["mediumblob"] = "byte[]",
            ["longblob"] = "byte[]",
            ["bfile"] = "byte[]",
        };

    private static readonly HashSet<string> NullableValueTypes = new(StringComparer.Ordinal)
    {
        "bool",
        "byte",
        "short",
        "int",
        "long",
        "decimal",
        "double",
        "float",
        "DateTime",
        "DateTimeOffset",
        "TimeSpan",
        "Guid",
    };

    private readonly IReadOnlyDictionary<string, string> typeMappings;
    private readonly string dialectName;

    /// <summary>
    /// Initializes a new instance of the <see cref="RelationalTypeMapperBase"/> class.
    /// </summary>
    /// <param name="typeMappings">The SQL type mappings.</param>
    /// <param name="dialectName">The user-facing SQL dialect name.</param>
    protected RelationalTypeMapperBase(
        IReadOnlyDictionary<string, string> typeMappings,
        string dialectName)
    {
        this.typeMappings = typeMappings ?? throw new ArgumentNullException(nameof(typeMappings));
        this.dialectName = dialectName ?? throw new ArgumentNullException(nameof(dialectName));
    }

    /// <inheritdoc />
    public string MapToCSharpType(string sqlType, bool isNullable)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sqlType);

        var normalizedType = NormalizeSqlType(sqlType);

        var csharpType = ResolveCSharpType(normalizedType);

        return isNullable && NullableValueTypes.Contains(csharpType)
            ? $"{csharpType}?"
            : csharpType;
    }

    /// <inheritdoc />
    public virtual string MapToCSharpType(
        string sqlType,
        bool isNullable,
        int? maxLength,
        byte? precision,
        byte? scale)
    {
        return MapToCSharpType(sqlType, isNullable);
    }

    /// <summary>
    /// Normalizes a SQL type declaration before dictionary lookup.
    /// </summary>
    /// <param name="sqlType">The SQL type declaration.</param>
    /// <returns>The normalized type name.</returns>
    protected virtual string NormalizeSqlType(string sqlType)
    {
        var normalizedType = sqlType.Trim();

        var facetStart = normalizedType.IndexOf('(', StringComparison.Ordinal);
        if (facetStart >= 0)
        {
            normalizedType = normalizedType[..facetStart];
        }

        normalizedType = normalizedType
            .Replace("[", string.Empty, StringComparison.Ordinal)
            .Replace("]", string.Empty, StringComparison.Ordinal)
            .Replace("\"", string.Empty, StringComparison.Ordinal)
            .Replace("`", string.Empty, StringComparison.Ordinal)
            .Trim();

        normalizedType = string.Join(
            ' ',
            normalizedType.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        return normalizedType.ToLowerInvariant();
    }

    private string ResolveCSharpType(string normalizedType)
    {
        if (TryResolveMappedType(normalizedType, out var csharpType))
        {
            return csharpType;
        }

        if (normalizedType.EndsWith("[]", StringComparison.Ordinal))
        {
            var elementType = normalizedType[..^2];
            var elementCSharpType = ResolveCSharpType(elementType);

            return elementCSharpType.EndsWith("[]", StringComparison.Ordinal)
                ? elementCSharpType
                : $"{elementCSharpType}[]";
        }

        return InferFallbackType(normalizedType);
    }

    private bool TryResolveMappedType(string normalizedType, out string csharpType)
    {
        if (typeMappings.TryGetValue(normalizedType, out var mappedType) ||
            CommonTypeMappings.TryGetValue(normalizedType, out mappedType))
        {
            csharpType = mappedType;
            return true;
        }

        var firstToken = normalizedType.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (firstToken is null)
        {
            csharpType = string.Empty;
            return false;
        }

        if (typeMappings.TryGetValue(firstToken, out mappedType) ||
            CommonTypeMappings.TryGetValue(firstToken, out mappedType))
        {
            csharpType = mappedType;
            return true;
        }

        csharpType = string.Empty;
        return false;
    }

    private static string InferFallbackType(string normalizedType)
    {
        if (ContainsAny(normalizedType, "binary", "blob", "bytea", "image", "raw", "rowversion"))
        {
            return "byte[]";
        }

        if (ContainsAny(normalizedType, "uuid", "guid", "identifier"))
        {
            return "Guid";
        }

        if (ContainsAny(normalizedType, "bool"))
        {
            return "bool";
        }

        if (ContainsAny(normalizedType, "date", "timestamp"))
        {
            return "DateTime";
        }

        if (normalizedType.Contains("time", StringComparison.OrdinalIgnoreCase))
        {
            return "TimeSpan";
        }

        if (ContainsAny(normalizedType, "money", "decimal", "numeric", "number"))
        {
            return "decimal";
        }

        if (ContainsAny(normalizedType, "bigint", "int8"))
        {
            return "long";
        }

        if (ContainsAny(normalizedType, "smallint", "int2"))
        {
            return "short";
        }

        if (ContainsAny(normalizedType, "tinyint"))
        {
            return "byte";
        }

        if (normalizedType.Contains("int", StringComparison.OrdinalIgnoreCase))
        {
            return "int";
        }

        if (ContainsAny(normalizedType, "double", "float"))
        {
            return "double";
        }

        if (normalizedType.Contains("real", StringComparison.OrdinalIgnoreCase))
        {
            return "float";
        }

        return "string";
    }

    private static bool ContainsAny(string value, params string[] tokens)
    {
        return tokens.Any(token => value.Contains(token, StringComparison.OrdinalIgnoreCase));
    }
}
