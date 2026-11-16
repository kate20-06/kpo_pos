using POS.Domain;

namespace POS.Infrastructure;

public class InMemoryStudentEvaluationRepository
    : IStudentEvaluationRepository
{
    private readonly Dictionary<Guid, StudentEvaluation>
        _evaluations = new();

    public StudentEvaluation? GetByStudentId(Guid studentId)
    {
        return _evaluations.GetValueOrDefault(studentId);
    }

    public void Save(StudentEvaluation evaluation)
    {
        _evaluations[evaluation.StudentId] = evaluation;
    }
}