using Microsoft.Extensions.DependencyInjection;
using POS.Application;
using POS.ConsoleUI;
using POS.Domain;
using POS.Infrastructure;
using Xunit;

namespace PosUnitTests;

public class DependencyInjectionTests
{
    [Fact]
    public void AllScenariosAndFacades_ResolveSuccessfully()
    {
        var services = new ServiceCollection();
        services.AddPosServices();
        using var provider = services.BuildServiceProvider(validateScopes: true);
        Assert.NotNull(provider.GetRequiredService<ConsoleController>());
        Assert.NotNull(provider.GetRequiredService<StudentScenario>());
        Assert.NotNull(provider.GetRequiredService<TeacherScenario>());
        Assert.NotNull(provider.GetRequiredService<ManagerScenario>());
        Assert.NotNull(provider.GetRequiredService<ReportFacade>());
        Assert.NotNull(provider.GetRequiredService<TeacherFacade>());
        Assert.NotNull(provider.GetRequiredService<SchoolFacade>());
    }

    [Fact]
    public void SchoolRubricAndEvaluationRepository_AreSingletons()
    {
        var services = new ServiceCollection();
        services.AddPosServices();
        using var provider = services.BuildServiceProvider();
        Assert.Same(provider.GetRequiredService<ISchoolData>(), provider.GetRequiredService<ISchoolData>());
        Assert.IsType<InMemorySchoolData>(provider.GetRequiredService<ISchoolData>());
        Assert.Same(provider.GetRequiredService<Rubric>(), provider.GetRequiredService<Rubric>());
        Assert.Same(provider.GetRequiredService<IStudentEvaluationRepository>(),
            provider.GetRequiredService<IStudentEvaluationRepository>());
        Assert.NotSame(provider.GetRequiredService<SchoolFacade>(), provider.GetRequiredService<SchoolFacade>());
        Assert.Empty(provider.GetRequiredService<Rubric>().Criteria); // No preloaded criteria.
    }
}
