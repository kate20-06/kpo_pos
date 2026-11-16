using POS.Application;
using POS.ConsoleUI;
using POS.Domain;
using POS.Infrastructure;
using Xunit;

namespace PosUnitTests;

public class CsvReportExporterTests
{
    [Fact]
    public void Export_WritesHeaderBOMNumericValuesAndMissingGrades()
    {
        var group = new Group(Guid.NewGuid(), "G");
        var s1 = new Student(Guid.NewGuid(), "Анна", "Иванова", group.Id);
        var s2 = new Student(Guid.NewGuid(), "Борис", "Петров", group.Id);
        var report = new GroupReport(group, new[]
        {
            new StudentReport(s1, Array.Empty<GradeDetail>(),
                new Dictionary<string, double?> { ["Активность"] = 5.5 }, 55),
            new StudentReport(s2, Array.Empty<GradeDetail>(),
                new Dictionary<string, double?> { ["Активность"] = null }, null)
        });
        using var file = TempFile.Create("");
        new CsvReportExporter().Export(report, file.Path);
        var bytes = File.ReadAllBytes(file.Path);
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes.Take(3).ToArray());
        var csv = File.ReadAllText(file.Path);
        Assert.Contains("\"Имя\";\"Фамилия\";\"Активность\";\"Итог (%)\"", csv);
        Assert.Contains("\"Анна\";\"Иванова\";\"5.50\";\"55.00\"", csv);
        Assert.Contains("\"Борис\";\"Петров\";\"\";\"\"", csv);
    }

    [Fact]
    public void Export_EscapesQuotesSemicolonsAndPreventsFormulaInjection()
    {
        var group = new Group(Guid.NewGuid(), "G");
        var student = new Student(Guid.NewGuid(), "=1+1", "Ива\"нов;Ф", group.Id);
        var report = new GroupReport(group, new[]
        {
            new StudentReport(student, Array.Empty<GradeDetail>(),
                new Dictionary<string, double?> { ["@cmd"] = 1 }, 100)
        });
        using var file = TempFile.Create("");
        new CsvReportExporter().Export(report, file.Path);
        var csv = File.ReadAllText(file.Path);
        Assert.Contains("\"'=1+1\"", csv);
        Assert.Contains("\"Ива\"\"нов;Ф\"", csv);
        Assert.Contains("\"'@cmd\"", csv);
    }

    [Fact]
    public void Export_EmptyGroupStillWritesHeader()
    {
        using var file = TempFile.Create("");
        var report = new GroupReport(new Group(Guid.NewGuid(), "G"), Array.Empty<StudentReport>());
        new CsvReportExporter().Export(report, file.Path);
        Assert.Contains("\"Итог (%)\"", File.ReadAllText(file.Path));
    }
}

// Tests use a locked console because Console.SetIn is a process-wide dependency.
[CollectionDefinition("Console tests", DisableParallelization = true)]
public class ConsoleTestsCollection { }

[Collection("Console tests")]
public class ConsoleInputTests
{
    private static T RunWithInput<T>(string input, Func<T> action)
    {
        var previousIn = Console.In;
        var previousOut = Console.Out;
        try
        {
            using var reader = new StringReader(input);
            using var writer = new StringWriter();
            Console.SetIn(reader);
            Console.SetOut(writer);
            return action();
        }
        finally
        {
            Console.SetIn(previousIn);
            Console.SetOut(previousOut);
        }
    }

    [Fact]
    public void Text_RepeatsAfterBlanksAndReturnsTrimmed()
    {
        var result = RunWithInput("\n  \n Анна \n", () => ConsoleInput.ReadText("Имя: "));
        Assert.Equal("Анна", result);
    }

    [Fact]
    public void Text_EndOfStreamThrows()
    {
        Assert.Throws<EndOfStreamException>(() =>
            RunWithInput("", () => ConsoleInput.ReadText("Имя: ")));
    }

    [Fact]
    public void Int_RepeatsUntilWithinInclusiveBounds()
    {
        var result = RunWithInput("abcd\n-1\n11\n0\n", () => ConsoleInput.ReadInt("Балл: ", 0, 10));
        Assert.Equal(0, result);
        Assert.Equal(10, RunWithInput("10\n", () => ConsoleInput.ReadInt("Балл: ", 0, 10)));
    }

    [Fact]
    public void Double_AcceptsCommaAndDotAndRejectsNaNInfinityAndZero()
    {
        var value = RunWithInput("NaN\nInfinity\n0\n-1\n0,25\n", () => ConsoleInput.ReadPositiveDouble("Вес: "));
        Assert.Equal(0.25, value);
        Assert.Equal(0.5, RunWithInput("0.5\n", () => ConsoleInput.ReadPositiveDouble("Вес: ")));
    }

    [Theory]
    [InlineData("l\nд\n", true)]
    [InlineData("l\nн\n", false)]
    [InlineData("?\nД\n", true)]
    [InlineData("0\nН\n", false)]
    [InlineData(" \n  д  \n", true)]
    public void YesNo_RepeatsInvalidAnswersAndAcceptsEitherCase(
        string input, bool expected)
    {
        Assert.Equal(expected,
            RunWithInput(input, () => ConsoleInput.ReadYesNo("Продолжить? (д/н): ")));
    }

    [Fact]
    public void Choice_RepeatsUntilAllowed()
    {
        Assert.Equal("2", RunWithInput("bad\n9\n2\n", () => ConsoleInput.ReadChoice("Выбор: ", "1", "2")));
    }
}
