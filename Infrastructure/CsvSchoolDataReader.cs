using System.Globalization;
using Microsoft.VisualBasic.FileIO;
using POS.Application;

namespace POS.Infrastructure;

public class CsvSchoolDataReader : ISchoolDataReader
{
    private const long MaxFileBytes = 5 * 1024 * 1024;
    private const int MaxRecords = 10000;

    public IReadOnlyList<StudentImportRow> ReadStudents(string path) =>
        ReadRows(path, new[] { "Name", "Surname", "Group" }, (fields, _) =>
            new StudentImportRow(Required(fields[0], "Name"),
                Required(fields[1], "Surname"), Required(fields[2], "Group")));

    public IReadOnlyList<TeacherImportRow> ReadTeachers(string path) =>
        ReadRows(path, new[] { "Name", "Surname", "Groups" }, (fields, _) =>
        {
            var groups = Required(fields[2], "Groups")
                .Split('|', StringSplitOptions.TrimEntries);
            if (groups.Any(string.IsNullOrWhiteSpace))
                throw new FormatException("Список групп содержит пустое название");
            return new TeacherImportRow(Required(fields[0], "Name"),
                Required(fields[1], "Surname"), groups);
        });

    public IReadOnlyList<CriterionImportRow> ReadCriteria(string path) =>
        ReadRows(path, new[] { "Name", "Text", "MaxScore", "Weight" }, (fields, _) =>
        {
            if (!int.TryParse(fields[2], NumberStyles.Integer,
                CultureInfo.InvariantCulture, out int maxScore) || maxScore <= 0)
                throw new FormatException("MaxScore должен быть положительным целым числом");
            var weightText = fields[3].Replace(',', '.');
            if (!double.TryParse(weightText, NumberStyles.Float,
                CultureInfo.InvariantCulture, out double weight) ||
                !double.IsFinite(weight) || weight <= 0)
                throw new FormatException("Weight должен быть конечным положительным числом");
            return new CriterionImportRow(Required(fields[0], "Name"),
                Required(fields[1], "Text"), maxScore, weight);
        });

    private static string Required(string text, string column)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new FormatException($"Поле {column} не может быть пустым");
        return text.Trim();
    }

    private static List<T> ReadRows<T>(string path, string[] columns,
        Func<string[], long, T> parse)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Путь к файлу не задан");
        if (!File.Exists(path))
            throw new FileNotFoundException("Файл не найден", path);
        if (new FileInfo(path).Length > MaxFileBytes)
            throw new FormatException("Файл слишком большой (максимум 5 МБ)");

        using var parser = new TextFieldParser(path);
        parser.TextFieldType = FieldType.Delimited;
        parser.SetDelimiters(";");
        parser.HasFieldsEnclosedInQuotes = true;
        parser.TrimWhiteSpace = true;

        try
        {
            var header = parser.ReadFields();
            if (header == null || header.Length != columns.Length ||
                !header.Zip(columns).All(pair =>
                    string.Equals(pair.First.Trim().TrimStart('\uFEFF'), pair.Second,
                        StringComparison.OrdinalIgnoreCase)))
                throw new FormatException($"Неверный заголовок CSV. Ожидается: {string.Join(';', columns)}");

            var result = new List<T>();
            while (!parser.EndOfData)
            {
                var line = parser.LineNumber;
                var fields = parser.ReadFields();
                if (fields == null || fields.Length != columns.Length)
                    throw new FormatException($"Строка {line}: ожидается {columns.Length} полей");
                if (result.Count >= MaxRecords)
                    throw new FormatException($"Слишком много строк (максимум {MaxRecords})");
                try
                {
                    result.Add(parse(fields, line));
                }
                catch (FormatException ex)
                {
                    throw new FormatException($"Строка {line}: {ex.Message}", ex);
                }
            }
            return result;
        }
        catch (MalformedLineException ex)
        {
            throw new FormatException($"Некорректный CSV, строка {ex.LineNumber}: {ex.Message}", ex);
        }
    }
}
