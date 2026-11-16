namespace POS.Domain;

public class StudentEvaluation
{
    public Guid StudentId { get; }
    private readonly List<CriterionGrade> _grades = new();
    public IReadOnlyList<CriterionGrade> Grades => _grades.AsReadOnly();

    public StudentEvaluation(Guid studentId)
    {
        if (studentId == Guid.Empty)
            throw new ArgumentException("Идентификатор студента не может быть пустым");
        StudentId = studentId;
    }

    public void AddGrade(CriterionGrade grade, Rubric rubric)
    {
        ArgumentNullException.ThrowIfNull(grade);
        ArgumentNullException.ThrowIfNull(rubric);
        var criterion = rubric.Find(grade.Criterion.Name);
        if (criterion == null || !ReferenceEquals(criterion, grade.Criterion))
            throw new InvalidOperationException("Оценка относится к критерию вне текущего рубрикатора");
        if (_grades.Any(g => g.TeacherId == grade.TeacherId &&
            string.Equals(g.Criterion.Name, grade.Criterion.Name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Преподаватель уже выставил оценку по этому критерию");
        _grades.Add(grade);
    }
}
