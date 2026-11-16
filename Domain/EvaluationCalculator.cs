namespace POS.Domain;

public class EvaluationCalculator
{
    public Dictionary<string, double?> CalculateAverages(StudentEvaluation evaluation, Rubric rubric)
    {
        ArgumentNullException.ThrowIfNull(evaluation);
        ArgumentNullException.ThrowIfNull(rubric);
        var grouped = evaluation.Grades
            .GroupBy(g => g.Criterion.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Average(x => (double)x.Score),
                StringComparer.OrdinalIgnoreCase);
        var result = new Dictionary<string, double?>(StringComparer.OrdinalIgnoreCase);
        foreach (var criterion in rubric.Criteria)
            result[criterion.Name] = grouped.TryGetValue(criterion.Name, out var value) ? value : null;
        return result;
    }

    public double? CalculateFinalScore(StudentEvaluation evaluation, Rubric rubric)
    {
        var averages = CalculateAverages(evaluation, rubric);
        var rated = rubric.Criteria
            .Where(c => averages[c.Name].HasValue)
            .ToList();
        if (rated.Count == 0) return null;
        var maxWeight = rated.Max(c => c.Weight);
        double weightedSum = 0;
        double totalWeight = 0;
        foreach (var criterion in rated)
        {
            double scaledWeight = criterion.Weight / maxWeight;
            weightedSum += averages[criterion.Name]!.Value / criterion.MaxScore * scaledWeight;
            totalWeight += scaledWeight;
        }
        return weightedSum / totalWeight * 100;
    }
}
