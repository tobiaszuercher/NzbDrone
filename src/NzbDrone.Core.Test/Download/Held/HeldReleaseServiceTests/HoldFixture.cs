using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FizzWare.NBuilder;
using FluentAssertions;
using Marr.Data;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download.Held;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Download.Held.HeldReleaseServiceTests
{
    [TestFixture]
    public class HoldFixture : CoreTest<HeldReleaseService>
    {
        private DownloadDecision _approved;
        private DownloadDecision _rejected;
        private DownloadDecision _temporarilyRejected;
        private Series _series;
        private Episode _episode;
        private Profile _profile;
        private ReleaseInfo _release;
        private ParsedEpisodeInfo _parsedEpisodeInfo;
        private RemoteEpisode _remoteEpisode;

        [SetUp]
        public void Setup()
        {
            _series = Builder<Series>.CreateNew()
                                     .Build();

            _episode = Builder<Episode>.CreateNew()
                                       .Build();

            _profile = new Profile
                       {
                           Name = "Test",
                           Cutoff = Quality.HDTV720p,
                           Delay = 1,
                           Items = new List<ProfileQualityItem>
                                   {
                                       new ProfileQualityItem { Allowed = true, Quality = Quality.HDTV720p },
                                       new ProfileQualityItem { Allowed = true, Quality = Quality.WEBDL720p },
                                       new ProfileQualityItem { Allowed = true, Quality = Quality.Bluray720p }
                                   },
                       };

            _series.Profile = new LazyLoaded<Profile>(_profile);

            _release = Builder<ReleaseInfo>.CreateNew().Build();

            _parsedEpisodeInfo = Builder<ParsedEpisodeInfo>.CreateNew().Build();
            _parsedEpisodeInfo.Quality = new QualityModel(Quality.HDTV720p);

            _remoteEpisode = new RemoteEpisode();
            _remoteEpisode.Episodes = new List<Episode>{ _episode };
            _remoteEpisode.Series = _series;
            _remoteEpisode.ParsedEpisodeInfo = _parsedEpisodeInfo;
            _remoteEpisode.Release = _release;
            
            _approved = new DownloadDecision(Builder<RemoteEpisode>.CreateNew().Build());
            _rejected = new DownloadDecision(Builder<RemoteEpisode>.CreateNew().Build(), new Rejection("Rejected!", RejectionType.Permanent));
            _temporarilyRejected = new DownloadDecision(_remoteEpisode, new Rejection("Temp Rejected", RejectionType.Temporary));

            Mocker.GetMock<IHeldReleaseRepository>()
                  .Setup(s => s.All())
                  .Returns(new List<HeldRelease>());

            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.GetSeries(It.IsAny<Int32>()))
                  .Returns(_series);

            Mocker.GetMock<IParsingService>()
                  .Setup(s => s.GetEpisodes(It.IsAny<ParsedEpisodeInfo>(), _series, true, null))
                  .Returns(new List<Episode> {_episode});
        }

        private void GivenHeldRelease()
        {
            var parsedEpisode = _parsedEpisodeInfo.JsonClone();

            var heldReleases = Builder<HeldRelease>.CreateListOfSize(1)
                                                   .All()
                                                   .With(h => h.SeriesId = _series.Id)
                                                   .With(h => h.ParsedEpisodeInfo = parsedEpisode)
                                                   .Build();

            Mocker.GetMock<IHeldReleaseRepository>()
                  .Setup(s => s.All())
                  .Returns(heldReleases);
        }

        [Test]
        public void should_return_empty_list_when_reports_are_approved()
        {
            var decisions = new List<DownloadDecision> {_approved};

            Subject.Hold(decisions).Should().BeEmpty();
            VerifyNoInsert();
        }

        [Test]
        public void should_return_empty_list_when_reports_are_permanently_rejected()
        {
            var decisions = new List<DownloadDecision> { _rejected };

            Subject.Hold(decisions).Should().BeEmpty();
            VerifyNoInsert();
        }

        [Test]
        public void should_return_empty_list_if_already_held()
        {
            GivenHeldRelease();
            
            var decisions = new List<DownloadDecision> { _temporarilyRejected };

            Subject.Hold(decisions).Should().BeEmpty();
            VerifyNoInsert();
        }

        [Test]
        public void should_only_queue_one_release_when_there_are_multiple_of_the_same()
        {
            var decisions = new List<DownloadDecision> { _temporarilyRejected, _temporarilyRejected };

            Subject.Hold(decisions).Should().HaveCount(1);

            VerifyInsert();
        }

        [Test]
        public void should_remove_old_held_release_if_it_is_a_lower_quality()
        {
            GivenHeldRelease();

            _remoteEpisode.ParsedEpisodeInfo.Quality = new QualityModel(Quality.WEBDL720p);

            var decisions = new List<DownloadDecision> { _temporarilyRejected };

            Subject.Hold(decisions).Should().HaveCount(1);

            Mocker.GetMock<IHeldReleaseRepository>()
                .Verify(v => v.Delete(It.IsAny<HeldRelease>()), Times.Once());

            VerifyInsert();
        }

        private void VerifyInsert()
        {
            Mocker.GetMock<IHeldReleaseRepository>()
                .Verify(v => v.Insert(It.IsAny<HeldRelease>()), Times.Once());
        }

        private void VerifyNoInsert()
        {
            Mocker.GetMock<IHeldReleaseRepository>()
                .Verify(v => v.Insert(It.IsAny<HeldRelease>()), Times.Never());
        }
    }
}
