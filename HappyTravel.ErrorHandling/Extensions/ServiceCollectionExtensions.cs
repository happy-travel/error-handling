using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace HappyTravel.ErrorHandling.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddProblemDetailsFactory(this IServiceCollection services)
            => services.AddTransient<ProblemDetailsFactory, PublicProblemDetailsFactory>()
                .AddTransient<PublicProblemDetailsFactory>();
    }
}
