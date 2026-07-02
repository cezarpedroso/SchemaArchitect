using System.Text.RegularExpressions;
using SchemaArchitect.Core.Models;

namespace SchemaArchitect.Web.Services;

/// <summary>
/// Performs lightweight schema health analysis for the preview workflow.
/// </summary>
public sealed partial class SchemaHealthAnalyzer : ISchemaHealthAnalyzer
{
    private static readonly HashSet<string> StringLikeStoreTypeTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "char",
        "text",
        "clob",
        "xml",
        "json",
        "enum",
        "set",
        "name",
        "rowid",
        "uuid",
    };

    /// <inheritdoc />
    public SchemaHealthReport Analyze(DatabaseSchema schema)
    {
        ArgumentNullException.ThrowIfNull(schema);

        var items = new List<SchemaHealthItem>();
        var incomingForeignKeyCounts = BuildIncomingForeignKeyCounts(schema);

        foreach (var table in schema.Tables)
        {
            AnalyzeTable(table, incomingForeignKeyCounts, items);
        }

        AddCircularReferenceFindings(schema, items);

        if (items.Count == 0)
        {
            items.Add(new SchemaHealthItem
            {
                Severity = SchemaHealthSeverity.Ready,
                Title = "Schema ready for generation",
                Explanation = "No schema health warnings were detected in the parsed model.",
            });
        }

        return new SchemaHealthReport
        {
            TableCount = schema.Tables.Count,
            ColumnCount = schema.Tables.Sum(static table => table.Columns.Count),
            PrimaryKeyColumnCount = schema.Tables.Sum(static table => table.PrimaryKey.Count),
            ForeignKeyCount = schema.Tables.Sum(static table => table.ForeignKeys.Count),
            IndexCount = schema.Tables.Sum(static table => table.Indexes.Count),
            RequiredColumnCount = schema.Tables.Sum(static table => table.Columns.Count(static column => !column.IsNullable)),
            NullableColumnCount = schema.Tables.Sum(static table => table.Columns.Count(static column => column.IsNullable)),
            Items = items
                .OrderByDescending(static item => item.Severity)
                .ThenBy(static item => item.TableName)
                .ThenBy(static item => item.ColumnName)
                .ThenBy(static item => item.Title)
                .ToArray(),
        };
    }

    private static void AnalyzeTable(
        TableSchema table,
        IReadOnlyDictionary<string, int> incomingForeignKeyCounts,
        ICollection<SchemaHealthItem> items)
    {
        var tableKey = CreateTableKey(table.Schema, table.Name);

        if (table.PrimaryKey.Count == 0)
        {
            items.Add(new SchemaHealthItem
            {
                Severity = SchemaHealthSeverity.Issue,
                Title = "Missing primary key",
                Explanation = "EF Core can map keyless types, but CRUD generation expects a stable primary key.",
                TableName = $"{table.Schema}.{table.Name}",
            });
        }
        else
        {
            items.Add(new SchemaHealthItem
            {
                Severity = SchemaHealthSeverity.Ready,
                Title = "Primary key detected",
                Explanation = "The table has a primary key that can be used for entity identity and controller routes.",
                TableName = $"{table.Schema}.{table.Name}",
            });
        }

        if (table.PrimaryKey.Count > 1)
        {
            items.Add(new SchemaHealthItem
            {
                Severity = SchemaHealthSeverity.Warning,
                Title = "Composite primary key",
                Explanation = "Composite keys are supported in Fluent API, but generated CRUD endpoints may need manual route adjustments.",
                TableName = $"{table.Schema}.{table.Name}",
            });
        }

        if (table.ForeignKeys.Count == 0 && !incomingForeignKeyCounts.ContainsKey(tableKey))
        {
            items.Add(new SchemaHealthItem
            {
                Severity = SchemaHealthSeverity.Warning,
                Title = "Isolated table",
                Explanation = "No incoming or outgoing relationships were detected for this table.",
                TableName = $"{table.Schema}.{table.Name}",
            });
        }

        AnalyzeColumns(table, items);
    }

    private static void AnalyzeColumns(TableSchema table, ICollection<SchemaHealthItem> items)
    {
        var foreignKeyColumns = table.ForeignKeys
            .SelectMany(static foreignKey => foreignKey.Columns)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var column in table.Columns)
        {
            if (column.IsIdentity)
            {
                items.Add(new SchemaHealthItem
                {
                    Severity = SchemaHealthSeverity.Ready,
                    Title = "Identity column detected",
                    Explanation = "Generated Fluent API will mark this property as value-generated on add.",
                    TableName = $"{table.Schema}.{table.Name}",
                    ColumnName = column.Name,
                });
            }

            if (foreignKeyColumns.Contains(column.Name) && column.IsNullable)
            {
                items.Add(new SchemaHealthItem
                {
                    Severity = SchemaHealthSeverity.Warning,
                    Title = "Nullable foreign key",
                    Explanation = "This relationship will be generated as optional. Confirm that nullable navigation behavior is intended.",
                    TableName = $"{table.Schema}.{table.Name}",
                    ColumnName = column.Name,
                });
            }

            if (LooksUnsupported(column))
            {
                items.Add(new SchemaHealthItem
                {
                    Severity = SchemaHealthSeverity.Warning,
                    Title = "Possible fallback type mapping",
                    Explanation = "The SQL type was mapped to string by convention. Review the generated property before using it in production.",
                    TableName = $"{table.Schema}.{table.Name}",
                    ColumnName = column.Name,
                });
            }

            if (!IsConventionalName(column.Name))
            {
                items.Add(new SchemaHealthItem
                {
                    Severity = SchemaHealthSeverity.Warning,
                    Title = "Column naming convention",
                    Explanation = "The column name contains characters or casing that may generate less idiomatic C# property names.",
                    TableName = $"{table.Schema}.{table.Name}",
                    ColumnName = column.Name,
                });
            }
        }

        if (!IsConventionalName(table.Name))
        {
            items.Add(new SchemaHealthItem
            {
                Severity = SchemaHealthSeverity.Warning,
                Title = "Table naming convention",
                Explanation = "The table name contains characters or casing that may generate less idiomatic C# type names.",
                TableName = $"{table.Schema}.{table.Name}",
            });
        }
    }

    private static bool LooksUnsupported(ColumnSchema column)
    {
        if (!string.Equals(column.CSharpType.TrimEnd('?'), "string", StringComparison.Ordinal))
        {
            return false;
        }

        return !StringLikeStoreTypeTokens.Any(token =>
            column.StoreType.Contains(token, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsConventionalName(string name)
    {
        return IdentifierRegex().IsMatch(name);
    }

    private static IReadOnlyDictionary<string, int> BuildIncomingForeignKeyCounts(DatabaseSchema schema)
    {
        return schema.Tables
            .SelectMany(static table => table.ForeignKeys)
            .GroupBy(
                static foreignKey => CreateTableKey(foreignKey.PrincipalSchema, foreignKey.PrincipalTable),
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(static group => group.Key, static group => group.Count(), StringComparer.OrdinalIgnoreCase);
    }

    private static void AddCircularReferenceFindings(DatabaseSchema schema, ICollection<SchemaHealthItem> items)
    {
        var graph = schema.Tables.ToDictionary(
            static table => CreateTableKey(table.Schema, table.Name),
            static table => table.ForeignKeys
                .Select(static foreignKey => CreateTableKey(foreignKey.PrincipalSchema, foreignKey.PrincipalTable))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            StringComparer.OrdinalIgnoreCase);

        var reported = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var table in schema.Tables)
        {
            var tableKey = CreateTableKey(table.Schema, table.Name);
            if (HasCycle(tableKey, tableKey, graph, [], out var cycleTarget) && reported.Add(tableKey))
            {
                items.Add(new SchemaHealthItem
                {
                    Severity = SchemaHealthSeverity.Warning,
                    Title = "Circular relationship",
                    Explanation = $"A relationship path loops back through {cycleTarget}. Review navigation generation for serialization concerns.",
                    TableName = $"{table.Schema}.{table.Name}",
                });
            }
        }
    }

    private static bool HasCycle(
        string start,
        string current,
        IReadOnlyDictionary<string, string[]> graph,
        HashSet<string> visited,
        out string cycleTarget)
    {
        cycleTarget = start;

        if (!graph.TryGetValue(current, out var nextTables))
        {
            return false;
        }

        foreach (var next in nextTables)
        {
            if (string.Equals(next, start, StringComparison.OrdinalIgnoreCase))
            {
                cycleTarget = next;
                return true;
            }

            if (visited.Add(next) && HasCycle(start, next, graph, visited, out cycleTarget))
            {
                return true;
            }
        }

        return false;
    }

    private static string CreateTableKey(string schema, string table)
    {
        return $"{schema}.{table}";
    }

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*$")]
    private static partial Regex IdentifierRegex();
}
