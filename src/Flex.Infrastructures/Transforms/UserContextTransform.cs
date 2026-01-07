using Flex.Infrastructures.Authentication;
using Flex.Infrastructures.Headers;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace Flex.Infrastructures.Transforms
{
    /// <summary>
    /// YARP transform that extracts authenticated user claims and forwards them as headers to downstream services.
    /// </summary>
    public class UserContextTransform : ITransformProvider
    {
        public void Apply(TransformBuilderContext context)
        {
            context.AddRequestTransform(ctx =>
            {
                var user = ctx.HttpContext.User;

                if (user?.Identity?.IsAuthenticated != true)
                {
                    return ValueTask.CompletedTask;
                }    
                    
                var headers = ctx.ProxyRequest.Headers;

                // User context headers
                var userId = user.FindFirst(ClaimTypes.Sub)?.Value ?? string.Empty;

                if (!string.IsNullOrEmpty(userId))
                {
                    headers.TryAddWithoutValidation(HeaderNames.UserId, userId);
                }

                // Add more here

                return ValueTask.CompletedTask;
            });
        }

        public void ValidateCluster(TransformClusterValidationContext context)
        {
        }

        public void ValidateRoute(TransformRouteValidationContext context)
        {
        }
    }
}
