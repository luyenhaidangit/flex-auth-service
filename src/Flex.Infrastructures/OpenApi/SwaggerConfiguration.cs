using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Flex.Infrastructures.OpenApi
{
    public static class SwaggerConfiguration
    {
        /// <summary>
        /// Required setings: Directory.Build.props.
        /// <GenerateDocumentationFile>true</GenerateDocumentationFile>
        /// <NoWarn>$(NoWarn);1591</NoWarn>
        /// </summary>
        public static void ConfigureSwagger(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
                {
                    Title = "Flex.Auth",
                    Version = "v1"
                });

                c.DocumentFilter<LowerCaseDocumentFilter>();

                var entryAssembly = Assembly.GetEntryAssembly();
                if (entryAssembly == null)
                {
                    return;
                }

                var xmlFile = $"{entryAssembly.GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
                }
            });
        }
    }
}
