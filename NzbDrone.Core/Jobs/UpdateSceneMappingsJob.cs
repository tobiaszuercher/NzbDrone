using System;
using System.Linq;
using NzbDrone.Core.Model.Notification;
using NzbDrone.Core.Providers;

namespace NzbDrone.Core.Jobs
{
    public class UpdateSceneMappingsJob : IJob
    {
        private readonly SceneMappingProvider _sceneNameMappingProvider;
        private readonly XemProvider _xemProvider;

        public UpdateSceneMappingsJob(SceneMappingProvider sceneNameMappingProvider, XemProvider xemProvider)
        {
            _sceneNameMappingProvider = sceneNameMappingProvider;
            _xemProvider = xemProvider;
        }

        public UpdateSceneMappingsJob()
        {

        }

        public string Name
        {
            get { return "Update Scene Mappings"; }
        }

        public TimeSpan DefaultInterval
        {
            get { return TimeSpan.FromHours(12); }
        }

        public virtual void Start(ProgressNotification notification, dynamic options)
        {
            _sceneNameMappingProvider.UpdateMappings();
            _xemProvider.UpdateAlternateNames();
        }
    }
}