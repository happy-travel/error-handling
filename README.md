```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddProblemDetailsFactory();
        ...
    }


    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        var logger = _loggerFactory.CreateLogger<Startup>();
        app.UseProblemDetailsExceptionHandler(env, logger);

        ...
    }
}
```
