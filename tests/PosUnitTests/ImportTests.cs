using POS.Application;
using POS.Domain;
using POS.Infrastructure;
using Xunit;

namespace PosUnitTests;

public class SchoolImportServiceTests
{
    private static (FakeSchoolReader reader, InMemorySchoolData school, Rubric rubric, SchoolImportService service) Build()
    {
        var school = new InMemorySchoolData();
        var rubric = new Rubric();
        var reader = new FakeSchoolReader();
        var service = new SchoolImportService(reader, new StudentFactory(school),
            new TeacherFactory(school), school, rubric);
        return (reader, school, rubric, service);
    }

    [Fact]
    public void Students_SkipDuplicatesInInputAndStore_AndRerunningIsIdempotent()
    {
        var (reader, school, _, service) = Build();
        var existingGroup = school.GetOrCreateGroup("G");
        school.AddStudent(new Student(Guid.NewGuid(), "Анна", "Иванова", existingGroup.Id));
        reader.Students = new StudentImportRow[]
        {
            new("АННА", "иванова", " g "),
            new("Борис", "Смирнов", "G"),
            new(" борис ", "СМИРНОВ", "g"),
            new("Борис", "Смирнов", "H")
        };
        var result = service.ImportStudents("unused.csv");
        Assert.Equal(2, result.Imported);
        Assert.Equal(2, result.Skipped);
        Assert.Equal(3, school.Students.Count);
        Assert.Equal(2, school.Groups.Count);
        var repeated = service.ImportStudents("unused.csv");
        Assert.Equal(0, repeated.Imported);
        Assert.Equal(4, repeated.Skipped);
    }

    [Fact]
    public void Students_InvalidRow_DoesNotPartiallyWrite()
    {
        var (reader, school, _, service) = Build();
        reader.Students = new StudentImportRow[]
        {
            new("Новый", "Человек", "G"),
            new("", "Человек", "G")
        };
        Assert.Throws<ArgumentException>(() => service.ImportStudents("unused"));
        Assert.Empty(school.Students);
        Assert.Empty(school.Groups);
    }

    [Fact]
    public void Teachers_SkipExistingAndRepeatedNames_ButCreateAssociatedGroups()
    {
        var (reader, school, _, service) = Build();
        var g = school.GetOrCreateGroup("G");
        var existing = new Teacher(Guid.NewGuid(), "И", "Ф");
        existing.AddGroup(g.Id);
        school.AddTeacher(existing);
        reader.Teachers = new TeacherImportRow[]
        {
            new(" и ", "ф", new[] { "ShouldNotExist" }),
            new("В", "П", new[] { "G", "H" }),
            new("в", "п", new[] { "Other" })
        };
        var result = service.ImportTeachers("unused");
        Assert.Equal(1, result.Imported);
        Assert.Equal(2, result.Skipped);
        Assert.Equal(2, school.Teachers.Count);
        Assert.Equal(2, school.Groups.Count);
        Assert.DoesNotContain(school.Groups, x => x.Name == "Other" || x.Name == "ShouldNotExist");
    }

    [Fact]
    public void Teachers_InvalidGroupInLaterRow_RejectsEntireFile()
    {
        var (reader, school, _, service) = Build();
        reader.Teachers = new TeacherImportRow[]
        {
            new("А", "Б", new[] { "G" }),
            new("В", "Г", new[] { "G", " " })
        };
        Assert.Throws<ArgumentException>(() => service.ImportTeachers("unused"));
        Assert.Empty(school.Teachers);
        Assert.Empty(school.Groups);
    }

    [Fact]
    public void Criteria_SkipDuplicatesCaseInsensitivelyAndValidateEvenSkippedRows()
    {
        var (reader, _, rubric, service) = Build();
        rubric.AddCriterion(new Criterion("A", "Описание", 10, 1));
        reader.Criteria = new CriterionImportRow[]
        {
            new(" a ", "Другое", 5, 1),
            new("B", "Описание", 10, 0.5),
            new("b", "Описание", 10, 2)
        };
        var result = service.ImportCriteria("unused");
        Assert.Equal(1, result.Imported);
        Assert.Equal(2, result.Skipped);
        Assert.Equal(2, rubric.Criteria.Count);
        reader.Criteria = new CriterionImportRow[]
        {
            new("C", "Описание", 10, 1),
            new("a", "Описание", 10, double.NaN)
        };
        Assert.Throws<ArgumentOutOfRangeException>(() => service.ImportCriteria("unused"));
        Assert.Null(rubric.Find("C"));
    }

    [Fact]
    public void EmptyImport_ReturnsZeros()
    {
        var (_, school, rubric, service) = Build();
        Assert.Equal(new ImportResult(0, 0), service.ImportStudents("ignored"));
        Assert.Equal(new ImportResult(0, 0), service.ImportTeachers("ignored"));
        Assert.Equal(new ImportResult(0, 0), service.ImportCriteria("ignored"));
        Assert.Empty(school.Students);
        Assert.Empty(rubric.Criteria);
    }
}

public sealed class CsvReaderTests
{
    [Fact]
    public void Students_ParsesQuotedSemicolonQuotedNewlineAndEscapedQuotes()
    {
        using var file = TempFile.Create("Name;Surname;Group\n\"Мария;Анна\";\"Ива\"\"нова\";G\n\"Олег\nПётр\";Сидоров;G2\n");
        var rows = new CsvSchoolDataReader().ReadStudents(file.Path);
        Assert.Equal(2, rows.Count);
        Assert.Equal("Мария;Анна", rows[0].Name);
        Assert.Equal("Ива\"нова", rows[0].Surname);
        Assert.Equal("Олег\nПётр", rows[1].Name);
    }

    [Fact]
    public void Students_AcceptsUtf8BomAndMixedCaseHeader()
    {
        using var file = TempFile.Create("\uFEFFNAME;SURNAME;GROUP\nАнна;Иванова;G\n");
        Assert.Single(new CsvSchoolDataReader().ReadStudents(file.Path));
    }

    [Theory]
    [InlineData("wrong;Surname;Group\nА;Б;G\n")]
    [InlineData("Name;Surname\nА;Б\n")]
    [InlineData("Name;Surname;Group\nА;Б\n")]
    [InlineData("Name;Surname;Group\nА;Б;G;Extra\n")]
    [InlineData("Name;Surname;Group\n;Б;G\n")]
    [InlineData("Name;Surname;Group\nА;;G\n")]
    [InlineData("Name;Surname;Group\nА;Б;\n")]
    public void Students_RejectsMalformedContents(string content)
    {
        using var file = TempFile.Create(content);
        Assert.Throws<FormatException>(() => new CsvSchoolDataReader().ReadStudents(file.Path));
    }

    [Fact]
    public void Students_RejectsMissingFileAndEmptyPath()
    {
        var reader = new CsvSchoolDataReader();
        Assert.Throws<ArgumentException>(() => reader.ReadStudents(""));
        Assert.Throws<FileNotFoundException>(() => reader.ReadStudents(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".csv")));
    }

    [Fact]
    public void Students_RejectsMoreThanTenThousandRecords()
    {
        var content = "Name;Surname;Group\n" + string.Concat(Enumerable.Range(0, 10001)
            .Select(i => $"Имя{i};Иванов;G\n"));
        using var file = TempFile.Create(content);
        Assert.Throws<FormatException>(() => new CsvSchoolDataReader().ReadStudents(file.Path));
    }

    [Fact]
    public void Students_RejectsOversizedFile()
    {
        using var file = TempFile.Create("Name;Surname;Group\n" + new string('x', 5 * 1024 * 1024 + 1));
        Assert.Throws<FormatException>(() => new CsvSchoolDataReader().ReadStudents(file.Path));
    }

    [Fact]
    public void Teachers_ParsesGroupSeparatorAndRejectsEmptyGroup()
    {
        using var valid = TempFile.Create("Name;Surname;Groups\nПётр;Петров;G1| G2\n");
        var row = Assert.Single(new CsvSchoolDataReader().ReadTeachers(valid.Path));
        Assert.Equal(new[] { "G1", "G2" }, row.Groups);
        using var invalid = TempFile.Create("Name;Surname;Groups\nПётр;Петров;G1||G2\n");
        Assert.Throws<FormatException>(() => new CsvSchoolDataReader().ReadTeachers(invalid.Path));
    }

    [Theory]
    [InlineData("0", "0.5")]
    [InlineData("-1", "0.5")]
    [InlineData("bad", "0.5")]
    [InlineData("10", "NaN")]
    [InlineData("10", "Infinity")]
    [InlineData("10", "0")]
    [InlineData("10", "-0.5")]
    [InlineData("10", "bad")]
    public void Criteria_RejectsInvalidNumbers(string max, string weight)
    {
        using var file = TempFile.Create($"Name;Text;MaxScore;Weight\nA;B;{max};{weight}\n");
        Assert.Throws<FormatException>(() => new CsvSchoolDataReader().ReadCriteria(file.Path));
    }

    [Fact]
    public void Criteria_ParsesBothCommaAndDotDecimalSeparators()
    {
        using var file = TempFile.Create("Name;Text;MaxScore;Weight\nA;B;10;0,5\nC;D;20;0.25\n");
        var rows = new CsvSchoolDataReader().ReadCriteria(file.Path);
        Assert.Equal(0.5, rows[0].Weight);
        Assert.Equal(0.25, rows[1].Weight);
    }
}

internal sealed class TempFile : IDisposable
{
    public string Path { get; }
    private TempFile(string path) => Path = path;
    public static TempFile Create(string contents)
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pos-unit-" + Guid.NewGuid().ToString("N") + ".csv");
        File.WriteAllText(path, contents, new System.Text.UTF8Encoding(false));
        return new TempFile(path);
    }
    public void Dispose() { if (File.Exists(Path)) File.Delete(Path); }
}
