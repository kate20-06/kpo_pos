
using Microsoft.Extensions.DependencyInjection;
using POS.Application;
using POS.Domain;
using POS.Infrastructure;

namespace POS.ConsoleUI;

public static class DependencyInjection
{
    public static IServiceCollection AddPosServices(
        this IServiceCollection services)
    {
        services.AddSingleton<ISchoolData, InMemorySchoolData>();

        services.AddSingleton<Rubric>();

        services.AddSingleton<EvaluationCalculator>();

        services.AddSingleton<
            IStudentEvaluationRepository,
            InMemoryStudentEvaluationRepository>();

        services.AddTransient<
            IReportExporter,
            CsvReportExporter>();

        services.AddTransient<
            Factory<Student>,
            StudentFactory>();

        services.AddTransient<
            Factory<Teacher>,
            TeacherFactory>();

        services.AddTransient<SchoolFacade>();
        services.AddTransient<SchoolImportService>();
        services.AddTransient<ISchoolDataReader, CsvSchoolDataReader>();
        services.AddTransient<TeacherFacade>();
        services.AddTransient<ReportFacade>();

        services.AddTransient<StudentScenario>();
        services.AddTransient<TeacherScenario>();
        services.AddTransient<ManagerScenario>();

        services.AddTransient<ConsoleController>();

        return services;
    }
}
