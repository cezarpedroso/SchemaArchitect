namespace SchemaArchitect.Core.Interfaces;

/// <summary>
/// Maps SQL store types to C# type names used by generated artifacts.
/// </summary>
public interface ISqlTypeMapper
{
    /// <summary>
    /// Maps a SQL type to its corresponding C# type name.
    /// </summary>
    /// <param name="sqlType">The SQL type name or store type declaration.</param>
    /// <param name="isNullable">A value indicating whether the database column accepts null values.</param>
    /// <returns>The C# type name, including nullable value-type notation when appropriate.</returns>
    string MapToCSharpType(string sqlType, bool isNullable);
}
