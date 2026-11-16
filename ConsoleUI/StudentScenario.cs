
using POS.Application;

namespace POS.ConsoleUI;

public class StudentScenario : IUserScenario
{
    private readonly ReportFacade _reports;

    public StudentScenario(ReportFacade reports)
    {
        _reports = reports;
    }

    public void Execute()
    {
        Console.WriteLine("\nУспеваемость студента");

        StudentReport report;

        while (true)
        {
            string name = ConsoleInput.ReadText("Имя: ");
            string surname = ConsoleInput.ReadText("Фамилия: ");
            string group = ConsoleInput.ReadText("Группа: ");

            try
            {
                report = _reports.GetStudentReport(
                    name, surname, group);
                break;
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("1. Повторить ввод, 0. Вернуться в главное меню");
                if (ConsoleInput.ReadChoice("Выбор: ", "1", "0") == "0") return;
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }
        }

        ReportPrinter.Print(report);
    }
}
