
namespace POS.ConsoleUI;

public class ConsoleController
{
    private readonly StudentScenario _studentScenario;
    private readonly TeacherScenario _teacherScenario;
    private readonly ManagerScenario _managerScenario;

    public ConsoleController(
        StudentScenario studentScenario,
        TeacherScenario teacherScenario,
        ManagerScenario managerScenario)
    {
        _studentScenario = studentScenario;
        _teacherScenario = teacherScenario;
        _managerScenario = managerScenario;
    }

    public void Run()
    {
        Console.WriteLine("Добро пожаловать в ПОС!");
        Console.WriteLine("Выберите роль:");
        Console.WriteLine("1. Студент");
        Console.WriteLine("2. Преподаватель");
        Console.WriteLine("3. Менеджер");

        try
        {
            string choice = ConsoleInput.ReadChoice(
                "Ваш выбор: ", "1", "2", "3");

            IUserScenario scenario = choice switch
            {
                "1" => _studentScenario,
                "2" => _teacherScenario,
                "3" => _managerScenario,
                _ => throw new InvalidOperationException()
            };

            scenario.Execute();
        }
        catch (FormatException ex)
        {
            Console.WriteLine($"Ошибка формата: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"Недостаточно прав: {ex.Message}");
        }
        catch (EndOfStreamException)
        {
            Console.WriteLine("Ввод завершён.");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Ошибка чтения/записи: {ex.Message}");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
        }
    }
}
