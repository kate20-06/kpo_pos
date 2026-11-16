using POS.Domain;

namespace POS.Application;

public class StudentFactory : Factory<Student>
{
    public StudentFactory(ISchoolData school)
        : base(school)
    {
    }

    protected override void ValidateGroups(
        List<string> groupNames)
    {
        if (groupNames.Count != 1)
        {
            throw new ArgumentException(
                "Студент должен принадлежать ровно одной группе"
            );
        }
    }

    protected override void ValidateCreation(string name, string surname, List<string> groupNames)
    {
        if (School.ContainsStudent(name, surname, groupNames[0]))
            throw new InvalidOperationException("Студент уже существует в этой группе");
    }

    protected override Student CreateEntity(
        string name,
        string surname,
        List<Group> groups)
    {
        return new Student(
            Guid.NewGuid(),
            name,
            surname,
            groups[0].Id
        );
    }

    protected override void Save(Student student)
    {
        School.AddStudent(student);
    }
}