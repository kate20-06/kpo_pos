using POS.Domain;

namespace POS.Application;

public class SchoolImportService
{
    private readonly ISchoolDataReader _reader;
    private readonly Factory<Student> _studentFactory;
    private readonly Factory<Teacher> _teacherFactory;
    private readonly ISchoolData _school;
    private readonly Rubric _rubric;

    public SchoolImportService(
        ISchoolDataReader reader,
        Factory<Student> studentFactory,
        Factory<Teacher> teacherFactory,
        ISchoolData school,
        Rubric rubric)
    {
        _reader = reader;
        _studentFactory = studentFactory;
        _teacherFactory = teacherFactory;
        _school = school;
        _rubric = rubric;
    }

    public ImportResult ImportStudents(string path)
    {
        var rows = _reader.ReadStudents(path);
        ArgumentNullException.ThrowIfNull(rows);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pending = new List<StudentImportRow>();
        int skipped = 0;

        // Проверяем весь файл до изменения SchoolData.
        foreach (var row in rows)
        {
            CheckPerson(row.Name, row.Surname);
            if (string.IsNullOrWhiteSpace(row.Group))
                throw new ArgumentException("Группа студента не может быть пустой");

            var key = $"{PersonKey(row.Name, row.Surname)}\u001F{row.Group.Trim().ToUpperInvariant()}";
            if (!seen.Add(key) || _school.ContainsStudent(row.Name, row.Surname, row.Group))
                skipped++;
            else
                pending.Add(row);
        }

        foreach (var row in pending)
            _studentFactory.Create(row.Name, row.Surname, new List<string> { row.Group });

        return new ImportResult(pending.Count, skipped);
    }

    public ImportResult ImportTeachers(string path)
    {
        var rows = _reader.ReadTeachers(path);
        ArgumentNullException.ThrowIfNull(rows);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pending = new List<TeacherImportRow>();
        int skipped = 0;

        foreach (var row in rows)
        {
            CheckPerson(row.Name, row.Surname);
            if (row.Groups == null || row.Groups.Count == 0 ||
                row.Groups.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentException("У преподавателя должна быть хотя бы одна группа");

            var key = PersonKey(row.Name, row.Surname);
            if (!seen.Add(key) || _school.ContainsTeacher(row.Name, row.Surname))
                skipped++;
            else
                pending.Add(row);
        }

        foreach (var row in pending)
            _teacherFactory.Create(row.Name, row.Surname, row.Groups.ToList());

        return new ImportResult(pending.Count, skipped);
    }

    public ImportResult ImportCriteria(string path)
    {
        var rows = _reader.ReadCriteria(path);
        ArgumentNullException.ThrowIfNull(rows);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pending = new List<Criterion>();
        int skipped = 0;

        foreach (var row in rows)
        {
            // Проверяем даже дубликаты: некорректные данные нельзя молча пропускать.
            var criterion = new Criterion(row.Name, row.Text, row.MaxScore, row.Weight);
            if (!seen.Add(criterion.Name) || _rubric.Find(criterion.Name) != null)
                skipped++;
            else
                pending.Add(criterion);
        }

        foreach (var criterion in pending)
            _rubric.AddCriterion(criterion);

        return new ImportResult(pending.Count, skipped);
    }

    private static string PersonKey(string name, string surname) =>
        $"{name.Trim().ToUpperInvariant()}\u001F{surname.Trim().ToUpperInvariant()}";

    private static void CheckPerson(string name, string surname)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(surname))
            throw new ArgumentException("Имя и фамилия обязательны");
    }
}
