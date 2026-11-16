namespace POS.Domain;

public interface IStudentEvaluationRepository
{
    StudentEvaluation? GetByStudentId(Guid studentId);

    void Save(StudentEvaluation evaluation);
}