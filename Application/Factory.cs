using POS.Domain;

namespace POS.Application;

public abstract class Factory<T>
{
    protected readonly ISchoolData School;

    protected Factory(ISchoolData school)
    {
        School = school;
    }

    public T Create(
        string name,
        string surname,
        List<string> groupNames)
    {
        if (string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(surname))
        {
            throw new ArgumentException(
                "Имя и фамилия не могут быть пустыми"
            );
        }

        if (groupNames == null ||
            groupNames.Count == 0 ||
            groupNames.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException(
                "Необходимо указать хотя бы одну группу"
            );
        }

        ValidateGroups(groupNames);
        ValidateCreation(name.Trim(), surname.Trim(), groupNames);

        var groups = groupNames
            .Select(School.GetOrCreateGroup)
            .ToList();

        T entity = CreateEntity(
            name.Trim(),
            surname.Trim(),
            groups
        );

        Save(entity);

        return entity;
    }

    protected virtual void ValidateGroups(List<string> groupNames)
    {
    }

    protected virtual void ValidateCreation(string name, string surname, List<string> groupNames)
    {
    }

    protected abstract T CreateEntity(
        string name,
        string surname,
        List<Group> groups
    );

    protected abstract void Save(T entity);
}