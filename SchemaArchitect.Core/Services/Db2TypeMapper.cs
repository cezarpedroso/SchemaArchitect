namespace SchemaArchitect.Core.Services;

/// <summary>
/// Provides IBM Db2 to C# type mappings for Schema Architect generation workflows.
/// </summary>
public sealed class Db2TypeMapper : RelationalTypeMapperBase
{
    private static readonly IReadOnlyDictionary<string, string> TypeMappings =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["boolean"] = "bool",
            ["smallint"] = "short",
            ["integer"] = "int",
            ["int"] = "int",
            ["bigint"] = "long",
            ["decimal"] = "decimal",
            ["dec"] = "decimal",
            ["numeric"] = "decimal",
            ["real"] = "float",
            ["double"] = "double",
            ["double precision"] = "double",
            ["float"] = "double",
            ["char"] = "string",
            ["character"] = "string",
            ["varchar"] = "string",
            ["character varying"] = "string",
            ["clob"] = "string",
            ["graphic"] = "string",
            ["vargraphic"] = "string",
            ["dbclob"] = "string",
            ["date"] = "DateTime",
            ["timestamp"] = "DateTime",
            ["time"] = "TimeSpan",
            ["binary"] = "byte[]",
            ["varbinary"] = "byte[]",
            ["blob"] = "byte[]",
        };

    /// <summary>
    /// Initializes a new instance of the <see cref="Db2TypeMapper"/> class.
    /// </summary>
    public Db2TypeMapper()
        : base(TypeMappings, "IBM Db2")
    {
    }
}
