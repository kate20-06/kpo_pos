
using POS.Application;

namespace POS.ConsoleUI;

public class ManagerScenario : IUserScenario
{
    private readonly ReportFacade _reports;
    private readonly SchoolFacade _school;

    public ManagerScenario(
        ReportFacade reports,
        SchoolFacade school)
    {
        _reports = reports;
        _school = school;
    }

    public void Execute()
    {
        while (true)
        {
            Console.WriteLine("\nМенеджер");
            Console.WriteLine("1. Результаты студента");
            Console.WriteLine("2. Результаты группы");
            Console.WriteLine("3. Экспорт результатов группы");
            Console.WriteLine("4. Добавить студента");
            Console.WriteLine("5. Добавить группу студентов");
            Console.WriteLine("6. Добавить преподавателя");
            Console.WriteLine("7. Добавить критерий");
            Console.WriteLine("8. Посмотреть критерии");
            Console.WriteLine("9. Импорт CSV");
            Console.WriteLine("10. Посмотреть список студентов");
            Console.WriteLine("11. Посмотреть список преподавателей");
            Console.WriteLine("0. Вернуться к выбору роли");

            string choice = ConsoleInput.ReadChoice(
                "Выбор: ",
                "0", "1", "2", "3", "4",
                "5", "6", "7", "8", "9", "10", "11");

            if (choice == "0")
                return;

            try
            {
                switch (choice)
                {
                    case "1":
                        ShowStudent();
                        break;

                    case "2":
                        ShowGroup();
                        break;

                    case "3":
                        ExportGroup();
                        break;

                    case "4":
                        CreateStudent();
                        break;

                    case "5":
                        CreateGroup();
                        break;

                    case "6":
                        CreateTeacher();
                        break;

                    case "7":
                        CreateCriterion();
                        break;

                    case "8":
                        ShowCriteria();
                        break;

                    case "9":
                        ImportCsv();
                        break;

                    case "10":
                        ShowStudents();
                        break;
                    case "11":
                        ShowTeachers();
                        break;
                }
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
            catch (FormatException ex)
            {
                Console.WriteLine($"Неверный формат CSV: {ex.Message}");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Ошибка файла: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Нет доступа: {ex.Message}");
            }
        }
    }

    private void ShowStudent()
    {
        while (true)
        {
            string name = ConsoleInput.ReadText(
                "Имя студента: ");
            string surname = ConsoleInput.ReadText(
                "Фамилия студента: ");
            string group = ConsoleInput.ReadText("Группа: ");

            try
            {
                var report = _reports.GetStudentReport(
                    name, surname, group);

                PrintStudentReport(report);
                return;
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("1. Повторить поиск, 0. В меню менеджера");
                if (ConsoleInput.ReadChoice("Выбор: ", "1", "0") == "0") return;
            }
        }
    }

    private void ShowGroup()
    {
        while (true)
        {
            string groupName = ConsoleInput.ReadText(
                "Группа: ");

            try
            {
                var report = _reports.GetGroupReport(groupName);

                Console.WriteLine(
                    $"\nВедомость группы {report.Group.Name}");

                if (report.Students.Count == 0)
                {
                    Console.WriteLine(
                        "В группе нет студентов.");
                    return;
                }

                foreach (var student in report.Students)
                {
                    PrintStudentReport(student);
                }

                return;
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("1. Повторить поиск, 0. В меню менеджера");
                if (ConsoleInput.ReadChoice("Выбор: ", "1", "0") == "0") return;
            }
        }
    }

    private void ExportGroup()
    {
        string groupName = ConsoleInput.ReadText("Группа: ");
        string filePath = ConsoleInput.ReadText(
            "Путь к CSV-файлу: ");

        _reports.ExportGroupReport(groupName, filePath);

        Console.WriteLine("Результаты экспортированы.");
    }

    private void CreateStudent()
    {
        string name = ConsoleInput.ReadText(
            "Имя студента: ");
        string surname = ConsoleInput.ReadText(
            "Фамилия студента: ");
        string group = ConsoleInput.ReadText("Группа: ");

        _school.CreateStudent(name, surname, group);

        Console.WriteLine("Студент добавлен.");
    }

    private void CreateGroup()
    {
        string groupName = ConsoleInput.ReadText(
            "Название группы: ");

        int count = ConsoleInput.ReadInt(
            "Количество студентов: ", 1, 10000);

        var students = new List<(string Name, string Surname)>();

        for (int i = 0; i < count; i++)
        {
            Console.WriteLine($"\nСтудент {i + 1}:");

            string name = ConsoleInput.ReadText("Имя: ");
            string surname = ConsoleInput.ReadText("Фамилия: ");

            students.Add((name, surname));
        }

        _school.CreateGroup(groupName, students);

        Console.WriteLine("Группа студентов создана.");
    }

    private void CreateTeacher()
    {
        string name = ConsoleInput.ReadText(
            "Имя преподавателя: ");
        string surname = ConsoleInput.ReadText(
            "Фамилия преподавателя: ");

        int count = ConsoleInput.ReadInt(
            "Количество групп: ", 1, 1000);

        var groups = new List<string>();

        for (int i = 0; i < count; i++)
        {
            string groupName = ConsoleInput.ReadText(
                $"Название группы {i + 1}: ");

            groups.Add(groupName);
        }

        _school.CreateTeacher(name, surname, groups);

        Console.WriteLine("Преподаватель добавлен.");
    }

    private void CreateCriterion()
    {
        string name = ConsoleInput.ReadText(
            "Название критерия: ");

        string description = ConsoleInput.ReadText(
            "Описание: ");

        int maxScore = ConsoleInput.ReadInt(
            "Максимальный балл: ", 1, int.MaxValue);

        double weight = ConsoleInput.ReadPositiveDouble(
            "Вес критерия: ");

        _school.CreateCriterion(
            name, description, maxScore, weight);

        Console.WriteLine("Критерий добавлен.");
    }

    private void ShowCriteria()
    {
        var criteria = _school.GetCriteria();

        if (criteria.Count == 0)
        {
            Console.WriteLine("Критериев пока нет.");
            return;
        }

        Console.WriteLine("\nКритерии оценивания:");

        foreach (var criterion in criteria)
        {
            Console.WriteLine(
                $"{criterion.Name}: {criterion.Text}");

            Console.WriteLine(
                $"Максимум: {criterion.MaxScore}, " +
                $"вес: {criterion.Weight}");
        }
    }

    private void ImportCsv()
    {
        Console.WriteLine("\nИмпорт из CSV:");
        Console.WriteLine("1. Студенты (Name;Surname;Group)");
        Console.WriteLine("2. Преподаватели (Name;Surname;Groups)");
        Console.WriteLine("3. Критерии (Name;Text;MaxScore;Weight)");
        Console.WriteLine("0. Назад");
        var choice = ConsoleInput.ReadChoice("Выбор: ", "0", "1", "2", "3");
        if (choice == "0") return;
        var path = ConsoleInput.ReadText("Путь к CSV-файлу: ");
        var result = choice switch
        {
            "1" => _school.ImportStudents(path),
            "2" => _school.ImportTeachers(path),
            "3" => _school.ImportCriteria(path),
            _ => throw new InvalidOperationException("Неизвестный тип")
        };
        Console.WriteLine($"Импортировано: {result.Imported}; пропущено дубликатов: {result.Skipped}.");
    }

    private void ShowStudents()
    {
        var students = _school.GetStudents();
        var groups = _school.GetGroups().ToDictionary(g => g.Id, g => g.Name);
        if (students.Count == 0)
        {
            Console.WriteLine("Студентов пока нет.");
            return;
        }
        foreach (var student in students.OrderBy(s => groups[s.GroupId])
                     .ThenBy(s => s.Surname).ThenBy(s => s.Name))
        {
            Console.WriteLine($"{groups[student.GroupId]}: {student.Name} {student.Surname}");
        }
    }

    private void PrintStudentReport(StudentReport report) => ReportPrinter.Print(report);

    private void ShowTeachers()
    {
        var teachers = _school.GetTeachers();

        if (teachers.Count == 0)
        {
            Console.WriteLine("Преподавателей пока нет.");
            return;
        }

        var groups = _school.GetGroups()
            .ToDictionary(g => g.Id, g => g.Name);

        Console.WriteLine("\nСписок преподавателей:");

        foreach (var teacher in teachers
                    .OrderBy(t => t.Surname)
                    .ThenBy(t => t.Name))
        {
            var teacherGroups = teacher.GroupIds
                .Select(id => groups[id])
                .OrderBy(name => name);

            Console.WriteLine(
                $"{teacher.Surname} {teacher.Name} — " +
                $"группы: {string.Join(", ", teacherGroups)}");
        }
    }
}
