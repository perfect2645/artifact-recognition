using artifact.service.domain.Configurations.Files;
using NetUtils.Aspnet.Configurations;

namespace artifact.service.domain.Configurations.Services
{
    public static class ServicesRegister
    {
        extension(WebApplicationBuilder builder)
        {
            public void RegisterServices()
            {
                builder.RegisterAssembliesAutofac(
                [
                    typeof(Program).Assembly,
                ]);
                builder.RegisterMiddlewares();
                builder.Configurations();
            }

            private void RegisterMiddlewares()
            {
                // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
                //builder.Services.AddOpenApi();
                builder.Services.AddSignalR();
            }

            private void Configurations()
            {
                builder.Services.Configure<SignalRSettings>(builder.Configuration.GetSection(Constants.SignalRSettings));
                builder.Services.Configure<FileStorage>(builder.Configuration.GetSection(Constants.FIleStorage));
            }
        }
    }
}
