using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Download.Held;
using NzbDrone.Core.Housekeeping.Housekeepers;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Test.Housekeeping.Housekeepers
{
    [TestFixture]
    public class CleanupOrphanedHeldReleasesFixture : DbTest<CleanupOrphanedHeldReleases, HeldRelease>
    {
        [Test]
        public void should_delete_orphaned_blacklist_items()
        {
            var heldRelease = Builder<HeldRelease>.CreateNew()
                                                  .BuildNew();

            Db.Insert(heldRelease);
            Subject.Clean();
            AllStoredModels.Should().BeEmpty();
        }

        [Test]
        public void should_not_delete_unorphaned_blacklist_items()
        {
            var series = Builder<Series>.CreateNew().BuildNew();

            Db.Insert(series);

            var heldRelease = Builder<HeldRelease>.CreateNew()
                                               .With(h => h.SeriesId = series.Id)
                                               .BuildNew();

            Db.Insert(heldRelease);

            Subject.Clean();
            AllStoredModels.Should().HaveCount(1);
        }
    }
}
