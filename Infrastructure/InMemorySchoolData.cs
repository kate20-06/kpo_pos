using POS.Domain;
using POS.Application;

namespace POS.Infrastructure;

public class InMemorySchoolData : ISchoolData
{
    private readonly List<Student> _students = new();
    private readonly List<Teacher> _teachers = new();
    private readonly List<Group> _groups = new();

    public IReadOnlyList<Student> Students => _students.AsReadOnly();
    public IReadOnlyList<Teacher> Teachers => _teachers.AsReadOnly();
    public IReadOnlyList<Group> Groups => _groups.AsReadOnly();

    private static bool Same(string a, string b) =>
        string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);

    public bool ContainsStudent(string name, string surname, string groupName)
    {
        var group = FindGroup(groupName);
        return group != null && _students.Any(s =>
            s.GroupId == group.Id && Same(s.Name, name) && Same(s.Surname, surname));
    }

    public bool ContainsTeacher(string name, string surname) =>
        _teachers.Any(t => Same(t.Name, name) && Same(t.Surname, surname));

    public Group? FindGroup(string name) => string.IsNullOrWhiteSpace(name)
        ? null : _groups.FirstOrDefault(g => Same(g.Name, name));

    public void AddStudent(Student student)
    {
        ArgumentNullException.ThrowIfNull(student);
        var group = _groups.FirstOrDefault(g => g.Id == student.GroupId);
        if (group == null) throw new InvalidOperationException("Группа студента не найдена");
        if (_students.Any(s => s.Id == student.Id ||
            (s.GroupId == student.GroupId && Same(s.Name, student.Name) && Same(s.Surname, student.Surname))))
            throw new InvalidOperationException("Студент уже существует в этой группе");
        _students.Add(student);
    }

    public void AddTeacher(Teacher teacher)
    {
        ArgumentNullException.ThrowIfNull(teacher);
        if (teacher.GroupIds.Count == 0 || teacher.GroupIds.Any(id => _groups.All(g => g.Id != id)))
            throw new InvalidOperationException("У преподавателя должна быть хотя бы одна существующая группа");
        if (_teachers.Any(t => t.Id == teacher.Id ||
            (Same(t.Name, teacher.Name) && Same(t.Surname, teacher.Surname))))
            throw new InvalidOperationException("Преподаватель уже существует");
        _teachers.Add(teacher);
    }

    public Group GetOrCreateGroup(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
            throw new ArgumentException("Название группы не может быть пустым");
        return FindGroup(groupName) ?? AddGroup(groupName);
    }

    private Group AddGroup(string groupName)
    {
        var group = new Group(Guid.NewGuid(), groupName);
        _groups.Add(group);
        return group;
    }
}
