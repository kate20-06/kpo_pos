using POS.Domain;
using Xunit;

namespace PosUnitTests;

public class EntityTests
{
    [Theory]
    [InlineData("", "Иванов")]
    [InlineData("  ", "Иванов")]
    [InlineData("Иван", "")]
    [InlineData(null, "Иванов")]
    [InlineData("Иван", null)]
    public void Student_RejectsBlankNames(string? name, string? surname) =>
        Assert.Throws<ArgumentException>(() => new Student(Guid.NewGuid(), name!, surname!, Guid.NewGuid()));

    [Fact]
    public void Student_RequiresValidIds()
    {
        Assert.Throws<ArgumentException>(() => new Student(Guid.Empty, "И", "Ф", Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() => new Student(Guid.NewGuid(), "И", "Ф", Guid.Empty));
    }

    [Fact]
    public void Student_TrimsNamesAndPreservesIdentity()
    {
        var id = Guid.NewGuid(); var group = Guid.NewGuid();
        var student = new Student(id, "  Анна ", " Иванова ", group);
        Assert.Equal(id, student.Id);
        Assert.Equal(group, student.GroupId);
        Assert.Equal("Анна", student.Name);
        Assert.Equal("Иванова", student.Surname);
    }

    [Theory]
    [InlineData("", "Ф")]
    [InlineData("И", " ")]
    [InlineData(null, "Ф")]
    public void Teacher_RejectsBlankNames(string? name, string? surname) =>
        Assert.Throws<ArgumentException>(() => new Teacher(Guid.NewGuid(), name!, surname!));

    [Fact]
    public void Teacher_RejectsEmptyIdAndBadGroup()
    {
        Assert.Throws<ArgumentException>(() => new Teacher(Guid.Empty, "И", "Ф"));
        var teacher = new Teacher(Guid.NewGuid(), " И ", " Ф ");
        Assert.Equal("И", teacher.Name);
        Assert.Equal("Ф", teacher.Surname);
        Assert.Throws<ArgumentException>(() => teacher.AddGroup(Guid.Empty));
        Assert.Empty(teacher.GroupIds);
    }

    [Fact]
    public void Teacher_StoresUniqueGroups()
    {
        var teacher = new Teacher(Guid.NewGuid(), "И", "Ф");
        var a = Guid.NewGuid(); var b = Guid.NewGuid();
        teacher.AddGroup(a); teacher.AddGroup(a); teacher.AddGroup(b);
        Assert.Equal(new[] { a, b }, teacher.GroupIds);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void Group_RejectsBlankName(string? name) =>
        Assert.Throws<ArgumentException>(() => new Group(Guid.NewGuid(), name!));

    [Fact]
    public void Group_RequiresIdAndTrimsName()
    {
        Assert.Throws<ArgumentException>(() => new Group(Guid.Empty, "G"));
        Assert.Equal("G", new Group(Guid.NewGuid(), " G ").Name);
    }
}

public class CriterionTests
{
    [Theory]
    [InlineData("", "Описание")]
    [InlineData("  ", "Описание")]
    [InlineData("Тест", "")]
    [InlineData("Тест", " ")]
    [InlineData(null, "Описание")]
    [InlineData("Тест", null)]
    public void RejectsBlankText(string? name, string? text) =>
        Assert.Throws<ArgumentException>(() => new Criterion(name!, text!, 10, 1));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void RejectsNonPositiveMaxScore(int max) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new Criterion("A", "B", max, 1));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void RejectsInvalidWeight(double weight) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new Criterion("A", "B", 10, weight));

    [Fact]
    public void AcceptsTinyAndLargeFiniteWeights()
    {
        Assert.Equal(double.Epsilon, new Criterion("A", "B", 1, double.Epsilon).Weight);
        Assert.Equal(double.MaxValue, new Criterion("A", "B", 1, double.MaxValue).Weight);
        var c = new Criterion(" A ", " B ", int.MaxValue, 0.4);
        Assert.Equal("A", c.Name);
        Assert.Equal("B", c.Text);
    }

    [Fact]
    public void Rubric_DoesNotAcceptNullOrDuplicateAndFindsIgnoringCase()
    {
        var rubric = new Rubric();
        Assert.Throws<ArgumentNullException>(() => rubric.AddCriterion(null!));
        var criterion = new Criterion("Активность", "Описание", 10, 1);
        rubric.AddCriterion(criterion);
        Assert.Same(criterion, rubric.Find(" активность "));
        Assert.Null(rubric.Find("Несуществующий"));
        Assert.Throws<ArgumentException>(() =>
            rubric.AddCriterion(new Criterion(" АКТИВНОСТЬ ", "Другая", 5, 2)));
        Assert.Single(rubric.Criteria);
    }

    [Fact]
    public void Rubric_ExposesReadOnlyCriteria()
    {
        var rubric = new Rubric();
        var collection = Assert.IsAssignableFrom<ICollection<Criterion>>(rubric.Criteria);
        Assert.Throws<NotSupportedException>(() => collection.Add(new Criterion("A", "B", 2, 1)));
    }
}

public class EvaluationTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void Grade_RejectsOutOfRangeScores(int score)
    {
        var criterion = new Criterion("A", "B", 10, 1);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CriterionGrade(criterion, Guid.NewGuid(), score));
    }

    [Fact]
    public void Grade_AcceptsBothBoundsAndRejectsInvalidReferences()
    {
        var c = new Criterion("A", "B", 10, 1);
        var teacher = Guid.NewGuid();
        Assert.Equal(0, new CriterionGrade(c, teacher, 0).Score);
        Assert.Equal(10, new CriterionGrade(c, teacher, 10).Score);
        Assert.Throws<ArgumentNullException>(() => new CriterionGrade(null!, teacher, 0));
        Assert.Throws<ArgumentException>(() => new CriterionGrade(c, Guid.Empty, 0));
    }

    [Fact]
    public void Evaluation_RequiresIdAndRejectsNullArguments()
    {
        Assert.Throws<ArgumentException>(() => new StudentEvaluation(Guid.Empty));
        var e = new StudentEvaluation(Guid.NewGuid());
        var rubric = new Rubric();
        var c = new Criterion("A", "B", 10, 1);
        rubric.AddCriterion(c);
        Assert.Throws<ArgumentNullException>(() => e.AddGrade(null!, rubric));
        Assert.Throws<ArgumentNullException>(() => e.AddGrade(new CriterionGrade(c, Guid.NewGuid(), 5), null!));
        Assert.Empty(e.Grades);
    }

    [Fact]
    public void Evaluation_RejectsCriterionAbsentFromRubricEvenWithSameName()
    {
        var rubric = new Rubric();
        var canonical = new Criterion("A", "B", 10, 1);
        rubric.AddCriterion(canonical);
        var impostor = new Criterion("a", "Other", 10, 1);
        var evaluation = new StudentEvaluation(Guid.NewGuid());
        Assert.Throws<InvalidOperationException>(() =>
            evaluation.AddGrade(new CriterionGrade(impostor, Guid.NewGuid(), 5), rubric));
        Assert.Empty(evaluation.Grades);
    }

    [Fact]
    public void Evaluation_RejectsRepeatedTeacherAndCriterionButAllowsOthers()
    {
        var rubric = new Rubric();
        var a = new Criterion("A", "B", 10, 1);
        var b = new Criterion("B", "B", 10, 1);
        rubric.AddCriterion(a); rubric.AddCriterion(b);
        var t1 = Guid.NewGuid(); var t2 = Guid.NewGuid();
        var evaluation = new StudentEvaluation(Guid.NewGuid());
        evaluation.AddGrade(new CriterionGrade(a, t1, 0), rubric);
        Assert.Throws<InvalidOperationException>(() =>
            evaluation.AddGrade(new CriterionGrade(a, t1, 9), rubric));
        evaluation.AddGrade(new CriterionGrade(a, t2, 10), rubric);
        evaluation.AddGrade(new CriterionGrade(b, t1, 10), rubric);
        Assert.Equal(3, evaluation.Grades.Count);
        Assert.Equal(0, evaluation.Grades[0].Score);
        var list = Assert.IsAssignableFrom<ICollection<CriterionGrade>>(evaluation.Grades);
        Assert.Throws<NotSupportedException>(() => list.Clear());
    }
}

public class EvaluationCalculatorTests
{
    private static (Rubric rubric, Criterion a, Criterion b) TwoCriteria(double weightA = 1, double weightB = 1)
    {
        var r = new Rubric();
        var a = new Criterion("A", "A", 10, weightA);
        var b = new Criterion("B", "B", 20, weightB);
        r.AddCriterion(a); r.AddCriterion(b);
        return (r, a, b);
    }

    [Fact]
    public void EmptyEvaluation_ReturnsNullAveragesAndFinal()
    {
        var (rubric, _, _) = TwoCriteria();
        var calc = new EvaluationCalculator();
        var e = new StudentEvaluation(Guid.NewGuid());
        var avg = calc.CalculateAverages(e, rubric);
        Assert.Null(avg["A"]); Assert.Null(avg["B"]);
        Assert.Null(calc.CalculateFinalScore(e, rubric));
        Assert.Null(calc.CalculateFinalScore(e, new Rubric()));
    }

    [Fact]
    public void AverageUsesAllTeachers_AndFinalUsesNormalizedWeights()
    {
        var (rubric, a, b) = TwoCriteria(3, 1);
        var e = new StudentEvaluation(Guid.NewGuid());
        e.AddGrade(new CriterionGrade(a, Guid.NewGuid(), 10), rubric);
        e.AddGrade(new CriterionGrade(a, Guid.NewGuid(), 0), rubric);
        e.AddGrade(new CriterionGrade(b, Guid.NewGuid(), 20), rubric);
        var calc = new EvaluationCalculator();
        Assert.Equal(5d, calc.CalculateAverages(e, rubric)["A"]);
        Assert.Equal(20d, calc.CalculateAverages(e, rubric)["B"]);
        Assert.Equal(62.5, calc.CalculateFinalScore(e, rubric)!.Value, 8);
    }

    [Fact]
    public void MissingCriterion_DoesNotReduceDenominator()
    {
        var (rubric, a, _) = TwoCriteria(1, 99);
        var e = new StudentEvaluation(Guid.NewGuid());
        e.AddGrade(new CriterionGrade(a, Guid.NewGuid(), 8), rubric);
        Assert.Equal(80, new EvaluationCalculator().CalculateFinalScore(e, rubric)!.Value, 8);
    }

    [Fact]
    public void ZeroAndMaximumFinal_AreValid()
    {
        var (rubric, a, b) = TwoCriteria();
        var e0 = new StudentEvaluation(Guid.NewGuid());
        e0.AddGrade(new CriterionGrade(a, Guid.NewGuid(), 0), rubric);
        Assert.Equal(0d, new EvaluationCalculator().CalculateFinalScore(e0, rubric)!.Value);
        var e100 = new StudentEvaluation(Guid.NewGuid());
        e100.AddGrade(new CriterionGrade(a, Guid.NewGuid(), 10), rubric);
        e100.AddGrade(new CriterionGrade(b, Guid.NewGuid(), 20), rubric);
        Assert.Equal(100d, new EvaluationCalculator().CalculateFinalScore(e100, rubric)!.Value);
    }

    [Fact]
    public void VeryLargeWeights_DoNotOverflow()
    {
        var (rubric, a, b) = TwoCriteria(1e308, 1e308);
        var evaluation = new StudentEvaluation(Guid.NewGuid());
        evaluation.AddGrade(new CriterionGrade(a, Guid.NewGuid(), 10), rubric);
        evaluation.AddGrade(new CriterionGrade(b, Guid.NewGuid(), 0), rubric);
        Assert.Equal(50, new EvaluationCalculator().CalculateFinalScore(evaluation, rubric)!.Value, 8);
    }

    [Fact]
    public void NullArguments_AreRejected()
    {
        var calc = new EvaluationCalculator();
        Assert.Throws<ArgumentNullException>(() => calc.CalculateAverages(null!, new Rubric()));
        Assert.Throws<ArgumentNullException>(() => calc.CalculateAverages(new StudentEvaluation(Guid.NewGuid()), null!));
    }
}
