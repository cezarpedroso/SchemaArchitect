namespace SchemaArchitect.Core.Models;

/// <summary>
/// Represents a column discovered in a table definition.
/// </summary>
public sealed record ColumnSchema
{
    private static readonly HashSet<string> ValueTypes = new(StringComparer.Ordinal)
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

    /// <summary>
    /// Gets the column name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the SQL store type exactly as declared by the schema.
    /// </summary>
    public required string StoreType { get; init; }

    /// <summary>
    /// Gets the SQL type name represented by the column.
    /// </summary>
    public string SqlType => StoreType;

    /// <summary>
    /// Gets the mapped C# type name used by generation steps.
    /// </summary>
    public string CSharpType { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the column accepts null values.
    /// </summary>
    public bool IsNullable { get; init; }

    /// <summary>
    /// Gets a value indicating whether the column participates in the primary key.
    /// </summary>
    public bool IsPrimaryKey { get; init; }

    /// <summary>
    /// Gets a value indicating whether the column is identity-generated.
    /// </summary>
    public bool IsIdentity { get; init; }

    /// <summary>
    /// Gets the maximum length declared for a character or binary column.
    /// </summary>
    public int? MaxLength { get; init; }

    /// <summary>
    /// Gets the numeric precision declared for the column.
    /// </summary>
    public byte? Precision { get; init; }

    /// <summary>
    /// Gets the numeric scale declared for the column.
    /// </summary>
    public byte? Scale { get; init; }

    /// <summary>
    /// Gets a value indicating whether the mapped C# type is <see cref="string"/>.
    /// </summary>
    public bool IsString => string.Equals(CSharpType, "string", StringComparison.Ordinal);

    /// <summary>
    /// Gets a value indicating whether the mapped C# type is a byte array.
    /// </summary>
    public bool IsByteArray => string.Equals(CSharpType, "byte[]", StringComparison.Ordinal);

    /// <summary>
    /// Gets a value indicating whether the mapped C# type is a value type.
    /// </summary>
    public bool IsValueType => ValueTypes.Contains(GetNonNullableCSharpType());

    private string GetNonNullableCSharpType()
    {
        return CSharpType.EndsWith("?", StringComparison.Ordinal)
            ? CSharpType[..^1]
            : CSharpType;
    }
}
