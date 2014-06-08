using System;
using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine.Specifications.RssSync;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Test.DecisionEngineTests.RssSync
{
    [TestFixture]
    public class DelaySpecificationFixture : CoreTest<DelaySpecification>
    {
        private Profile _profile;
        private RemoteEpisode _remoteEpisode;

        [SetUp]
        public void Setup()
        {
            _profile = Builder<Profile>.CreateNew()
                                       .Build();

            var series = Builder<Series>.CreateNew()
                                        .With(s => s.Profile = _profile)
                                        .Build();

            _remoteEpisode = Builder<RemoteEpisode>.CreateNew()
                                                   .With(r => r.Series = series)
                                                   .Build();

            _profile.Items = new List<ProfileQualityItem>();
            _remoteEpisode.ParsedEpisodeInfo = new ParsedEpisodeInfo();
            _remoteEpisode.Release = new ReleaseInfo();
        }

        [Test]
        public void should_be_true_if_search()
        {
            Subject.IsSatisfiedBy(new RemoteEpisode(), new SingleEpisodeSearchCriteria()).Should().BeTrue();
        }

        [Test]
        public void should_be_true_if_profile_does_not_have_a_delay()
        {
            _profile.Delay = 0;

            Subject.IsSatisfiedBy(_remoteEpisode, null).Should().BeTrue();
        }

        [Test]
        public void should_be_true_if_quality_is_last_allowed_in_profile()
        {
            _profile.Items.Add(new ProfileQualityItem { Allowed = true, Quality = Quality.HDTV720p });
            _remoteEpisode.ParsedEpisodeInfo.Quality = new QualityModel(Quality.HDTV720p);

            Subject.IsSatisfiedBy(_remoteEpisode, null).Should().BeTrue();
        }

        [Test]
        public void should_be_true_if_release_is_older_than_delay()
        {
            _profile.Items.Add(new ProfileQualityItem { Allowed = true, Quality = Quality.HDTV720p });
            _remoteEpisode.ParsedEpisodeInfo.Quality = new QualityModel(Quality.SDTV);
            _remoteEpisode.Release.PublishDate = DateTime.UtcNow.AddHours(-10);
            
            _profile.Delay = 1;

            Subject.IsSatisfiedBy(_remoteEpisode, null).Should().BeTrue();
        }

        [Test]
        public void should_be_false_if_release_is_younger_than_delay()
        {
            _profile.Items.Add(new ProfileQualityItem { Allowed = true, Quality = Quality.HDTV720p });
            _remoteEpisode.ParsedEpisodeInfo.Quality = new QualityModel(Quality.SDTV);
            _remoteEpisode.Release.PublishDate = DateTime.UtcNow.AddHours(10);

            _profile.Delay = 12;

            Subject.IsSatisfiedBy(_remoteEpisode, null).Should().BeFalse();
        }
    }
}
