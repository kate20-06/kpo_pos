
using System.Globalization;
using System.Text;
using POS.Application;

namespace POS.Infrastructure;

public class CsvReportExporter : IReportExporter
{
    public void Export(GroupReport report, string filePath)
    {
        var csv = new StringBuilder();

        var criteria = report.Students
            .SelectMany(s => s.Averages.Keys)
            .Distinct()
            .ToList();

        var header = new List<string>
        {
            "Имя",
            "Фамилия"
        };

        header.AddRange(criteria);
        header.Add("Итог (%)");

        csv.AppendLine(ToCsvRow(header));

        foreach (var student in report.Students)
        {
            var row = new List<string>
            {
                student.Student.Name,
                student.Student.Surname
            };

            foreach (var criterion in criteria)
            {
                student.Averages.TryGetValue(
                    criterion, out double? average);

                row.Add(FormatScore(average));
            }

            row.Add(FormatScore(student.FinalScore));

            csv.AppendLine(ToCsvRow(row));
        }

        File.WriteAllText(
            filePath,
            csv.ToString(),
            new UTF8Encoding(true)
        );
    }

    private static string FormatScore(double? score)
    {
        return score?.ToString(
            "F2",
            CultureInfo.InvariantCulture
        ) ?? "";
    }

    private static string ToCsvRow(IEnumerable<string> values)
    {
        return string.Join(";", values.Select(Escape));
    }

    private static string Escape(string value)
    {
        if (!string.IsNullOrEmpty(value) && ("=+-@".Contains(value[0]) || value[0] == '\t' || value[0] == '\r' || value[0] == '\n'))
            value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
