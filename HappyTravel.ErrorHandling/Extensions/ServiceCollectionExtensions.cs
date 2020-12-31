using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace HappyTravel.ErrorHandling.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddProblemDetailsErrorHandling(this IServiceCollection services)
            => services.AddHttpContextAccessor()
                .AddTransient<ProblemDetailsFactory, PublicProblemDetailsFactory>()
                .AddTransient<PublicProblemDetailsFactory>();
    }
}
