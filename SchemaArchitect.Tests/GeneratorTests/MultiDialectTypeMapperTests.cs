using SchemaArchitect.Core.Interfaces;
using SchemaArchitect.Core.Services;

namespace SchemaArchitect.Tests.GeneratorTests;

/// <summary>
/// Verifies type mapping behavior shared across supported SQL dialects.
/// </summary>
public sealed class MultiDialectTypeMapperTests
{
    /// <summary>
    /// Gets the supported dialect type mappers.
    /// </summary>
    public static IEnumerable<object[]> Mappers()
    {
        yield return [new SqlServerTypeMapper()];
        yield return [new MySqlTypeMapper()];
        yield return [new PostgreSqlTypeMapper()];
        yield return [new SqliteTypeMapper()];
        yield return [new OracleTypeMapper()];
        yield return [new Db2TypeMapper()];
    }

    /// <summary>
    /// Verifies common text aliases map to string even when they are not native to the selected dialect.
    /// </summary>
    /// <param name="mapper">The mapper under test.</param>
    [Theory]
    [MemberData(nameof(Mappers))]
    public void MapToCSharpType_WhenCommonTextAliasIsProvided_ReturnsString(ISqlTypeMapper mapper)
    {
        Assert.Equal("string", mapper.MapToCSharpType("nvarchar(120)", isNullable: false));
        Assert.Equal("string", mapper.MapToCSharpType("nvarchar2(120)", isNullable: false));
        Assert.Equal("string", mapper.MapToCSharpType("character varying(120)", isNullable: false));
    }

    /// <summary>
    /// Verifies common identifier aliases map to Guid across dialects.
    /// </summary>
    /// <param name="mapper">The mapper under test.</param>
    [Theory]
    [MemberData(nameof(Mappers))]
    public void MapToCSharpType_WhenCommonGuidAliasIsProvided_ReturnsGuid(ISqlTypeMapper mapper)
    {
        Assert.Equal("Guid", mapper.MapToCSharpType("uniqueidentifier", isNullable: false));
        Assert.Equal("Guid?", mapper.MapToCSharpType("uuid", isNullable: true));
    }

    /// <summary>
    /// Verifies unknown vendor-specific types fall back to a compilable C# type.
    /// </summary>
    /// <param name="mapper">The mapper under test.</param>
    [Theory]
    [MemberData(nameof(Mappers))]
    public void MapToCSharpType_WhenTypeIsUnknown_ReturnsStringFallback(ISqlTypeMapper mapper)
    {
        var csharpType = mapper.MapToCSharpType("made_up_vendor_type", isNullable: true);

        Assert.Equal("string", csharpType);
    }
}
