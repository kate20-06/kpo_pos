using POS.Domain;

namespace POS.Application;

public class SchoolFacade
{
    private readonly Factory<Student> _studentFactory;
    private readonly Factory<Teacher> _teacherFactory;
    private readonly ISchoolData _school;
    private readonly Rubric _rubric;
    private readonly SchoolImportService _imports;

    public SchoolFacade(Factory<Student> studentFactory, Factory<Teacher> teacherFactory,
        ISchoolData school, Rubric rubric, SchoolImportService imports)
    {
        _studentFactory = studentFactory;
        _teacherFactory = teacherFactory;
        _school = school;
        _rubric = rubric;
        _imports = imports;
    }

    public Student CreateStudent(string name, string surname, string groupName) =>
        _studentFactory.Create(name, surname, new List<string> { groupName });

    public List<Student> CreateGroup(string groupName, List<(string Name, string Surname)> students)
    {
        if (string.IsNullOrWhiteSpace(groupName))
            throw new ArgumentException("Название группы не может быть пустым");
        if (students == null || students.Count == 0)
            throw new ArgumentException("Список студентов не может быть пустым");
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, surname) in students)
        {
            CheckPerson(name, surname);
            if (!keys.Add(PersonKey(name, surname)))
                throw new ArgumentException("В списке есть повторяющийся студент");
            if (_school.ContainsStudent(name, surname, groupName))
                throw new InvalidOperationException("Студент уже существует в этой группе");
        }
        var result = new List<Student>();
        foreach (var (name, surname) in students)
            result.Add(CreateStudent(name, surname, groupName));
        return result;
    }

    public Teacher CreateTeacher(string name, string surname, List<string> groups) =>
        _teacherFactory.Create(name, surname, groups);

    public Criterion CreateCriterion(string name, string text, int maxScore, double weight)
    {
        var criterion = new Criterion(name, text, maxScore, weight);
        _rubric.AddCriterion(criterion);
        return criterion;
    }

    public IReadOnlyList<Criterion> GetCriteria() => _rubric.Criteria;
    public IReadOnlyList<Student> GetStudents() => _school.Students;
    public IReadOnlyList<Group> GetGroups() => _school.Groups;
    public IReadOnlyList<Teacher> GetTeachers() => _school.Teachers;
    private static string PersonKey(string name, string surname) =>
        $"{name.Trim().ToUpperInvariant()}\u001F{surname.Trim().ToUpperInvariant()}";

    private static void CheckPerson(string name, string surname)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(surname))
            throw new ArgumentException("Имя и фамилия обязательны");
    }

    public ImportResult ImportStudents(string path) => _imports.ImportStudents(path);

    public ImportResult ImportTeachers(string path) => _imports.ImportTeachers(path);

    public ImportResult ImportCriteria(string path) => _imports.ImportCriteria(path);
}
