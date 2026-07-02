namespace SchemaArchitect.Core.Interfaces;

/// <summary>
/// Maps SQL store types to C# type names using optional column facets.
/// </summary>
public interface IColumnTypeMapper : ISqlTypeMapper
{
    /// <summary>
    /// Maps a SQL type and column facets to its corresponding C# type name.
    /// </summary>
    /// <param name="sqlType">The SQL type name or store type declaration.</param>
    /// <param name="isNullable">A value indicating whether the database column accepts null values.</param>
    /// <param name="maxLength">The max length facet, when declared.</param>
    /// <param name="precision">The precision facet, when declared.</param>
    /// <param name="scale">The scale facet, when declared.</param>
    /// <returns>The C# type name, including nullable value-type notation when appropriate.</returns>
    string MapToCSharpType(
        string sqlType,
        bool isNullable,
        int? maxLength,
        byte? precision,
        byte? scale);
}
