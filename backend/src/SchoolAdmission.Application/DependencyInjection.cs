using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SchoolAdmission.Application.Admissions;
using SchoolAdmission.Application.Classes;

namespace SchoolAdmission.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, HoldSettings holdSettings)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, ServiceLifetime.Singleton);
        services.AddSingleton(holdSettings);
        services.AddScoped<IClassService, ClassService>();
        services.AddScoped<IAdmissionService, AdmissionService>();

        return services;
    }
}
