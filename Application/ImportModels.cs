namespace POS.Application;

public record StudentImportRow(string Name, string Surname, string Group);
public record TeacherImportRow(string Name, string Surname, IReadOnlyList<string> Groups);
public record CriterionImportRow(string Name, string Text, int MaxScore, double Weight);
public record ImportResult(int Imported, int Skipped);

public interface ISchoolDataReader
{
    IReadOnlyList<StudentImportRow> ReadStudents(string path);
    IReadOnlyList<TeacherImportRow> ReadTeachers(string path);
    IReadOnlyList<CriterionImportRow> ReadCriteria(string path);
}
