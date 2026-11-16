namespace POS.Domain;

public class Group
{
    public Guid Id { get; }
    public string Name { get; }

    public Group(Guid id, string name)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Идентификатор группы не может быть пустым");
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Название группы не может быть пустым");
        Id = id;
        Name = name.Trim();
    }
}
