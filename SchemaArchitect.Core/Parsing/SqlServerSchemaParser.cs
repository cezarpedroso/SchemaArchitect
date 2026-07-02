using System.Text;
using System.Text.RegularExpressions;
using SchemaArchitect.Core.Interfaces;
using SchemaArchitect.Core.Models;
using SchemaArchitect.Core.Services;

namespace SchemaArchitect.Core.Parsing;

/// <summary>
/// Parses SQL Server schema scripts into Schema Architect schema models.
/// </summary>
public sealed class SqlServerSchemaParser : ISchemaParser
{
    private static readonly Regex CreateTableRegex = new(
        @"\bCREATE\s+TABLE\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex IdentityRegex = new(
        @"\bIDENTITY\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex NotNullRegex = new(
        @"\bNOT\s+NULL\b",
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

    private static readonly HashSet<string> LengthTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "nvarchar",
        "varchar",
        "nchar",
        "char",
        "varbinary",
        "binary",
    };

    private static readonly HashSet<string> PrecisionScaleTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "decimal",
        "numeric",
    };

    private readonly ISqlTypeMapper typeMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerSchemaParser"/> class.
    /// </summary>
    public SqlServerSchemaParser()
        : this(new SqlServerTypeMapper())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerSchemaParser"/> class.
    /// </summary>
    /// <param name="typeMapper">The SQL Server type mapper used for C# type metadata.</param>
    public SqlServerSchemaParser(ISqlTypeMapper typeMapper)
    {
        this.typeMapper = typeMapper ?? throw new ArgumentNullException(nameof(typeMapper));
    }

    /// <inheritdoc />
    public Task<DatabaseSchema> ParseAsync(
        string schemaSql,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(schemaSql);

        cancellationToken.ThrowIfCancellationRequested();

        var sanitizedSql = RemoveComments(schemaSql);
        var tables = SqlIndexParser.AttachStandaloneIndexes(
            sanitizedSql,
            ParseCreateTableStatements(sanitizedSql, cancellationToken),
            "dbo");

        var schema = new DatabaseSchema
        {
            Dialect = SqlDialect.SqlServer,
            Tables = tables,
        };

        return Task.FromResult(schema);
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

            var tableNameStart = match.Index + match.Length;
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

        foreach (var definition in SplitTopLevel(body, ','))
        {
            var trimmedDefinition = definition.Trim();
            if (trimmedDefinition.Length == 0)
            {
                continue;
            }

            if (IsTableLevelDefinition(trimmedDefinition))
            {
                ParseTableLevelDefinition(
                    trimmedDefinition,
                    tableName,
                    primaryKeyColumns,
                    foreignKeys);

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
                .Select(columnBuilder => columnBuilder.ToColumnSchema(typeMapper))
                .ToArray(),
            ForeignKeys = foreignKeys,
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
        var isNullable = !NotNullRegex.IsMatch(remainingDefinition);

        var columnBuilder = new ColumnBuilder
        {
            Name = columnName,
            StoreType = typeDescriptor.StoreType,
            MaxLength = typeDescriptor.MaxLength,
            Precision = typeDescriptor.Precision,
            Scale = typeDescriptor.Scale,
            IsNullable = isPrimaryKey ? false : isNullable,
            IsPrimaryKey = isPrimaryKey,
            IsIdentity = IdentityRegex.IsMatch(remainingDefinition),
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

    private static SqlTypeDescriptor ReadSqlType(string definition, ref int position)
    {
        var storeType = ReadIdentifier(definition, ref position);
        if (string.IsNullOrWhiteSpace(storeType))
        {
            throw new FormatException($"Unable to parse SQL type from definition '{definition}'.");
        }

        SkipWhitespace(definition, ref position);

        string? facetText = null;
        if (position < definition.Length && definition[position] == '(')
        {
            var facetEnd = FindMatchingParenthesis(definition, position);
            if (facetEnd < 0)
            {
                throw new FormatException($"SQL type '{storeType}' has an unterminated facet list.");
            }

            facetText = definition[(position + 1)..facetEnd];
            position = facetEnd + 1;
        }

        var maxLength = default(int?);
        var precision = default(byte?);
        var scale = default(byte?);

        if (!string.IsNullOrWhiteSpace(facetText))
        {
            var facets = SplitTopLevel(facetText, ',')
                .Select(static value => value.Trim())
                .ToArray();

            if (LengthTypes.Contains(storeType))
            {
                maxLength = string.Equals(facets[0], "max", StringComparison.OrdinalIgnoreCase)
                    ? -1
                    : int.Parse(facets[0]);
            }
            else if (PrecisionScaleTypes.Contains(storeType))
            {
                precision = byte.Parse(facets[0]);

                if (facets.Length > 1)
                {
                    scale = byte.Parse(facets[1]);
                }
            }
            else if (byte.TryParse(facets[0], out var singlePrecision))
            {
                precision = singlePrecision;
            }
        }

        return new SqlTypeDescriptor(
            storeType,
            maxLength,
            precision,
            scale);
    }

    private static void ParseTableLevelDefinition(
        string definition,
        string tableName,
        ISet<string> primaryKeyColumns,
        ICollection<ForeignKeySchema> foreignKeys)
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
    }

    private static ForeignKeySchema ParseForeignKeyDefinition(
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

    private static IReadOnlyList<string> ReadColumnListAfter(string definition, int searchStart)
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

    private static IEnumerable<string> ParseColumnList(string columnList)
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
        return StartsWithKeyword(definition, "CONSTRAINT") ||
            StartsWithKeyword(definition, "PRIMARY") ||
            StartsWithKeyword(definition, "FOREIGN") ||
            StartsWithKeyword(definition, "UNIQUE") ||
            StartsWithKeyword(definition, "CHECK") ||
            StartsWithKeyword(definition, "INDEX");
    }

    private static (string Schema, string Table) ParseSchemaAndTableName(string tableNameText)
    {
        var position = 0;
        var parts = ReadQualifiedIdentifierParts(tableNameText, ref position);

        if (parts.Count == 0)
        {
            throw new FormatException($"Unable to parse table name from '{tableNameText}'.");
        }

        return ResolveSchemaAndTableName(parts);
    }

    private static (string Schema, string Table) ResolveSchemaAndTableName(IReadOnlyList<string> nameParts)
    {
        return nameParts.Count switch
        {
            1 => ("dbo", nameParts[0]),
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

        if (text[position] == '[')
        {
            return ReadBracketedIdentifier(text, ref position);
        }

        if (text[position] == '"')
        {
            return ReadQuotedIdentifier(text, ref position);
        }

        var start = position;

        while (position < text.Length &&
            !char.IsWhiteSpace(text[position]) &&
            text[position] is not '(' and not ')' and not ',' and not '.')
        {
            position++;
        }

        return start == position
            ? null
            : text[start..position].Trim();
    }

    private static string ReadBracketedIdentifier(string text, ref int position)
    {
        var builder = new StringBuilder();
        position++;

        while (position < text.Length)
        {
            if (text[position] == ']')
            {
                if (position + 1 < text.Length && text[position + 1] == ']')
                {
                    builder.Append(']');
                    position += 2;
                    continue;
                }

                position++;
                return builder.ToString();
            }

            builder.Append(text[position]);
            position++;
        }

        throw new FormatException("Bracketed identifier is unterminated.");
    }

    private static string ReadQuotedIdentifier(string text, ref int position)
    {
        var builder = new StringBuilder();
        position++;

        while (position < text.Length)
        {
            if (text[position] == '"')
            {
                if (position + 1 < text.Length && text[position + 1] == '"')
                {
                    builder.Append('"');
                    position += 2;
                    continue;
                }

                position++;
                return builder.ToString();
            }

            builder.Append(text[position]);
            position++;
        }

        throw new FormatException("Quoted identifier is unterminated.");
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

    private static bool StartsWithKeyword(string text, string keyword)
    {
        var position = 0;
        SkipWhitespace(text, ref position);

        if (position + keyword.Length > text.Length)
        {
            return false;
        }

        if (!text.AsSpan(position, keyword.Length).Equals(keyword, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var end = position + keyword.Length;
        return end == text.Length || !IsIdentifierCharacter(text[end]);
    }

    private static bool IsIdentifierCharacter(char character)
    {
        return char.IsLetterOrDigit(character) || character is '_' or '@' or '#';
    }

    private static int FindFirstOpeningParenthesis(string text, int startIndex)
    {
        var inBracketedIdentifier = false;
        var inQuotedIdentifier = false;
        var inStringLiteral = false;

        for (var index = startIndex; index < text.Length; index++)
        {
            var character = text[index];

            if (inStringLiteral)
            {
                if (character == '\'' && index + 1 < text.Length && text[index + 1] == '\'')
                {
                    index++;
                }
                else if (character == '\'')
                {
                    inStringLiteral = false;
                }

                continue;
            }

            if (inBracketedIdentifier)
            {
                if (character == ']' && index + 1 < text.Length && text[index + 1] == ']')
                {
                    index++;
                }
                else if (character == ']')
                {
                    inBracketedIdentifier = false;
                }

                continue;
            }

            if (inQuotedIdentifier)
            {
                if (character == '"' && index + 1 < text.Length && text[index + 1] == '"')
                {
                    index++;
                }
                else if (character == '"')
                {
                    inQuotedIdentifier = false;
                }

                continue;
            }

            switch (character)
            {
                case '\'':
                    inStringLiteral = true;
                    break;
                case '[':
                    inBracketedIdentifier = true;
                    break;
                case '"':
                    inQuotedIdentifier = true;
                    break;
                case '(':
                    return index;
            }
        }

        return -1;
    }

    private static int FindMatchingParenthesis(string text, int openingParenthesisIndex)
    {
        var depth = 0;
        var inBracketedIdentifier = false;
        var inQuotedIdentifier = false;
        var inStringLiteral = false;

        for (var index = openingParenthesisIndex; index < text.Length; index++)
        {
            var character = text[index];

            if (inStringLiteral)
            {
                if (character == '\'' && index + 1 < text.Length && text[index + 1] == '\'')
                {
                    index++;
                }
                else if (character == '\'')
                {
                    inStringLiteral = false;
                }

                continue;
            }

            if (inBracketedIdentifier)
            {
                if (character == ']' && index + 1 < text.Length && text[index + 1] == ']')
                {
                    index++;
                }
                else if (character == ']')
                {
                    inBracketedIdentifier = false;
                }

                continue;
            }

            if (inQuotedIdentifier)
            {
                if (character == '"' && index + 1 < text.Length && text[index + 1] == '"')
                {
                    index++;
                }
                else if (character == '"')
                {
                    inQuotedIdentifier = false;
                }

                continue;
            }

            switch (character)
            {
                case '\'':
                    inStringLiteral = true;
                    break;
                case '[':
                    inBracketedIdentifier = true;
                    break;
                case '"':
                    inQuotedIdentifier = true;
                    break;
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
        var inBracketedIdentifier = false;
        var inQuotedIdentifier = false;
        var inStringLiteral = false;

        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];

            if (inStringLiteral)
            {
                if (character == '\'' && index + 1 < text.Length && text[index + 1] == '\'')
                {
                    index++;
                }
                else if (character == '\'')
                {
                    inStringLiteral = false;
                }

                continue;
            }

            if (inBracketedIdentifier)
            {
                if (character == ']' && index + 1 < text.Length && text[index + 1] == ']')
                {
                    index++;
                }
                else if (character == ']')
                {
                    inBracketedIdentifier = false;
                }

                continue;
            }

            if (inQuotedIdentifier)
            {
                if (character == '"' && index + 1 < text.Length && text[index + 1] == '"')
                {
                    index++;
                }
                else if (character == '"')
                {
                    inQuotedIdentifier = false;
                }

                continue;
            }

            switch (character)
            {
                case '\'':
                    inStringLiteral = true;
                    break;
                case '[':
                    inBracketedIdentifier = true;
                    break;
                case '"':
                    inQuotedIdentifier = true;
                    break;
                case '(':
                    depth++;
                    break;
                case ')':
                    depth--;
                    break;
                default:
                    if (character == separator && depth == 0)
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

    private static string RemoveComments(string sql)
    {
        var builder = new StringBuilder(sql.Length);
        var inBracketedIdentifier = false;
        var inQuotedIdentifier = false;
        var inStringLiteral = false;

        for (var index = 0; index < sql.Length; index++)
        {
            var character = sql[index];

            if (inStringLiteral)
            {
                builder.Append(character);

                if (character == '\'' && index + 1 < sql.Length && sql[index + 1] == '\'')
                {
                    builder.Append(sql[index + 1]);
                    index++;
                }
                else if (character == '\'')
                {
                    inStringLiteral = false;
                }

                continue;
            }

            if (inBracketedIdentifier)
            {
                builder.Append(character);

                if (character == ']' && index + 1 < sql.Length && sql[index + 1] == ']')
                {
                    builder.Append(sql[index + 1]);
                    index++;
                }
                else if (character == ']')
                {
                    inBracketedIdentifier = false;
                }

                continue;
            }

            if (inQuotedIdentifier)
            {
                builder.Append(character);

                if (character == '"' && index + 1 < sql.Length && sql[index + 1] == '"')
                {
                    builder.Append(sql[index + 1]);
                    index++;
                }
                else if (character == '"')
                {
                    inQuotedIdentifier = false;
                }

                continue;
            }

            if (character == '-' && index + 1 < sql.Length && sql[index + 1] == '-')
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

            if (character == '/' && index + 1 < sql.Length && sql[index + 1] == '*')
            {
                index += 2;

                while (index + 1 < sql.Length && (sql[index] != '*' || sql[index + 1] != '/'))
                {
                    index++;
                }

                index++;
                continue;
            }

            builder.Append(character);

            switch (character)
            {
                case '\'':
                    inStringLiteral = true;
                    break;
                case '[':
                    inBracketedIdentifier = true;
                    break;
                case '"':
                    inQuotedIdentifier = true;
                    break;
            }
        }

        return builder.ToString();
    }

    private static void SkipWhitespace(string text, ref int position)
    {
        while (position < text.Length && char.IsWhiteSpace(text[position]))
        {
            position++;
        }
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

            return new ColumnSchema
            {
                Name = Name,
                StoreType = StoreType,
                CSharpType = typeMapper.MapToCSharpType(StoreType, isNullable),
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
