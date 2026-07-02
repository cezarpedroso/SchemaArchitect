using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using SchemaArchitect.Core.Interfaces;
using SchemaArchitect.Core.Models;

namespace SchemaArchitect.Core.Parsing;

/// <summary>
/// Parses common relational CREATE TABLE DDL into Schema Architect schema models.
/// </summary>
internal sealed class CreateTableSchemaParser : ISchemaParser
{
    private static readonly Regex CreateTableRegex = new(
        @"\bCREATE\s+(?:(?:GLOBAL|LOCAL)\s+TEMPORARY\s+|TEMP(?:ORARY)?\s+|UNLOGGED\s+)?TABLE\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex NotNullRegex = new(
        @"\bNOT\s+NULL\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ExplicitNullRegex = new(
        @"(?<!NOT\s)\bNULL\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PrimaryKeyRegex = new(
        @"\bPRIMARY\s+KEY\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ForeignKeyRegex = new(
        @"\bFOREIGN\s+KEY\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ReferencesRegex = new(
        @"\bREFERENCES\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ConstraintRegex = new(
        @"\bCONSTRAINT\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly HashSet<string> TypeTerminatorKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "not",
        "null",
        "primary",
        "references",
        "default",
        "constraint",
        "collate",
        "comment",
        "auto_increment",
        "autoincrement",
        "generated",
        "identity",
        "unique",
        "check",
        "enable",
        "disable",
        "on",
        "compress",
        "encode",
        "distkey",
        "sortkey",
        "masking",
    };

    private static readonly HashSet<string> TableLevelDefinitionKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "constraint",
        "primary",
        "foreign",
        "unique",
        "check",
        "index",
        "key",
        "fulltext",
        "spatial",
        "exclude",
        "like",
        "partition",
    };

    private readonly CreateTableSchemaParserOptions options;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateTableSchemaParser"/> class.
    /// </summary>
    /// <param name="options">The parser options.</param>
    public CreateTableSchemaParser(CreateTableSchemaParserOptions options)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public Task<DatabaseSchema> ParseAsync(
        string schemaSql,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(schemaSql);

        cancellationToken.ThrowIfCancellationRequested();

        var sanitizedSql = RemoveComments(schemaSql, options.SupportsHashLineComments);
        var tables = SqlIndexParser.AttachStandaloneIndexes(
            sanitizedSql,
            ParseCreateTableStatements(sanitizedSql, cancellationToken),
            options.DefaultSchema);

        return Task.FromResult(new DatabaseSchema
        {
            Dialect = options.Dialect,
            Tables = tables,
        });
    }

    private IReadOnlyList<TableSchema> ParseCreateTableStatements(
        string schemaSql,
        CancellationToken cancellationToken)
    {
        var tables = new List<TableSchema>();
        var searchIndex = 0;

        while (searchIndex < schemaSql.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var match = CreateTableRegex.Match(schemaSql, searchIndex);
            if (!match.Success)
            {
                break;
            }

            var tableNameStart = SkipOptionalCreateTableClauses(schemaSql, match.Index + match.Length);
            var bodyStart = FindFirstOpeningParenthesis(schemaSql, tableNameStart);
            if (bodyStart < 0)
            {
                throw new FormatException("CREATE TABLE statement is missing a column definition body.");
            }

            var bodyEnd = FindMatchingParenthesis(schemaSql, bodyStart);
            if (bodyEnd < 0)
            {
                throw new FormatException("CREATE TABLE statement has an unterminated column definition body.");
            }

            var tableNameText = schemaSql[tableNameStart..bodyStart].Trim();
            var body = schemaSql[(bodyStart + 1)..bodyEnd];

            tables.Add(ParseCreateTableStatement(tableNameText, body));
            searchIndex = bodyEnd + 1;
        }

        return tables;
    }

    private TableSchema ParseCreateTableStatement(string tableNameText, string body)
    {
        var (schemaName, tableName) = ParseSchemaAndTableName(tableNameText);
        var columnBuilders = new List<ColumnBuilder>();
        var primaryKeyColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var foreignKeys = new List<ForeignKeySchema>();
        var indexes = new List<IndexSchema>();

        foreach (var definition in SplitTopLevel(body, ','))
        {
            var trimmedDefinition = definition.Trim();
            if (trimmedDefinition.Length == 0)
            {
                continue;
            }

            if (IsTableLevelDefinition(trimmedDefinition))
            {
                ParseTableLevelDefinition(trimmedDefinition, tableName, primaryKeyColumns, foreignKeys, indexes);
                continue;
            }

            var columnBuilder = ParseColumnDefinition(trimmedDefinition, tableName);
            columnBuilders.Add(columnBuilder);

            if (columnBuilder.IsPrimaryKey)
            {
                primaryKeyColumns.Add(columnBuilder.Name);
            }

            if (columnBuilder.InlineForeignKey is not null)
            {
                foreignKeys.Add(columnBuilder.InlineForeignKey);
            }
        }

        foreach (var columnBuilder in columnBuilders)
        {
            if (primaryKeyColumns.Contains(columnBuilder.Name))
            {
                columnBuilder.IsPrimaryKey = true;
            }
        }

        return new TableSchema
        {
            Schema = schemaName,
            Name = tableName,
            Columns = columnBuilders
                .Select(columnBuilder => columnBuilder.ToColumnSchema(options.TypeMapper))
                .ToArray(),
            ForeignKeys = foreignKeys,
            Indexes = indexes,
        };
    }

    private ColumnBuilder ParseColumnDefinition(string definition, string tableName)
    {
        var position = 0;
        var columnName = ReadIdentifier(definition, ref position);
        if (string.IsNullOrWhiteSpace(columnName))
        {
            throw new FormatException($"Unable to parse column name from definition '{definition}'.");
        }

        var typeDescriptor = ReadSqlType(definition, ref position);
        var remainingDefinition = definition[position..];
        var isPrimaryKey = PrimaryKeyRegex.IsMatch(remainingDefinition);
        var hasExplicitNull = ExplicitNullRegex.IsMatch(remainingDefinition);
        var isNullable = hasExplicitNull || !NotNullRegex.IsMatch(remainingDefinition);
        var isIdentity = options.IdentityRegex.IsMatch(remainingDefinition) || IsSerialType(typeDescriptor.StoreType);

        if (isPrimaryKey)
        {
            isNullable = false;
        }

        var columnBuilder = new ColumnBuilder
        {
            Name = columnName,
            StoreType = typeDescriptor.StoreType,
            MaxLength = typeDescriptor.MaxLength,
            Precision = typeDescriptor.Precision,
            Scale = typeDescriptor.Scale,
            IsNullable = isNullable,
            IsPrimaryKey = isPrimaryKey,
            IsIdentity = isIdentity,
        };

        var referencesMatch = ReferencesRegex.Match(remainingDefinition);
        if (referencesMatch.Success)
        {
            columnBuilder.InlineForeignKey = ParseForeignKeyDefinition(
                definition,
                tableName,
                [columnName],
                referencesMatch.Index + position);
        }

        return columnBuilder;
    }

    private SqlTypeDescriptor ReadSqlType(string definition, ref int position)
    {
        var typeStart = position;
        var typeParts = new List<string>();
        string? facetText = null;

        while (position < definition.Length)
        {
            SkipWhitespace(definition, ref position);

            if (position >= definition.Length)
            {
                break;
            }

            if (definition[position] == '(' && typeParts.Count > 0)
            {
                var facetEnd = FindMatchingParenthesis(definition, position);
                if (facetEnd < 0)
                {
                    throw new FormatException($"SQL type '{string.Join(' ', typeParts)}' has an unterminated facet list.");
                }

                facetText = definition[(position + 1)..facetEnd];
                position = facetEnd + 1;
                break;
            }

            var beforeIdentifier = position;
            var part = ReadIdentifier(definition, ref position);
            if (string.IsNullOrWhiteSpace(part))
            {
                break;
            }

            if (typeParts.Count > 0 && TypeTerminatorKeywords.Contains(part))
            {
                position = beforeIdentifier;
                break;
            }

            if (typeParts.Count == 0 && TypeTerminatorKeywords.Contains(part))
            {
                throw new FormatException($"Unable to parse SQL type from definition '{definition}'.");
            }

            typeParts.Add(part);
        }

        if (typeParts.Count == 0)
        {
            throw new FormatException($"Unable to parse SQL type from definition '{definition[typeStart..]}'.");
        }

        var storeType = NormalizeStoreType(string.Join(' ', typeParts));
        var maxLength = default(int?);
        var precision = default(byte?);
        var scale = default(byte?);

        if (!string.IsNullOrWhiteSpace(facetText))
        {
            var facets = SplitTopLevel(facetText, ',')
                .Select(static value => value.Trim())
                .Where(static value => value.Length > 0)
                .ToArray();

            if (facets.Length > 0)
            {
                if (options.LengthTypes.Contains(storeType))
                {
                    maxLength = ParseLengthFacet(facets[0]);
                }
                else if (options.PrecisionScaleTypes.Contains(storeType))
                {
                    precision = ParseByteFacet(facets[0]);

                    if (facets.Length > 1)
                    {
                        scale = ParseByteFacet(facets[1]);
                    }
                }
                else if (byte.TryParse(ReadLeadingNumber(facets[0]), NumberStyles.Integer, CultureInfo.InvariantCulture, out var singlePrecision))
                {
                    precision = singlePrecision;
                }
            }
        }

        return new SqlTypeDescriptor(storeType, maxLength, precision, scale);
    }

    private void ParseTableLevelDefinition(
        string definition,
        string tableName,
        ISet<string> primaryKeyColumns,
        ICollection<ForeignKeySchema> foreignKeys,
        ICollection<IndexSchema> indexes)
    {
        var primaryKeyMatch = PrimaryKeyRegex.Match(definition);
        if (primaryKeyMatch.Success)
        {
            foreach (var columnName in ReadColumnListAfter(definition, primaryKeyMatch.Index + primaryKeyMatch.Length))
            {
                primaryKeyColumns.Add(columnName);
            }
        }

        var foreignKeyMatch = ForeignKeyRegex.Match(definition);
        if (foreignKeyMatch.Success)
        {
            var dependentColumns = ReadColumnListAfter(
                definition,
                foreignKeyMatch.Index + foreignKeyMatch.Length);

            foreignKeys.Add(ParseForeignKeyDefinition(
                definition,
                tableName,
                dependentColumns,
                foreignKeyMatch.Index + foreignKeyMatch.Length));
        }

        var index = TryParseTableLevelIndex(definition, tableName);
        if (index is not null)
        {
            indexes.Add(index);
        }
    }

    private IndexSchema? TryParseTableLevelIndex(string definition, string tableName)
    {
        var position = 0;
        var firstIdentifier = ReadIdentifier(definition, ref position);
        var isUnique = string.Equals(firstIdentifier, "unique", StringComparison.OrdinalIgnoreCase);

        if (isUnique)
        {
            firstIdentifier = ReadIdentifier(definition, ref position);
        }

        if (!string.Equals(firstIdentifier, "index", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(firstIdentifier, "key", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var indexName = ReadIdentifier(definition, ref position) ?? $"IX_{tableName}";
        var columnStart = FindFirstOpeningParenthesis(definition, position);
        if (columnStart < 0)
        {
            return null;
        }

        var columnEnd = FindMatchingParenthesis(definition, columnStart);
        if (columnEnd < 0)
        {
            return null;
        }

        var columns = ParseColumnList(definition[(columnStart + 1)..columnEnd])
            .ToArray();

        return columns.Length == 0
            ? null
            : new IndexSchema
            {
                Name = indexName,
                IsUnique = isUnique,
                Columns = columns,
            };
    }

    private ForeignKeySchema ParseForeignKeyDefinition(
        string definition,
        string tableName,
        IReadOnlyList<string> dependentColumns,
        int searchStart)
    {
        var referencesMatch = ReferencesRegex.Match(definition, searchStart);
        if (!referencesMatch.Success)
        {
            throw new FormatException($"Foreign key definition is missing REFERENCES clause: '{definition}'.");
        }

        var constraintName = TryReadConstraintName(definition, referencesMatch.Index)
            ?? GenerateForeignKeyName(tableName, dependentColumns);

        var position = referencesMatch.Index + referencesMatch.Length;
        var principalNameParts = ReadQualifiedIdentifierParts(definition, ref position);
        if (principalNameParts.Count == 0)
        {
            throw new FormatException($"Unable to parse referenced table from definition '{definition}'.");
        }

        var (principalSchema, principalTable) = ResolveSchemaAndTableName(principalNameParts);
        var principalColumns = Array.Empty<string>();

        SkipWhitespace(definition, ref position);

        if (position < definition.Length && definition[position] == '(')
        {
            var principalColumnEnd = FindMatchingParenthesis(definition, position);
            if (principalColumnEnd < 0)
            {
                throw new FormatException($"Referenced column list is unterminated in definition '{definition}'.");
            }

            principalColumns = ParseColumnList(definition[(position + 1)..principalColumnEnd])
                .ToArray();
        }

        return new ForeignKeySchema
        {
            Name = constraintName,
            Columns = dependentColumns,
            PrincipalSchema = principalSchema,
            PrincipalTable = principalTable,
            PrincipalColumns = principalColumns,
        };
    }

    private IReadOnlyList<string> ReadColumnListAfter(string definition, int searchStart)
    {
        var columnListStart = FindFirstOpeningParenthesis(definition, searchStart);
        if (columnListStart < 0)
        {
            throw new FormatException($"Column list is missing from definition '{definition}'.");
        }

        var columnListEnd = FindMatchingParenthesis(definition, columnListStart);
        if (columnListEnd < 0)
        {
            throw new FormatException($"Column list is unterminated in definition '{definition}'.");
        }

        return ParseColumnList(definition[(columnListStart + 1)..columnListEnd])
            .ToArray();
    }

    private IEnumerable<string> ParseColumnList(string columnList)
    {
        foreach (var column in SplitTopLevel(columnList, ','))
        {
            var position = 0;
            var columnName = ReadIdentifier(column, ref position);

            if (!string.IsNullOrWhiteSpace(columnName))
            {
                yield return columnName;
            }
        }
    }

    private static bool IsTableLevelDefinition(string definition)
    {
        var position = 0;
        var firstIdentifier = ReadIdentifier(definition, ref position);

        return firstIdentifier is not null &&
            TableLevelDefinitionKeywords.Contains(firstIdentifier);
    }

    private (string Schema, string Table) ParseSchemaAndTableName(string tableNameText)
    {
        var position = 0;
        var parts = ReadQualifiedIdentifierParts(tableNameText, ref position);

        if (parts.Count == 0)
        {
            throw new FormatException($"Unable to parse table name from '{tableNameText}'.");
        }

        return ResolveSchemaAndTableName(parts);
    }

    private (string Schema, string Table) ResolveSchemaAndTableName(IReadOnlyList<string> nameParts)
    {
        return nameParts.Count switch
        {
            1 => (options.DefaultSchema, nameParts[0]),
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

        throw new FormatException($"Delimited identifier starting with '{openingDelimiter}' is unterminated.");
    }

    private static string? TryReadConstraintName(string definition, int searchEnd)
    {
        string? constraintName = null;

        foreach (Match constraintMatch in ConstraintRegex.Matches(definition[..searchEnd]))
        {
            var position = constraintMatch.Index + constraintMatch.Length;
            var candidate = ReadIdentifier(definition, ref position);

            if (!string.IsNullOrWhiteSpace(candidate))
            {
                constraintName = candidate;
            }
        }

        return constraintName;
    }

    private static string GenerateForeignKeyName(string tableName, IReadOnlyList<string> dependentColumns)
    {
        var columnSuffix = dependentColumns.Count == 0
            ? "Relationship"
            : string.Join("_", dependentColumns);

        return $"FK_{tableName}_{columnSuffix}";
    }

    private static int SkipOptionalCreateTableClauses(string text, int position)
    {
        var remainingText = text[position..].TrimStart();
        var skippedWhitespace = text[position..].Length - remainingText.Length;
        position += skippedWhitespace;

        if (remainingText.StartsWith("IF NOT EXISTS", StringComparison.OrdinalIgnoreCase))
        {
            position += "IF NOT EXISTS".Length;
        }

        return position;
    }

    private static int FindFirstOpeningParenthesis(string text, int startIndex)
    {
        var state = ParserState.None;

        for (var index = startIndex; index < text.Length; index++)
        {
            if (UpdateState(text, ref index, ref state))
            {
                continue;
            }

            if (state == ParserState.None && text[index] == '(')
            {
                return index;
            }
        }

        return -1;
    }

    private static int FindMatchingParenthesis(string text, int openingParenthesisIndex)
    {
        var depth = 0;
        var state = ParserState.None;

        for (var index = openingParenthesisIndex; index < text.Length; index++)
        {
            if (UpdateState(text, ref index, ref state))
            {
                continue;
            }

            if (state != ParserState.None)
            {
                continue;
            }

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
        var state = ParserState.None;

        for (var index = 0; index < text.Length; index++)
        {
            if (UpdateState(text, ref index, ref state))
            {
                continue;
            }

            if (state != ParserState.None)
            {
                continue;
            }

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

    private static string RemoveComments(string sql, bool supportsHashLineComments)
    {
        var builder = new StringBuilder(sql.Length);
        var state = ParserState.None;

        for (var index = 0; index < sql.Length; index++)
        {
            if (state == ParserState.None)
            {
                if (sql[index] == '-' && index + 1 < sql.Length && sql[index + 1] == '-')
                {
                    while (index < sql.Length && sql[index] is not '\r' and not '\n')
                    {
                        index++;
                    }

                    if (index < sql.Length)
                    {
                        builder.Append(sql[index]);
                    }

                    continue;
                }

                if (sql[index] == '/' && index + 1 < sql.Length && sql[index + 1] == '*')
                {
                    index += 2;

                    while (index + 1 < sql.Length && (sql[index] != '*' || sql[index + 1] != '/'))
                    {
                        index++;
                    }

                    index++;
                    continue;
                }

                if (supportsHashLineComments && sql[index] == '#')
                {
                    while (index < sql.Length && sql[index] is not '\r' and not '\n')
                    {
                        index++;
                    }

                    if (index < sql.Length)
                    {
                        builder.Append(sql[index]);
                    }

                    continue;
                }
            }

            builder.Append(sql[index]);
            UpdateState(sql, ref index, ref state);
        }

        return builder.ToString();
    }

    private static bool UpdateState(string text, ref int index, ref ParserState state)
    {
        var character = text[index];

        switch (state)
        {
            case ParserState.SingleQuotedString:
                if (character == '\'' && index + 1 < text.Length && text[index + 1] == '\'')
                {
                    index++;
                    return true;
                }

                if (character == '\'')
                {
                    state = ParserState.None;
                    return true;
                }

                return true;
            case ParserState.BracketedIdentifier:
                if (character == ']' && index + 1 < text.Length && text[index + 1] == ']')
                {
                    index++;
                    return true;
                }

                if (character == ']')
                {
                    state = ParserState.None;
                    return true;
                }

                return true;
            case ParserState.DoubleQuotedIdentifier:
                if (character == '"' && index + 1 < text.Length && text[index + 1] == '"')
                {
                    index++;
                    return true;
                }

                if (character == '"')
                {
                    state = ParserState.None;
                    return true;
                }

                return true;
            case ParserState.BacktickIdentifier:
                if (character == '`' && index + 1 < text.Length && text[index + 1] == '`')
                {
                    index++;
                    return true;
                }

                if (character == '`')
                {
                    state = ParserState.None;
                    return true;
                }

                return true;
            case ParserState.None:
                switch (character)
                {
                    case '\'':
                        state = ParserState.SingleQuotedString;
                        return true;
                    case '[':
                        state = ParserState.BracketedIdentifier;
                        return true;
                    case '"':
                        state = ParserState.DoubleQuotedIdentifier;
                        return true;
                    case '`':
                        state = ParserState.BacktickIdentifier;
                        return true;
                }

                return false;
            default:
                return false;
        }
    }

    private static void SkipWhitespace(string text, ref int position)
    {
        while (position < text.Length && char.IsWhiteSpace(text[position]))
        {
            position++;
        }
    }

    private static string NormalizeStoreType(string storeType)
    {
        return string.Join(
            ' ',
            storeType
                .Replace("[", string.Empty, StringComparison.Ordinal)
                .Replace("]", string.Empty, StringComparison.Ordinal)
                .Replace("\"", string.Empty, StringComparison.Ordinal)
                .Replace("`", string.Empty, StringComparison.Ordinal)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .ToLowerInvariant();
    }

    private static bool IsSerialType(string storeType)
    {
        return storeType.Equals("serial", StringComparison.OrdinalIgnoreCase) ||
            storeType.Equals("bigserial", StringComparison.OrdinalIgnoreCase) ||
            storeType.Equals("smallserial", StringComparison.OrdinalIgnoreCase) ||
            storeType.Equals("serial2", StringComparison.OrdinalIgnoreCase) ||
            storeType.Equals("serial4", StringComparison.OrdinalIgnoreCase) ||
            storeType.Equals("serial8", StringComparison.OrdinalIgnoreCase);
    }

    private static int? ParseLengthFacet(string facet)
    {
        if (facet.Equals("max", StringComparison.OrdinalIgnoreCase))
        {
            return -1;
        }

        return int.TryParse(ReadLeadingNumber(facet), NumberStyles.Integer, CultureInfo.InvariantCulture, out var length)
            ? length
            : null;
    }

    private static byte? ParseByteFacet(string facet)
    {
        return byte.TryParse(ReadLeadingNumber(facet), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private static string ReadLeadingNumber(string value)
    {
        var trimmedValue = value.Trim();
        var end = 0;

        while (end < trimmedValue.Length && char.IsDigit(trimmedValue[end]))
        {
            end++;
        }

        return end == 0 ? trimmedValue : trimmedValue[..end];
    }

    private enum ParserState
    {
        None,
        SingleQuotedString,
        BracketedIdentifier,
        DoubleQuotedIdentifier,
        BacktickIdentifier,
    }

    private sealed record SqlTypeDescriptor(
        string StoreType,
        int? MaxLength,
        byte? Precision,
        byte? Scale);

    private sealed class ColumnBuilder
    {
        public required string Name { get; init; }

        public required string StoreType { get; init; }

        public int? MaxLength { get; init; }

        public byte? Precision { get; init; }

        public byte? Scale { get; init; }

        public bool IsNullable { get; init; } = true;

        public bool IsPrimaryKey { get; set; }

        public bool IsIdentity { get; init; }

        public ForeignKeySchema? InlineForeignKey { get; set; }

        public ColumnSchema ToColumnSchema(ISqlTypeMapper typeMapper)
        {
            var isNullable = IsPrimaryKey ? false : IsNullable;
            var csharpType = typeMapper is IColumnTypeMapper columnTypeMapper
                ? columnTypeMapper.MapToCSharpType(StoreType, isNullable, MaxLength, Precision, Scale)
                : typeMapper.MapToCSharpType(StoreType, isNullable);

            return new ColumnSchema
            {
                Name = Name,
                StoreType = StoreType,
                CSharpType = csharpType,
                IsNullable = isNullable,
                IsPrimaryKey = IsPrimaryKey,
                IsIdentity = IsIdentity,
                MaxLength = MaxLength,
                Precision = Precision,
                Scale = Scale,
            };
        }
    }
}
