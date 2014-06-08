using System;
using System.Collections.Generic;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Download.Queue
{
    public interface IQueuedReleaseRepository : IBasicRepository<HeldRelease>
    {
        List<HeldRelease> Expired();
    }

    public class HeldReleaseRepository : BasicRepository<HeldRelease>, IQueuedReleaseRepository
    {
        public HeldReleaseRepository(IDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public List<HeldRelease> Expired()
        {
            var currentTime = DateTime.UtcNow;

            return Query.Where(r => r.Expiry < currentTime);
        }
    }
}