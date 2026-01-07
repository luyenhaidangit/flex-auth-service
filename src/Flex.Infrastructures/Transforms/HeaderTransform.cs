using Flex.Infrastructures.Headers;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace Flex.Infrastructures.Transforms
{
    public class HeaderTransform : ITransformProvider
    {
        public void Apply(TransformBuilderContext context)
        {
            context.AddRequestTransform(ctx =>
            {
                var headers = ctx.ProxyRequest.Headers;

                headers.Remove(HeaderNames.Cookie);
                headers.Remove(HeaderNames.Referer);

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
