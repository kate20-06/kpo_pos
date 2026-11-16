namespace POS.Domain;

public class Teacher
{
    public Guid Id { get; }
    public string Name { get; }
    public string Surname { get; }

    private readonly List<Guid> _groupIds = new();
    public IReadOnlyCollection<Guid> GroupIds => _groupIds.AsReadOnly();

    public Teacher(Guid id, string name, string surname)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Идентификатор преподавателя не может быть пустым");
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(surname))
            throw new ArgumentException("Имя и фамилия преподавателя обязательны");
        Id = id;
        Name = name.Trim();
        Surname = surname.Trim();
    }

    public void AddGroup(Guid groupId)
    {
        if (groupId == Guid.Empty)
            throw new ArgumentException("Идентификатор группы не может быть пустым");
        if (!_groupIds.Contains(groupId)) _groupIds.Add(groupId);
    }
}
