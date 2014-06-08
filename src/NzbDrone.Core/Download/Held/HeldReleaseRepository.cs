using System;
using System.Collections.Generic;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Download.Held
{
    public interface IHeldReleaseRepository : IBasicRepository<HeldRelease>
    {
        List<HeldRelease> Expired();
        void DeleteBySeriesId(int seriesId);
    }

    public class HeldReleaseRepository : BasicRepository<HeldRelease>, IHeldReleaseRepository
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

        public void DeleteBySeriesId(int seriesId)
        {
            Delete(r => r.SeriesId == seriesId);
        }
    }
}