namespace SchemaArchitect.Core.Services;

/// <summary>
/// Provides PostgreSQL to C# type mappings for Schema Architect generation workflows.
/// </summary>
public sealed class PostgreSqlTypeMapper : RelationalTypeMapperBase
{
    private static readonly IReadOnlyDictionary<string, string> TypeMappings =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["smallint"] = "short",
            ["int2"] = "short",
            ["integer"] = "int",
            ["int"] = "int",
            ["int4"] = "int",
            ["bigint"] = "long",
            ["int8"] = "long",
            ["smallserial"] = "short",
            ["serial2"] = "short",
            ["serial"] = "int",
            ["serial4"] = "int",
            ["bigserial"] = "long",
            ["serial8"] = "long",
            ["boolean"] = "bool",
            ["bool"] = "bool",
            ["numeric"] = "decimal",
            ["decimal"] = "decimal",
            ["money"] = "decimal",
            ["real"] = "float",
            ["float4"] = "float",
            ["double precision"] = "double",
            ["float8"] = "double",
            ["uuid"] = "Guid",
            ["date"] = "DateTime",
            ["timestamp"] = "DateTime",
            ["timestamp without time zone"] = "DateTime",
            ["timestamp with time zone"] = "DateTime",
            ["timestamptz"] = "DateTime",
            ["time"] = "TimeSpan",
            ["time without time zone"] = "TimeSpan",
            ["time with time zone"] = "TimeSpan",
            ["interval"] = "TimeSpan",
            ["character"] = "string",
            ["char"] = "string",
            ["character varying"] = "string",
            ["varchar"] = "string",
            ["text"] = "string",
            ["citext"] = "string",
            ["json"] = "string",
            ["jsonb"] = "string",
            ["xml"] = "string",
            ["name"] = "string",
            ["bytea"] = "byte[]",
        };

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlTypeMapper"/> class.
    /// </summary>
    public PostgreSqlTypeMapper()
        : base(TypeMappings, "PostgreSQL")
    {
    }
}
