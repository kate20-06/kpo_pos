using POS.Domain;

namespace POS.Application;

public class TeacherFactory : Factory<Teacher>
{
    public TeacherFactory(ISchoolData school)
        : base(school)
    {
    }

    protected override void ValidateCreation(string name, string surname, List<string> groupNames)
    {
        if (School.ContainsTeacher(name, surname))
            throw new InvalidOperationException("Преподаватель уже существует");
    }

    protected override Teacher CreateEntity(
        string name,
        string surname,
        List<Group> groups)
    {
        var teacher = new Teacher(
            Guid.NewGuid(),
            name,
            surname
        );

        foreach (var group in groups)
        {
            teacher.AddGroup(group.Id);
        }

        return teacher;
    }

    protected override void Save(Teacher teacher)
    {
        School.AddTeacher(teacher);
    }
}