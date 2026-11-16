namespace POS.Domain;

public class Rubric
{
    private readonly List<Criterion> _criteria = new();
    public IReadOnlyList<Criterion> Criteria => _criteria.AsReadOnly();

    public Criterion? Find(string name) => _criteria.FirstOrDefault(c =>
        string.Equals(c.Name, name?.Trim(), StringComparison.OrdinalIgnoreCase));

    public void AddCriterion(Criterion criterion)
    {
        ArgumentNullException.ThrowIfNull(criterion);
        if (Find(criterion.Name) != null)
            throw new ArgumentException("Критерий с таким названием уже существует");
        _criteria.Add(criterion);
    }
}
