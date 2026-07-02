using System.Text;
using System.Text.RegularExpressions;
using SchemaArchitect.Core.Models;

namespace SchemaArchitect.Core.Parsing;

/// <summary>
/// Parses common standalone SQL index declarations.
/// </summary>
internal static class SqlIndexParser
{
    private static readonly Regex CreateIndexRegex = new(
        @"\bCREATE\s+(?<unique>UNIQUE\s+)?(?:(?:CLUSTERED|NONCLUSTERED|BITMAP|FULLTEXT|SPATIAL)\s+)?INDEX\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Attaches standalone index declarations to already parsed tables.
    /// </summary>
    /// <param name="sql">The sanitized SQL script.</param>
    /// <param name="tables">The parsed tables.</param>
    /// <param name="defaultSchema">The default schema for unqualified table names.</param>
    /// <returns>Tables with matching index declarations attached.</returns>
    public static IReadOnlyList<TableSchema> AttachStandaloneIndexes(
        string sql,
        IReadOnlyList<TableSchema> tables,
        string defaultSchema)
    {
        ArgumentNullException.ThrowIfNull(sql);
        ArgumentNullException.ThrowIfNull(tables);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultSchema);

        if (tables.Count == 0)
        {
            return tables;
        }

        var indexesByTable = ParseStandaloneIndexes(sql, defaultSchema)
            .GroupBy(static index => $"{index.Schema}.{index.Table}", StringComparer.OrdinalIgnoreCase)
            .ToDictionary(static group => group.Key, static group => group.Select(static item => item.Index).ToArray(), StringComparer.OrdinalIgnoreCase);

        if (indexesByTable.Count == 0)
        {
            return tables;
        }

        return tables
            .Select(table =>
            {
                var key = $"{table.Schema}.{table.Name}";
                return indexesByTable.TryGetValue(key, out var indexes)
                    ? table with { Indexes = table.Indexes.Concat(indexes).ToArray() }
                    : table;
            })
            .ToArray();
    }

    private static IReadOnlyList<StandaloneIndex> ParseStandaloneIndexes(string sql, string defaultSchema)
    {
        var indexes = new List<StandaloneIndex>();
        var searchIndex = 0;

        while (searchIndex < sql.Length)
        {
            var match = CreateIndexRegex.Match(sql, searchIndex);
            if (!match.Success)
            {
                break;
            }

            if (TryParseStandaloneIndex(sql, match, defaultSchema, out var index))
            {
                indexes.Add(index);
            }

            searchIndex = match.Index + match.Length;
        }

        return indexes;
    }

    private static bool TryParseStandaloneIndex(
        string sql,
        Match match,
        string defaultSchema,
        out StandaloneIndex index)
    {
        index = null!;

        var position = match.Index + match.Length;
        SkipOptionalIndexClauses(sql, ref position);

        var indexNameParts = ReadQualifiedIdentifierParts(sql, ref position);
        if (indexNameParts.Count == 0)
        {
            return false;
        }

        if (!TryReadKeyword(sql, ref position, "ON"))
        {
            return false;
        }

        var tableNameParts = ReadQualifiedIdentifierParts(sql, ref position);
        if (tableNameParts.Count == 0)
        {
            return false;
        }

        SkipWhitespace(sql, ref position);
        if (TryPeekKeyword(sql, position, "USING"))
        {
            TryReadKeyword(sql, ref position, "USING");
            _ = ReadIdentifier(sql, ref position);
        }

        SkipWhitespace(sql, ref position);
        if (position >= sql.Length || sql[position] != '(')
        {
            return false;
        }

        var columnsEnd = FindMatchingParenthesis(sql, position);
        if (columnsEnd < 0)
        {
            return false;
        }

        var (schema, table) = ResolveSchemaAndTableName(tableNameParts, defaultSchema);
        var columns = ParseColumnList(sql[(position + 1)..columnsEnd]);

        if (columns.Count == 0)
        {
            return false;
        }

        index = new StandaloneIndex(
            schema,
            table,
            new IndexSchema
            {
                Name = indexNameParts[^1],
                IsUnique = match.Groups["unique"].Success,
                Columns = columns,
            });

        return true;
    }

    private static void SkipOptionalIndexClauses(string text, ref int position)
    {
        while (true)
        {
            if (TryReadKeyword(text, ref position, "CONCURRENTLY") ||
                TryReadKeyword(text, ref position, "ONLINE"))
            {
                continue;
            }

            var beforeIf = position;
            if (TryReadKeyword(text, ref position, "IF") &&
                TryReadKeyword(text, ref position, "NOT") &&
                TryReadKeyword(text, ref position, "EXISTS"))
            {
                continue;
            }

            position = beforeIf;
            return;
        }
    }

    private static IReadOnlyList<string> ParseColumnList(string columnList)
    {
        return SplitTopLevel(columnList, ',')
            .Select(NormalizeIndexColumn)
            .Where(static column => !string.IsNullOrWhiteSpace(column))
            .ToArray();
    }

    private static string NormalizeIndexColumn(string definition)
    {
        var position = 0;
        var column = ReadIdentifier(definition, ref position);
        return column ?? definition.Trim();
    }

    private static (string Schema, string Table) ResolveSchemaAndTableName(
        IReadOnlyList<string> nameParts,
        string defaultSchema)
    {
        return nameParts.Count switch
        {
            1 => (defaultSchema, nameParts[0]),
            _ => (nameParts[^2], nameParts[^1]),
        };
    }

    private static IReadOnlyList<string> ReadQualifiedIdentifierParts(string text, ref int position)
    {
        var parts = new List<string>();

        while (position < text.Length)
        {
            var part = ReadIdentifier(text, ref position);
            if (string.IsNullOrWhiteSpace(part))
            {
                break;
            }

            parts.Add(part);
            SkipWhitespace(text, ref position);

            if (position >= text.Length || text[position] != '.')
            {
                break;
            }

            position++;
            SkipWhitespace(text, ref position);
        }

        return parts;
    }

    private static string? ReadIdentifier(string text, ref int position)
    {
        SkipWhitespace(text, ref position);

        if (position >= text.Length)
        {
            return null;
        }

        return text[position] switch
        {
            '[' => ReadDelimitedIdentifier(text, ref position, '[', ']'),
            '"' => ReadDelimitedIdentifier(text, ref position, '"', '"'),
            '`' => ReadDelimitedIdentifier(text, ref position, '`', '`'),
            _ => ReadUnquotedIdentifier(text, ref position),
        };
    }

    private static string? ReadUnquotedIdentifier(string text, ref int position)
    {
        var start = position;

        while (position < text.Length &&
            !char.IsWhiteSpace(text[position]) &&
            text[position] is not '(' and not ')' and not ',' and not '.' and not ';')
        {
            position++;
        }

        return start == position
            ? null
            : text[start..position].Trim();
    }

    private static string ReadDelimitedIdentifier(
        string text,
        ref int position,
        char openingDelimiter,
        char closingDelimiter)
    {
        var builder = new StringBuilder();
        position++;

        while (position < text.Length)
        {
            if (text[position] == closingDelimiter)
            {
                if (position + 1 < text.Length && text[position + 1] == closingDelimiter)
                {
                    builder.Append(closingDelimiter);
                    position += 2;
                    continue;
                }

                position++;
                return builder.ToString();
            }

            builder.Append(text[position]);
            position++;
        }

        return string.Empty;
    }

    private static bool TryReadKeyword(string text, ref int position, string keyword)
    {
        SkipWhitespace(text, ref position);

        if (!TryPeekKeyword(text, position, keyword))
        {
            return false;
        }

        position += keyword.Length;
        return true;
    }

    private static bool TryPeekKeyword(string text, int position, string keyword)
    {
        if (position + keyword.Length > text.Length ||
            !text.AsSpan(position, keyword.Length).Equals(keyword, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var nextPosition = position + keyword.Length;
        return nextPosition >= text.Length ||
            !char.IsLetterOrDigit(text[nextPosition]) &&
            text[nextPosition] != '_';
    }

    private static void SkipWhitespace(string text, ref int position)
    {
        while (position < text.Length && char.IsWhiteSpace(text[position]))
        {
            position++;
        }
    }

    private static int FindMatchingParenthesis(string text, int openingParenthesisIndex)
    {
        var depth = 0;

        for (var index = openingParenthesisIndex; index < text.Length; index++)
        {
            switch (text[index])
            {
                case '(':
                    depth++;
                    break;
                case ')':
                    depth--;
                    if (depth == 0)
                    {
                        return index;
                    }

                    break;
            }
        }

        return -1;
    }

    private static IReadOnlyList<string> SplitTopLevel(string text, char separator)
    {
        var values = new List<string>();
        var start = 0;
        var depth = 0;

        for (var index = 0; index < text.Length; index++)
        {
            switch (text[index])
            {
                case '(':
                    depth++;
                    break;
                case ')':
                    depth--;
                    break;
                default:
                    if (text[index] == separator && depth == 0)
                    {
                        values.Add(text[start..index]);
                        start = index + 1;
                    }

                    break;
            }
        }

        values.Add(text[start..]);

        return values;
    }

    private sealed record StandaloneIndex(string Schema, string Table, IndexSchema Index);
}
