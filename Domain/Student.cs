namespace POS.Domain;

public class Student
{
    public Guid Id { get; }
    public string Name { get; }
    public string Surname { get; }
    public Guid GroupId { get; }

    public Student(Guid id, string name, string surname, Guid groupId)
    {
        if (id == Guid.Empty || groupId == Guid.Empty)
            throw new ArgumentException("Идентификаторы студента и группы не могут быть пустыми");
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(surname))
            throw new ArgumentException("Имя и фамилия студента обязательны");
        Id = id;
        Name = name.Trim();
        Surname = surname.Trim();
        GroupId = groupId;
    }
}
