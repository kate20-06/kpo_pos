using POS.Application;
using POS.Domain;
using POS.Infrastructure;
using Xunit;

namespace PosUnitTests;

public class StorageTests
{
    [Fact]
    public void GetOrCreateGroup_UsesCaseInsensitiveNameAndTrims()
    {
        var school = new InMemorySchoolData();
        var original = school.GetOrCreateGroup(" БПИ241 ");
        Assert.Same(original, school.GetOrCreateGroup("бпи241"));
        Assert.Single(school.Groups);
        Assert.Equal("БПИ241", original.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void GetOrCreateGroup_RejectsBlankName(string? name)
    {
        var school = new InMemorySchoolData();
        Assert.Throws<ArgumentException>(() => school.GetOrCreateGroup(name!));
        Assert.Empty(school.Groups);
    }

    [Fact]
    public void AddStudent_RejectsNullAbsentGroupDuplicateIdAndFullName()
    {
        var school = new InMemorySchoolData();
        Assert.Throws<ArgumentNullException>(() => school.AddStudent(null!));
        var groupId = Guid.NewGuid();
        Assert.Throws<InvalidOperationException>(() =>
            school.AddStudent(new Student(Guid.NewGuid(), "И", "Ф", groupId)));
        var group = school.GetOrCreateGroup("G1");
        var id = Guid.NewGuid();
        school.AddStudent(new Student(id, "Анна", "Иванова", group.Id));
        Assert.Throws<InvalidOperationException>(() =>
            school.AddStudent(new Student(id, "Другое", "Имя", group.Id)));
        Assert.Throws<InvalidOperationException>(() =>
            school.AddStudent(new Student(Guid.NewGuid(), " АННА ", " иванова ", group.Id)));
        Assert.Single(school.Students);
        Assert.True(school.ContainsStudent(" анна ", "ИВАНОВА", "g1"));
        Assert.False(school.ContainsStudent("Анна", "Иванова", "G2"));
    }

    [Fact]
    public void StudentWithSameNameCanBeInDifferentGroups()
    {
        var school = new InMemorySchoolData();
        var a = school.GetOrCreateGroup("A");
        var b = school.GetOrCreateGroup("B");
        school.AddStudent(new Student(Guid.NewGuid(), "И", "Ф", a.Id));
        school.AddStudent(new Student(Guid.NewGuid(), "И", "Ф", b.Id));
        Assert.Equal(2, school.Students.Count);
    }

    [Fact]
    public void AddTeacher_RequiresKnownGroupsAndPreventsDuplicates()
    {
        var school = new InMemorySchoolData();
        Assert.Throws<ArgumentNullException>(() => school.AddTeacher(null!));
        var t = new Teacher(Guid.NewGuid(), "Пётр", "Петров");
        Assert.Throws<InvalidOperationException>(() => school.AddTeacher(t));
        t.AddGroup(Guid.NewGuid());
        Assert.Throws<InvalidOperationException>(() => school.AddTeacher(t));
        var group = school.GetOrCreateGroup("G");
        t.AddGroup(group.Id);
        Assert.Throws<InvalidOperationException>(() => school.AddTeacher(t));
        var valid = new Teacher(Guid.NewGuid(), "Пётр", "Петров");
        valid.AddGroup(group.Id);
        school.AddTeacher(valid);
        var duplicateName = new Teacher(Guid.NewGuid(), " пЁТР ", " ПЕТРОВ ");
        duplicateName.AddGroup(group.Id);
        Assert.Throws<InvalidOperationException>(() => school.AddTeacher(duplicateName));
        Assert.True(school.ContainsTeacher("ПЁТР", "петров"));
        Assert.Single(school.Teachers);
    }

    [Fact]
    public void Collections_AreReadOnly()
    {
        var school = new InMemorySchoolData();
        var students = Assert.IsAssignableFrom<ICollection<Student>>(school.Students);
        Assert.Throws<NotSupportedException>(() => students.Clear());
        var groups = Assert.IsAssignableFrom<ICollection<Group>>(school.Groups);
        Assert.Throws<NotSupportedException>(() => groups.Clear());
    }

    [Fact]
    public void EvaluationRepository_SaveAndOverwriteByStudentId()
    {
        var repo = new InMemoryStudentEvaluationRepository();
        var id = Guid.NewGuid();
        Assert.Null(repo.GetByStudentId(id));
        var first = new StudentEvaluation(id);
        var second = new StudentEvaluation(id);
        repo.Save(first);
        Assert.Same(first, repo.GetByStudentId(id));
        repo.Save(second);
        Assert.Same(second, repo.GetByStudentId(id));
    }
}

public class FactoryAndSchoolFacadeTests
{
    [Fact]
    public void StudentFactory_AutoCreatesGroupAndRejectsMultipleGroups()
    {
        var school = new InMemorySchoolData();
        Factory<Student> factory = new StudentFactory(school);
        Assert.Throws<ArgumentException>(() => factory.Create("Анна", "Иванова", new List<string>()));
        Assert.Throws<ArgumentException>(() => factory.Create("Анна", "Иванова", new List<string> { "A", "B" }));
        Assert.Empty(school.Groups);
        var first = factory.Create(" Анна ", " Иванова ", new List<string> { "A" });
        Assert.Equal("Анна", first.Name);
        Assert.Single(school.Groups);
        Assert.Throws<InvalidOperationException>(() => factory.Create("АННА", "Иванова", new List<string> { "a" }));
        Assert.Single(school.Students);
        Assert.Single(school.Groups);
    }

    [Fact]
    public void TeacherFactory_CreatesMultipleUniqueGroupsAndRejectsDuplicatePerson()
    {
        var school = new InMemorySchoolData();
        Factory<Teacher> factory = new TeacherFactory(school);
        var teacher = factory.Create("Пётр", "Петров", new List<string> { "A", "A", "B" });
        Assert.Equal(2, school.Groups.Count);
        Assert.Equal(2, teacher.GroupIds.Count);
        Assert.Throws<InvalidOperationException>(() => factory.Create("пётр", "ПЕТРОВ", new List<string> { "C" }));
        Assert.DoesNotContain(school.Groups, x => x.Name == "C");
    }

    [Fact]
    public void Factories_RejectInvalidInputWithoutCreatingGroups()
    {
        var school = new InMemorySchoolData();
        var sf = new StudentFactory(school);
        var tf = new TeacherFactory(school);
        Assert.Throws<ArgumentException>(() => sf.Create("", "Иванова", new List<string> { "A" }));
        Assert.Throws<ArgumentException>(() => tf.Create("Пётр", "", new List<string> { "B" }));
        Assert.Throws<ArgumentException>(() => tf.Create("Пётр", "Петров", new List<string> { " " }));
        Assert.Empty(school.Groups);
    }

    [Fact]
    public void SchoolFacade_CreateGroup_IsAtomicForDuplicateWithinBatch()
    {
        var (school, facade, _) = Build();
        Assert.Throws<ArgumentException>(() => facade.CreateGroup("G", new()
        {
            ("Анна", "Иванова"), (" АННА ", "иванова")
        }));
        Assert.Empty(school.Students);
        Assert.Empty(school.Groups);
    }

    [Fact]
    public void SchoolFacade_CreateGroup_IsAtomicWhenExistingStudentConflicts()
    {
        var (school, facade, _) = Build();
        facade.CreateStudent("Пётр", "Петров", "G");
        Assert.Throws<InvalidOperationException>(() => facade.CreateGroup("G", new()
        {
            ("Новая", "Фамилия"), ("ПЁТР", "ПЕТРОВ")
        }));
        Assert.Single(school.Students);
    }

    [Fact]
    public void SchoolFacade_CreatesEntitiesAndExposesLists()
    {
        var (school, facade, rubric) = Build();
        Assert.Throws<ArgumentException>(() => facade.CreateGroup("G", new()));
        Assert.Throws<ArgumentException>(() => facade.CreateGroup(" ", new() { ("И", "Ф") }));
        var students = facade.CreateGroup("G", new() { ("Анна", "А"), ("Борис", "Б") });
        Assert.Equal(2, students.Count);
        Assert.Equal(2, facade.GetStudents().Count);
        Assert.Single(facade.GetGroups());
        facade.CreateTeacher("И", "Ф", new() { "G" });
        facade.CreateCriterion("A", "Описание", 10, 0.5);
        Assert.Single(facade.GetCriteria());
        Assert.Same(rubric.Criteria[0], facade.GetCriteria()[0]);
        Assert.Throws<ArgumentException>(() => facade.CreateCriterion(" a ", "Иное", 10, 0.5));
        Assert.Equal(1, rubric.Criteria.Count);
    }

    private static (InMemorySchoolData, SchoolFacade, Rubric) Build()
    {
        var school = new InMemorySchoolData();
        var rubric = new Rubric();
        var sf = new StudentFactory(school);
        var tf = new TeacherFactory(school);
        var importer = new SchoolImportService(new FakeSchoolReader(), sf, tf, school, rubric);
        return (school, new SchoolFacade(sf, tf, school, rubric, importer), rubric);
    }
}

internal class FakeSchoolReader : ISchoolDataReader
{
    public IReadOnlyList<StudentImportRow> Students { get; set; } = Array.Empty<StudentImportRow>();
    public IReadOnlyList<TeacherImportRow> Teachers { get; set; } = Array.Empty<TeacherImportRow>();
    public IReadOnlyList<CriterionImportRow> Criteria { get; set; } = Array.Empty<CriterionImportRow>();
    public IReadOnlyList<StudentImportRow> ReadStudents(string path) => Students;
    public IReadOnlyList<TeacherImportRow> ReadTeachers(string path) => Teachers;
    public IReadOnlyList<CriterionImportRow> ReadCriteria(string path) => Criteria;
}
