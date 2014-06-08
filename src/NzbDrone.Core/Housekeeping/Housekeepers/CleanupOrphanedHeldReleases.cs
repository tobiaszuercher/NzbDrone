using NLog;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupOrphanedHeldReleases : IHousekeepingTask
    {
        private readonly IDatabase _database;
        private readonly Logger _logger;

        public CleanupOrphanedHeldReleases(IDatabase database, Logger logger)
        {
            _database = database;
            _logger = logger;
        }

        public void Clean()
        {
            _logger.Debug("Running orphaned held releases cleanup");

            var mapper = _database.GetDataMapper();

            mapper.ExecuteNonQuery(@"DELETE FROM HeldReleases
                                     WHERE Id IN (
                                     SELECT HeldReleases.Id FROM HeldReleases
                                     LEFT OUTER JOIN Series
                                     ON HeldReleases.SeriesId = Series.Id
                                     WHERE Series.Id IS NULL)");
        }
    }
}
