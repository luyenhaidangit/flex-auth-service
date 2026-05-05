using Flex.Infrastructures.Observability;
using Flex.Infrastructures.Responses;
using Serilog;

namespace Flex.Auth.Extensions
{
    public static class ApplicationExtensions
    {
        public static void UseInfrastructure(this WebApplication app)
        {
            app.UseForwardedHeaders();

            // Observability - must be in this order
            app.UseCorrelationId();
            app.UseGlobalLogging();
            
            app.UseRequestGuard();

            app.UseExceptionHandling();
            
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.DisplayRequestDuration();
                });
            }

            app.UseHttpsRedirection();

            app.UseCors();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            // Map Controllers
            app.MapControllers();

            app.UseRateLimiter();

            app.UseSerilogRequestLogging();
        }
    }
}
