
using System.Globalization;

namespace POS.ConsoleUI;

public static class ConsoleInput
{
    public static string ReadText(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);

            string? input = Console.ReadLine();

            if (input == null)
                throw new EndOfStreamException(
                    "Ввод завершён");

            if (!string.IsNullOrWhiteSpace(input))
                return input.Trim();

            Console.WriteLine(
                "Поле не может быть пустым. Попробуйте ещё раз.");
        }
    }

    public static int ReadInt(
        string prompt,
        int min,
        int max)
    {
        while (true)
        {
            string input = ReadText(prompt);

            if (int.TryParse(input, out int value) &&
                value >= min &&
                value <= max)
            {
                return value;
            }

            Console.WriteLine(
                $"Введите целое число от {min} до {max}.");
        }
    }

    public static double ReadPositiveDouble(string prompt)
    {
        while (true)
        {
            string input = ReadText(prompt);

            input = input.Replace(',', '.');

            if (double.TryParse(
                input,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double value) &&
                double.IsFinite(value) &&
                value > 0)
            {
                return value;
            }

            Console.WriteLine(
                "Введите положительное число.");
        }
    }

    public static bool ReadYesNo(string prompt)
    {
        while (true)
        {
            string answer = ReadText(prompt);

            if (answer.Equals("д", StringComparison.OrdinalIgnoreCase))
                return true;

            if (answer.Equals("н", StringComparison.OrdinalIgnoreCase))
                return false;

            Console.WriteLine("Введите «д» или «н». Попробуйте ещё раз.");
        }
    }

    public static string ReadChoice(
        string prompt,
        params string[] allowed)
    {
        while (true)
        {
            string choice = ReadText(prompt);

            if (allowed.Contains(choice))
                return choice;

            Console.WriteLine(
                "Неизвестная команда. Попробуйте ещё раз.");
        }
    }
}
