using POS.Domain;

namespace POS.Application;

public interface ISchoolData
{
    IReadOnlyList<Student> Students { get; }
    IReadOnlyList<Teacher> Teachers { get; }
    IReadOnlyList<Group> Groups { get; }

    bool ContainsStudent(string name, string surname, string groupName);
    bool ContainsTeacher(string name, string surname);
    Group? FindGroup(string name);

    void AddStudent(Student student);
    void AddTeacher(Teacher teacher);
    Group GetOrCreateGroup(string groupName);
}
