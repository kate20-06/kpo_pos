
using POS.Application;
using POS.Domain;

namespace POS.ConsoleUI;

public class TeacherScenario : IUserScenario
{
    private readonly TeacherFacade _facade;

    public TeacherScenario(TeacherFacade facade)
    {
        _facade = facade;
    }

    public void Execute()
    {
        Console.WriteLine("\nОценивание студентов");

        EvaluationSession session;

        while (true)
        {
            string name = ConsoleInput.ReadText(
                "Имя преподавателя: ");

            string surname = ConsoleInput.ReadText(
                "Фамилия преподавателя: ");

            try
            {
                session = _facade.GetEvaluationSession(name, surname);
                break;
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex.Message);

                Console.WriteLine("1. Попробовать ещё раз");
                Console.WriteLine("0. Вернуться в главное меню");

                string choice = ConsoleInput.ReadChoice(
                    "Выбор: ", "1", "0");

                if (choice == "0")
                    return;
            }
        }
        
        if (session.Students.Count == 0)
        {
            Console.WriteLine("Студентов нет.");
            return;
        }

        if (session.Criteria.Count == 0)
        {
            Console.WriteLine("Критерии не заданы.");
            return;
        }

        foreach (var student in session.Students)
        {
            Console.WriteLine(
                $"\nСтудент: {student.Name} {student.Surname}");

            var criteria = _facade.GetUnratedCriteria(
                session.Teacher.Id,
                student.Id
            );

            if (criteria.Count == 0)
            {
                Console.WriteLine(
                    "Вы уже выставили все оценки этому студенту.");
                continue;
            }

            var scores = new Dictionary<string, int>();

            foreach (var criterion in criteria)
            {
                int score = ConsoleInput.ReadInt(
                    $"{criterion.Name} (0–{criterion.MaxScore}): ",
                    0,
                    criterion.MaxScore
                );

                scores[criterion.Name] = score;
            }

            try
            {
                _facade.SubmitGrades(
                    session.Teacher.Id,
                    student.Id,
                    scores
                );

                Console.WriteLine("Оценки сохранены.");
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

        Console.WriteLine("\nОценивание завершено.");
    }
}
