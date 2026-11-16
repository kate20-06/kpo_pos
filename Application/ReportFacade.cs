
using POS.Domain;

namespace POS.Application;

public record GradeDetail(
    string CriterionName,
    string TeacherName,
    int Score
);

public record StudentReport(
    Student Student,
    IReadOnlyList<GradeDetail> Grades,
    Dictionary<string, double?> Averages,
    double? FinalScore
);

public record GroupReport(
    Group Group,
    IReadOnlyList<StudentReport> Students
);

public class ReportFacade
{
    private readonly ISchoolData _school;
    private readonly Rubric _rubric;
    private readonly IStudentEvaluationRepository _evaluations;
    private readonly EvaluationCalculator _calculator;
    private readonly IReportExporter _exporter;

    public ReportFacade(
        ISchoolData school,
        Rubric rubric,
        IStudentEvaluationRepository evaluations,
        EvaluationCalculator calculator,
        IReportExporter exporter)
    {
        _school = school;
        _rubric = rubric;
        _evaluations = evaluations;
        _calculator = calculator;
        _exporter = exporter;
    }

    public StudentReport GetStudentReport(
        string name,
        string surname,
        string groupName)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(surname))
            throw new ArgumentException("Имя и фамилия студента обязательны");
        var group = FindGroup(groupName);

        var students = _school.Students
            .Where(s =>
                s.GroupId == group.Id &&
                string.Equals(s.Name, name.Trim(),
                    StringComparison.OrdinalIgnoreCase) &&
                string.Equals(s.Surname, surname.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (students.Count == 0)
            throw new ArgumentException(
                "Студент не найден");

        if (students.Count > 1)
            throw new InvalidOperationException(
                "Найдено несколько студентов с такими данными");

        return BuildStudentReport(students[0]);
    }

    public GroupReport GetGroupReport(string groupName)
    {
        var group = FindGroup(groupName);

        var reports = _school.Students
            .Where(s => s.GroupId == group.Id)
            .OrderBy(s => s.Surname)
            .ThenBy(s => s.Name)
            .Select(BuildStudentReport)
            .ToList();

        return new GroupReport(group, reports);
    }

    private Group FindGroup(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
            throw new ArgumentException("Название группы не может быть пустым");
        return _school.Groups.FirstOrDefault(g =>
            string.Equals(g.Name, groupName.Trim(),
                StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException(
                "Группа не найдена");
    }

    private StudentReport BuildStudentReport(Student student)
    {
        var evaluation = _evaluations.GetByStudentId(student.Id)
            ?? new StudentEvaluation(student.Id);

        var grades = evaluation.Grades.Select(g =>
        {
            var teacher = _school.Teachers.FirstOrDefault(t => t.Id == g.TeacherId);
            return new GradeDetail(g.Criterion.Name,
                teacher == null ? "Неизвестный преподаватель" : $"{teacher.Name} {teacher.Surname}",
                g.Score);
        }).ToList();

        return new StudentReport(
            student,
            grades,
            _calculator.CalculateAverages(evaluation, _rubric),
            _calculator.CalculateFinalScore(evaluation, _rubric)
        );
    }

    public void ExportGroupReport(
        string groupName,
        string filePath)
    {
        var report = GetGroupReport(groupName);

        _exporter.Export(report, filePath);
    }
}
