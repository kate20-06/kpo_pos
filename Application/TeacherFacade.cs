
using POS.Domain;

namespace POS.Application;

public record EvaluationSession(
    Teacher Teacher,
    IReadOnlyList<Student> Students,
    IReadOnlyList<Criterion> Criteria
);

public class TeacherFacade
{
    private readonly ISchoolData _school;
    private readonly Rubric _rubric;
    private readonly IStudentEvaluationRepository _evaluations;

    public TeacherFacade(
        ISchoolData school,
        Rubric rubric,
        IStudentEvaluationRepository evaluations)
    {
        _school = school;
        _rubric = rubric;
        _evaluations = evaluations;
    }

    public EvaluationSession GetEvaluationSession(
        string name,
        string surname)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(surname))
            throw new ArgumentException("Имя и фамилия преподавателя обязательны");
        var teacher = _school.Teachers.FirstOrDefault(t =>
            string.Equals(t.Name, name.Trim(),
                StringComparison.OrdinalIgnoreCase) &&
            string.Equals(t.Surname, surname.Trim(),
                StringComparison.OrdinalIgnoreCase)
        );

        if (teacher == null)
            throw new ArgumentException(
                "Преподаватель не найден");

        var students = _school.Students
            .Where(s => teacher.GroupIds.Contains(s.GroupId))
            .OrderBy(s => s.Surname)
            .ThenBy(s => s.Name)
            .ToList();

        return new EvaluationSession(
            teacher,
            students,
            _rubric.Criteria.ToList()
        );
    }

    public void SubmitGrades(
        Guid teacherId,
        Guid studentId,
        Dictionary<string, int> scores)
    {
        var teacher = _school.Teachers.FirstOrDefault(
            t => t.Id == teacherId);

        var student = _school.Students.FirstOrDefault(
            s => s.Id == studentId);

        if (teacher == null || student == null)
            throw new ArgumentException(
                "Студент или преподаватель не найден");

        if (!teacher.GroupIds.Contains(student.GroupId))
            throw new InvalidOperationException(
                "Преподаватель не работает с группой студента");

        if (scores == null || scores.Count == 0)
            throw new ArgumentException(
                "Нет оценок для сохранения");

        var evaluation = _evaluations.GetByStudentId(studentId)
            ?? new StudentEvaluation(studentId);

        var grades = new List<CriterionGrade>();
        var namesInRequest = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (criterionName, score) in scores)
        {
            var criterion = _rubric.Criteria.FirstOrDefault(
                c => string.Equals(c.Name, criterionName, StringComparison.OrdinalIgnoreCase));

            if (criterion == null)
                throw new ArgumentException(
                    $"Критерий '{criterionName}' не найден");

            if (!namesInRequest.Add(criterion.Name))
                throw new ArgumentException(
                    $"Критерий '{criterionName}' повторяется в переданном наборе");

            if (evaluation.Grades.Any(g =>
                g.TeacherId == teacherId &&
                string.Equals(g.Criterion.Name, criterionName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(
                    $"Оценка по критерию '{criterionName}' уже существует");
            }

            grades.Add(new CriterionGrade(
                criterion,
                teacherId,
                score
            ));
        }

        foreach (var grade in grades)
        {
            evaluation.AddGrade(grade, _rubric);
        }

        _evaluations.Save(evaluation);
    }

    public IReadOnlyList<Criterion> GetUnratedCriteria(
        Guid teacherId,
        Guid studentId)
    {
        var teacher = _school.Teachers.FirstOrDefault(
            t => t.Id == teacherId);

        var student = _school.Students.FirstOrDefault(
            s => s.Id == studentId);

        if (teacher == null || student == null)
            throw new ArgumentException(
                "Студент или преподаватель не найден");

        if (!teacher.GroupIds.Contains(student.GroupId))
            throw new InvalidOperationException(
                "Преподаватель не работает с группой студента");

        var evaluation = _evaluations.GetByStudentId(studentId);

        if (evaluation == null)
            return _rubric.Criteria.ToList();

        var ratedCriteria = evaluation.Grades
            .Where(g => g.TeacherId == teacherId)
            .Select(g => g.Criterion.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return _rubric.Criteria
            .Where(c => !ratedCriteria.Contains(c.Name))
            .ToList();
    }

}
