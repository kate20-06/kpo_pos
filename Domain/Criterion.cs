namespace POS.Domain;

public class Criterion
{
    public string Name { get; }
    public string Text { get; }
    public int MaxScore { get; }
    public double Weight { get; }

    public Criterion(string name, string text, int maxScore, double weight)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Название критерия не может быть пустым", nameof(name));
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Описание критерия не может быть пустым", nameof(text));
        if (maxScore <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxScore), "Максимальный балл должен быть положительным");
        if (!double.IsFinite(weight) || weight <= 0)
            throw new ArgumentOutOfRangeException(nameof(weight), "Вес критерия должен быть конечным положительным числом");

        Name = name.Trim();
        Text = text.Trim();
        MaxScore = maxScore;
        Weight = weight;
    }
}
