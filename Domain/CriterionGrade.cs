namespace POS.Domain;

public sealed record CriterionGrade
{
    public Criterion Criterion { get; }
    public Guid TeacherId { get; }
    public int Score { get; }

    public CriterionGrade(Criterion criterion, Guid teacherId, int score)
    {
        ArgumentNullException.ThrowIfNull(criterion);
        if (teacherId == Guid.Empty)
            throw new ArgumentException("Идентификатор преподавателя не может быть пустым");
        if (score < 0 || score > criterion.MaxScore)
            throw new ArgumentOutOfRangeException(nameof(score), "Балл вне диапазона критерия");
        Criterion = criterion;
        TeacherId = teacherId;
        Score = score;
    }
}
