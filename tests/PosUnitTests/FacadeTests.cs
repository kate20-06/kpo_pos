using POS.Application;
using POS.Domain;
using POS.Infrastructure;
using Xunit;

namespace PosUnitTests;

public class TeacherFacadeTests
{
    private static (InMemorySchoolData school, Rubric rubric, InMemoryStudentEvaluationRepository repo,
        TeacherFacade facade, Teacher teacher, Student student, Criterion criterion) Build()
    {
        var school = new InMemorySchoolData();
        var group = school.GetOrCreateGroup("G");
        var student = new Student(Guid.NewGuid(), "Анна", "Иванова", group.Id);
        school.AddStudent(student);
        var teacher = new Teacher(Guid.NewGuid(), "Пётр", "Петров");
        teacher.AddGroup(group.Id);
        school.AddTeacher(teacher);
        var rubric = new Rubric();
        var criterion = new Criterion("A", "Описание", 10, 1);
        rubric.AddCriterion(criterion);
        var repo = new InMemoryStudentEvaluationRepository();
        return (school, rubric, repo, new TeacherFacade(school, rubric, repo), teacher, student, criterion);
    }

    [Fact]
    public void Session_ReturnsOnlyStudentsFromTeacherGroups()
    {
        var (school, _, _, facade, _, _, _) = Build();
        var otherGroup = school.GetOrCreateGroup("Other");
        school.AddStudent(new Student(Guid.NewGuid(), "Борис", "Петров", otherGroup.Id));
        var session = facade.GetEvaluationSession(" пЁТР ", " петров ");
        Assert.Equal("Пётр", session.Teacher.Name);
        Assert.Single(session.Students);
        Assert.Single(session.Criteria);
        Assert.Equal("Анна", session.Students[0].Name);
        Assert.Throws<ArgumentException>(() => facade.GetEvaluationSession("Несуществующий", "Человек"));
        Assert.Throws<ArgumentException>(() => facade.GetEvaluationSession("", "Человек"));
    }

    [Fact]
    public void SubmitGrades_PersistsScoresAndUnratedCriteriaChanges()
    {
        var (_, rubric, repo, facade, teacher, student, _) = Build();
        var b = new Criterion("B", "Описание", 20, 2);
        rubric.AddCriterion(b);
        Assert.Equal(2, facade.GetUnratedCriteria(teacher.Id, student.Id).Count);
        facade.SubmitGrades(teacher.Id, student.Id, new() { ["a"] = 0 });
        var grades = repo.GetByStudentId(student.Id)!.Grades;
        Assert.Single(grades);
        Assert.Equal(0, grades[0].Score);
        Assert.Single(facade.GetUnratedCriteria(teacher.Id, student.Id));
        Assert.Equal("B", facade.GetUnratedCriteria(teacher.Id, student.Id)[0].Name);
        facade.SubmitGrades(teacher.Id, student.Id, new() { ["B"] = 20 });
        Assert.Empty(facade.GetUnratedCriteria(teacher.Id, student.Id));
        Assert.Equal(2, repo.GetByStudentId(student.Id)!.Grades.Count);
    }

    [Fact]
    public void SubmitGrades_RejectsInvalidBatchWithoutMutatingExistingAggregate()
    {
        var (_, rubric, repo, facade, teacher, student, _) = Build();
        rubric.AddCriterion(new Criterion("B", "Описание", 10, 1));
        facade.SubmitGrades(teacher.Id, student.Id, new() { ["A"] = 5 });
        var current = repo.GetByStudentId(student.Id)!;
        Assert.Throws<ArgumentOutOfRangeException>(() => facade.SubmitGrades(
            teacher.Id, student.Id, new() { ["B"] = 11 }));
        Assert.Single(current.Grades);
        Assert.Throws<InvalidOperationException>(() => facade.SubmitGrades(
            teacher.Id, student.Id, new() { ["B"] = 3, ["A"] = 6 }));
        Assert.Single(current.Grades);
        Assert.Throws<ArgumentException>(() => facade.SubmitGrades(
            teacher.Id, student.Id, new() { ["B"] = 3, ["Unknown"] = 4 }));
        Assert.Single(current.Grades);
    }

    [Fact]
    public void SubmitGrades_RejectsCaseInsensitiveDuplicateWithinRequest()
    {
        var (_, _, repo, facade, teacher, student, _) = Build();
        var scores = new Dictionary<string, int> { ["A"] = 2, ["a"] = 3 };
        Assert.Throws<ArgumentException>(() => facade.SubmitGrades(teacher.Id, student.Id, scores));
        Assert.Null(repo.GetByStudentId(student.Id));
    }

    [Fact]
    public void SubmitGrades_RejectsUnknownPeopleAndTeacherOutsideGroup()
    {
        var (school, _, repo, facade, teacher, student, _) = Build();
        Assert.Throws<ArgumentException>(() => facade.SubmitGrades(Guid.NewGuid(), student.Id, new() { ["A"] = 3 }));
        Assert.Throws<ArgumentException>(() => facade.SubmitGrades(teacher.Id, Guid.NewGuid(), new() { ["A"] = 3 }));
        Assert.Throws<ArgumentException>(() => facade.SubmitGrades(teacher.Id, student.Id, new()));
        Assert.Throws<ArgumentException>(() => facade.SubmitGrades(teacher.Id, student.Id, null!));
        var other = school.GetOrCreateGroup("H");
        var foreignStudent = new Student(Guid.NewGuid(), "Борис", "Ф", other.Id);
        school.AddStudent(foreignStudent);
        Assert.Throws<InvalidOperationException>(() => facade.SubmitGrades(teacher.Id, foreignStudent.Id, new() { ["A"] = 3 }));
        Assert.Throws<InvalidOperationException>(() => facade.GetUnratedCriteria(teacher.Id, foreignStudent.Id));
        Assert.Throws<ArgumentException>(() => facade.GetUnratedCriteria(Guid.NewGuid(), student.Id));
        Assert.Null(repo.GetByStudentId(student.Id));
    }

    [Fact]
    public void MultipleTeachers_CanGradeSameCriterion()
    {
        var (school, _, repo, facade, teacher, student, _) = Build();
        var second = new Teacher(Guid.NewGuid(), "Второй", "Учитель");
        second.AddGroup(student.GroupId);
        school.AddTeacher(second);
        facade.SubmitGrades(teacher.Id, student.Id, new() { ["A"] = 10 });
        facade.SubmitGrades(second.Id, student.Id, new() { ["A"] = 0 });
        Assert.Equal(2, repo.GetByStudentId(student.Id)!.Grades.Count);
    }
}

public class ReportFacadeTests
{
    private static (InMemorySchoolData school, Rubric rubric, InMemoryStudentEvaluationRepository repo,
        ReportFacade facade, SpyExporter exporter, Student student, Teacher teacher, Criterion criterion) Build()
    {
        var school = new InMemorySchoolData();
        var group = school.GetOrCreateGroup("G");
        var student = new Student(Guid.NewGuid(), "Анна", "Иванова", group.Id);
        school.AddStudent(student);
        var teacher = new Teacher(Guid.NewGuid(), "Пётр", "Петров");
        teacher.AddGroup(group.Id);
        school.AddTeacher(teacher);
        var rubric = new Rubric();
        var criterion = new Criterion("A", "B", 10, 1);
        rubric.AddCriterion(criterion);
        var repo = new InMemoryStudentEvaluationRepository();
        var exporter = new SpyExporter();
        var facade = new ReportFacade(school, rubric, repo, new EvaluationCalculator(), exporter);
        return (school, rubric, repo, facade, exporter, student, teacher, criterion);
    }

    [Fact]
    public void NoGrades_ReportReturnsNullAndMissingEntries()
    {
        var (_, _, _, facade, _, _, _, _) = Build();
        var report = facade.GetStudentReport(" анна ", " ИВАНОВА ", "g");
        Assert.Empty(report.Grades);
        Assert.Null(report.FinalScore);
        Assert.Null(report.Averages["A"]);
    }

    [Fact]
    public void Report_ContainsRawGradesTeacherNamesAndCalculatedResult()
    {
        var (_, rubric, repo, facade, _, student, teacher, criterion) = Build();
        var evaluation = new StudentEvaluation(student.Id);
        evaluation.AddGrade(new CriterionGrade(criterion, teacher.Id, 7), rubric);
        evaluation.AddGrade(new CriterionGrade(criterion, Guid.NewGuid(), 3), rubric);
        repo.Save(evaluation);
        var report = facade.GetStudentReport("Анна", "Иванова", "G");
        Assert.Equal(2, report.Grades.Count);
        Assert.Contains(report.Grades, grade => grade.TeacherName == "Пётр Петров" && grade.Score == 7);
        Assert.Contains(report.Grades, grade => grade.TeacherName == "Неизвестный преподаватель" && grade.Score == 3);
        Assert.Equal(5d, report.Averages["A"]!.Value);
        Assert.Equal(50d, report.FinalScore!.Value);
    }

    [Fact]
    public void GroupReport_IsSortedAndIncludesUngradedStudents()
    {
        var (school, _, _, facade, _, _, _, _) = Build();
        school.AddStudent(new Student(Guid.NewGuid(), "Борис", "Александров", school.Groups[0].Id));
        var reports = facade.GetGroupReport("G");
        Assert.Equal(2, reports.Students.Count);
        Assert.Equal("Александров", reports.Students[0].Student.Surname);
        Assert.Null(reports.Students[1].FinalScore);
    }

    [Fact]
    public void Report_RejectsUnknownAndBlankPeopleAndGroups()
    {
        var (_, _, _, facade, _, _, _, _) = Build();
        Assert.Throws<ArgumentException>(() => facade.GetStudentReport("", "Иванова", "G"));
        Assert.Throws<ArgumentException>(() => facade.GetStudentReport("Борис", "Иванов", "G"));
        Assert.Throws<ArgumentException>(() => facade.GetStudentReport("Анна", "Иванова", "Unknown"));
        Assert.Throws<ArgumentException>(() => facade.GetGroupReport(" "));
        Assert.Throws<ArgumentException>(() => facade.GetGroupReport("Unknown"));
    }

    [Fact]
    public void Export_UsesExporterInterfaceAndPassesGroupReport()
    {
        var (_, _, _, facade, exporter, _, _, _) = Build();
        facade.ExportGroupReport("G", "results.csv");
        Assert.Equal("results.csv", exporter.LastPath);
        Assert.NotNull(exporter.LastReport);
        Assert.Single(exporter.LastReport!.Students);
        Assert.Equal("G", exporter.LastReport.Group.Name);
    }

    private sealed class SpyExporter : IReportExporter
    {
        public string? LastPath { get; private set; }
        public GroupReport? LastReport { get; private set; }
        public void Export(GroupReport report, string path)
        {
            LastReport = report;
            LastPath = path;
        }
    }
}
