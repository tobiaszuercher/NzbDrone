using Nancy;
using Nancy.Bootstrapper;
using NzbDrone.Common.EnvironmentInfo;

namespace NzbDrone.Api.Extensions.Pipelines
{
    public class NzbDroneVersionPipeline : IRegisterNancyPipeline
    {
        public void Register(IPipelines pipelines)
        {
            pipelines.AfterRequest.AddItemToStartOfPipeline(Handle);
        }

        private void Handle(NancyContext context)
        {
            var header = "X-ApplicationVersion";

            if (context.Response.Headers.ContainsKey(header))
            {
                return;
            }

            context.Response.Headers.Add(header, BuildInfo.Version.ToString());
        }
    }
}