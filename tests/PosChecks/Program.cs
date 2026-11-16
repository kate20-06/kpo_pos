using POS.Application;
using POS.Domain;
using POS.Infrastructure;

int checks = 0;
void Check(bool condition, string message)
{
    checks++;
    if (!condition) throw new Exception("Провален тест: " + message);
}
void Reject<T>(Action action, string message) where T : Exception
{
    checks++;
    try { action(); }
    catch (T) { return; }
    throw new Exception("Ожидалось исключение " + typeof(T).Name + ": " + message);
}

Reject<ArgumentException>(() => new Student(Guid.Empty, "И", "Ф", Guid.NewGuid()), "пустой ID студента");
Reject<ArgumentException>(() => new Student(Guid.NewGuid(), " ", "Ф", Guid.NewGuid()), "пустое имя");
Reject<ArgumentException>(() => new Student(Guid.NewGuid(), "И", "Ф", Guid.Empty), "пустая группа");
Reject<ArgumentException>(() => new Teacher(Guid.NewGuid(), "", "Ф"), "пустое имя преподавателя");
Reject<ArgumentException>(() => new Group(Guid.NewGuid(), " "), "пустое название группы");
Reject<ArgumentException>(() => new Teacher(Guid.NewGuid(), "И", "Ф").AddGroup(Guid.Empty), "пустой ID группы");
Reject<ArgumentException>(() => new Criterion("", "Описание", 10, 1), "пустое имя критерия");
Reject<ArgumentException>(() => new Criterion("Тест", " ", 10, 1), "пустое описание критерия");
Reject<ArgumentOutOfRangeException>(() => new Criterion("Тест", "Описание", 0, 1), "нулевой максимум");
Reject<ArgumentOutOfRangeException>(() => new Criterion("Тест", "Описание", -1, 1), "отрицательный максимум");
foreach (var weight in new[] { 0, -1, double.NaN, double.PositiveInfinity, double.NegativeInfinity })
    Reject<ArgumentOutOfRangeException>(() => new Criterion("Тест", "Описание", 10, weight), "вес " + weight);

var rubric = new Rubric();
var activity = new Criterion("Активность", "Описание", 10, 0.3);
var homework = new Criterion("Домашняя работа", "Описание", 20, 0.7);
rubric.AddCriterion(activity);
rubric.AddCriterion(homework);
Reject<ArgumentException>(() => rubric.AddCriterion(new Criterion(" активность ", "Описание", 5, 1)), "дубликат критерия регистронезависимо");
var studentId = Guid.NewGuid();
var teacherId = Guid.NewGuid();
var anotherTeacher = Guid.NewGuid();
var evaluation = new StudentEvaluation(studentId);
Reject<ArgumentException>(() => new StudentEvaluation(Guid.Empty), "пустой ID оценки");
Reject<ArgumentOutOfRangeException>(() => new CriterionGrade(activity, teacherId, -1), "отрицательный балл");
Reject<ArgumentOutOfRangeException>(() => new CriterionGrade(activity, teacherId, 11), "балл больше максимума");
Reject<ArgumentException>(() => new CriterionGrade(activity, Guid.Empty, 5), "пустой ID преподавателя");
Reject<InvalidOperationException>(() => evaluation.AddGrade(
    new CriterionGrade(new Criterion("Активность", "Другая", 10, 0.3), teacherId, 3), rubric),
    "чужой критерий даже при одинаковом названии");
Check(evaluation.Grades.Count == 0, "плохая оценка не изменяет агрегат");
evaluation.AddGrade(new CriterionGrade(activity, teacherId, 10), rubric);
evaluation.AddGrade(new CriterionGrade(activity, anotherTeacher, 0), rubric);
evaluation.AddGrade(new CriterionGrade(homework, teacherId, 20), rubric);
Reject<InvalidOperationException>(() => evaluation.AddGrade(new CriterionGrade(activity, teacherId, 3), rubric), "повторная оценка");
Check(evaluation.Grades.Count == 3, "три выставленные оценки");
var calculator = new EvaluationCalculator();
var averages = calculator.CalculateAverages(evaluation, rubric);
Check(Math.Abs(averages["Активность"]!.Value - 5) < 1e-8, "среднее 10 и 0");
Check(Math.Abs(calculator.CalculateFinalScore(evaluation, rubric)!.Value - 85) < 1e-8, "взвешенный итог 85%");
Check(calculator.CalculateFinalScore(new StudentEvaluation(Guid.NewGuid()), rubric) == null, "нет оценок — нет итога");

var bigRubric = new Rubric();
var a = new Criterion("A", "Описание", 10, 1e308);
var b = new Criterion("B", "Описание", 10, 1e308);
bigRubric.AddCriterion(a);
bigRubric.AddCriterion(b);
var bigEvaluation = new StudentEvaluation(Guid.NewGuid());
bigEvaluation.AddGrade(new CriterionGrade(a, teacherId, 10), bigRubric);
bigEvaluation.AddGrade(new CriterionGrade(b, teacherId, 0), bigRubric);
Check(Math.Abs(calculator.CalculateFinalScore(bigEvaluation, bigRubric)!.Value - 50) < 1e-8,
    "конечные большие веса не переполняют знаменатель");

var school = new InMemorySchoolData();
var csvReader = new CsvSchoolDataReader();
var importer = new SchoolImportService(csvReader, new StudentFactory(school),
    new TeacherFactory(school), school, rubric);
var schoolFacade = new SchoolFacade(new StudentFactory(school), new TeacherFactory(school),
    school, rubric, importer);
Reject<ArgumentException>(() => schoolFacade.CreateGroup("G1", new List<(string, string)>{ ("Вася", "Иванов"), ("вася", "иванов") }), "дубли внутри группы");
Check(school.Students.Count == 0 && school.Groups.Count == 0, "ошибка группы без частичного создания");
var firstStudent = schoolFacade.CreateStudent("Вася", "Иванов", "G1");
Reject<InvalidOperationException>(() => schoolFacade.CreateStudent(" васЯ ", "иванов", "g1"), "ручной дубль студента");
Check(school.Students.Count == 1 && school.Groups.Count == 1, "ручной дубль не добавился");
var teacher = schoolFacade.CreateTeacher("Ольга", "Петрова", new List<string>{"G1", "G2"});
Reject<InvalidOperationException>(() => schoolFacade.CreateTeacher("ольга", "ПЕТРОВА", new List<string>{"G3"}), "ручной дубль преподавателя");
Check(school.Groups.Count == 2, "дубликат учителя не создаёт новую группу");

var repository = new InMemoryStudentEvaluationRepository();
var teacherFacade = new TeacherFacade(school, rubric, repository);
Reject<ArgumentOutOfRangeException>(() => teacherFacade.SubmitGrades(teacher.Id, firstStudent.Id,
    new Dictionary<string,int>{{"Активность",5},{"Домашняя работа",21}}), "проверка пакета оценок");
Check(repository.GetByStudentId(firstStudent.Id) == null, "плохой пакет не сохраняется");
teacherFacade.SubmitGrades(teacher.Id, firstStudent.Id, new Dictionary<string,int>{{"Активность", 8}});
Check(teacherFacade.GetUnratedCriteria(teacher.Id, firstStudent.Id).Count == 1, "частично оценённый студент");
Reject<InvalidOperationException>(() => teacherFacade.SubmitGrades(teacher.Id, firstStudent.Id,
    new Dictionary<string,int>{{"Активность", 5}}), "повторный SubmitGrades");

var temp = Path.Combine(Path.GetTempPath(), "pos-checks-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(temp);
try
{
    string StudentsCsv = Path.Combine(temp, "students.csv");
    File.WriteAllText(StudentsCsv, "Name;Surname;Group\nВася;Иванов;G1\nАнна;Сидорова;G1\nанна;сидорова;g1\n\"Маша;Мария\";\"Ива\"\"нова\";G2\n");
    var s1 = schoolFacade.ImportStudents(StudentsCsv);
    Check(s1.Imported == 2 && s1.Skipped == 2, "дубли CSV существующие и внутри файла, кавычки");
    var s2 = schoolFacade.ImportStudents(StudentsCsv);
    Check(s2.Imported == 0 && s2.Skipped == 4, "идемпотентный импорт студентов");
    Check(school.Students.Count == 3, "ровно три студента");

    var badCsv = Path.Combine(temp, "broken.csv");
    File.WriteAllText(badCsv, "Name;Surname;Group\nНовый;Студент;G9\nНеполная;Строка\n");
    Reject<FormatException>(() => schoolFacade.ImportStudents(badCsv), "ошибка столбцов");
    Check(!school.Groups.Any(g => g.Name == "G9"), "ошибочный файл без частичного добавления");
    File.WriteAllText(badCsv, "Bad;Header;Group\nМ;Ф;G8\n");
    Reject<FormatException>(() => schoolFacade.ImportStudents(badCsv), "неправильный заголовок");
    File.WriteAllText(badCsv, "Name;Surname;Group\nМ;Ф;G8\n;Х;G8\n");
    Reject<FormatException>(() => schoolFacade.ImportStudents(badCsv), "пустые обязательные поля");

    var teachersCsv = Path.Combine(temp, "teachers.csv");
    File.WriteAllText(teachersCsv, "Name;Surname;Groups\nОльга;Петрова;G1|G2\nАлексей;Смирнов;G1|G3\n");
    var tr = schoolFacade.ImportTeachers(teachersCsv);
    Check(tr.Imported == 1 && tr.Skipped == 1, "преподаватели импорт: дубль пропущен");
    Check(school.Teachers.Count == 2 && school.Groups.Any(g => g.Name == "G3"), "импорт преподавателя создал группы");

    var criteriaCsv = Path.Combine(temp, "criteria.csv");
    File.WriteAllText(criteriaCsv, "Name;Text;MaxScore;Weight\nАктивность;Повтор;10;0.3\nПосещаемость;Ок;10;0,5\n");
    var cr = schoolFacade.ImportCriteria(criteriaCsv);
    Check(cr.Imported == 1 && cr.Skipped == 1, "критерии импорт с запятой в весе");
    File.WriteAllText(criteriaCsv, "Name;Text;MaxScore;Weight\nНормальный;Описание;10;1\nПлохой;Описание;10;NaN\n");
    Reject<FormatException>(() => schoolFacade.ImportCriteria(criteriaCsv), "NaN запрещён");
    Check(rubric.Find("Нормальный") == null, "невалидный CSV критериев не меняет рубрикатор");

    var reportFacade = new ReportFacade(school, rubric, repository, calculator, new CsvReportExporter());
    var report = reportFacade.GetStudentReport("Вася", "Иванов", "G1");
    Check(report.Grades.Count == 1 && report.Grades[0].Score == 8, "просмотр исходных оценок");
    var exportPath = Path.Combine(temp, "results.csv");
    reportFacade.ExportGroupReport("G1", exportPath);
    Check(File.Exists(exportPath) && File.ReadAllText(exportPath).Contains("Итог (%)"), "экспорт ведомости");
}
finally
{
    Directory.Delete(temp, recursive: true);
}
Console.WriteLine($"Проверки пройдены: {checks}");
