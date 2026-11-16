using POS.Application;

namespace POS.ConsoleUI;

public static class ReportPrinter
{
    public static void Print(StudentReport report)
    {
        Console.WriteLine($"\n{report.Student.Name} {report.Student.Surname}");
        if (report.Grades.Count == 0)
            Console.WriteLine("Оценки ещё не выставлены.");
        else
        {
            Console.WriteLine("Выставленные оценки:");
            foreach (var grade in report.Grades)
                Console.WriteLine($"{grade.CriterionName}: {grade.Score} ({grade.TeacherName})");
        }
        Console.WriteLine("Средние по критериям:");
        foreach (var (criterion, average) in report.Averages)
            Console.WriteLine($"{criterion}: {(average.HasValue ? average.Value.ToString("F2") : "—")}");
        Console.WriteLine($"Итог: {(report.FinalScore.HasValue ? $"{report.FinalScore.Value:F2}%" : "—")}");
    }
}
