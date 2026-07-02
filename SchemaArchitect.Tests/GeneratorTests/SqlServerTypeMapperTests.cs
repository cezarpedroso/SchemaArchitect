using SchemaArchitect.Core.Services;

namespace SchemaArchitect.Tests.GeneratorTests;

/// <summary>
/// Verifies SQL Server to C# type mapping behavior.
/// </summary>
public sealed class SqlServerTypeMapperTests
{
    private readonly SqlServerTypeMapper mapper = new();

    /// <summary>
    /// Verifies supported SQL Server types map to the expected non-nullable C# types.
    /// </summary>
    /// <param name="sqlType">The SQL Server type to map.</param>
    /// <param name="expectedCSharpType">The expected C# type name.</param>
    [Theory]
    [InlineData("int", "int")]
    [InlineData("bigint", "long")]
    [InlineData("smallint", "short")]
    [InlineData("tinyint", "byte")]
    [InlineData("bit", "bool")]
    [InlineData("decimal", "decimal")]
    [InlineData("numeric", "decimal")]
    [InlineData("money", "decimal")]
    [InlineData("float", "double")]
    [InlineData("real", "float")]
    [InlineData("nvarchar", "string")]
    [InlineData("varchar", "string")]
    [InlineData("nchar", "string")]
    [InlineData("char", "string")]
    [InlineData("text", "string")]
    [InlineData("ntext", "string")]
    [InlineData("datetime", "DateTime")]
    [InlineData("datetime2", "DateTime")]
    [InlineData("date", "DateTime")]
    [InlineData("time", "TimeSpan")]
    [InlineData("uniqueidentifier", "Guid")]
    [InlineData("varbinary", "byte[]")]
    [InlineData("binary", "byte[]")]
    public void MapToCSharpType_WhenTypeIsSupported_ReturnsExpectedType(
        string sqlType,
        string expectedCSharpType)
    {
        var csharpType = mapper.MapToCSharpType(sqlType, isNullable: false);

        Assert.Equal(expectedCSharpType, csharpType);
    }

    /// <summary>
    /// Verifies nullable value-type columns map to nullable C# value types.
    /// </summary>
    [Fact]
    public void MapToCSharpType_WhenValueTypeIsNullable_ReturnsNullableValueType()
    {
        var csharpType = mapper.MapToCSharpType("int", isNullable: true);

        Assert.Equal("int?", csharpType);
    }

    /// <summary>
    /// Verifies string columns do not receive nullable value-type notation.
    /// </summary>
    [Fact]
    public void MapToCSharpType_WhenStringIsNullable_ReturnsString()
    {
        var csharpType = mapper.MapToCSharpType("nvarchar(100)", isNullable: true);

        Assert.Equal("string", csharpType);
    }

    /// <summary>
    /// Verifies binary array columns do not receive nullable value-type notation.
    /// </summary>
    [Fact]
    public void MapToCSharpType_WhenByteArrayIsNullable_ReturnsByteArray()
    {
        var csharpType = mapper.MapToCSharpType("varbinary(max)", isNullable: true);

        Assert.Equal("byte[]", csharpType);
    }

    /// <summary>
    /// Verifies bracketed SQL Server store types are normalized before mapping.
    /// </summary>
    [Fact]
    public void MapToCSharpType_WhenTypeIsBracketed_ReturnsExpectedType()
    {
        var csharpType = mapper.MapToCSharpType("[nvarchar](200)", isNullable: false);

        Assert.Equal("string", csharpType);
    }

    /// <summary>
    /// Verifies unknown SQL Server store types fall back to a compilable C# type.
    /// </summary>
    [Fact]
    public void MapToCSharpType_WhenTypeIsUnknown_ReturnsStringFallback()
    {
        var csharpType = mapper.MapToCSharpType("geography", isNullable: false);

        Assert.Equal("string", csharpType);
    }
}
