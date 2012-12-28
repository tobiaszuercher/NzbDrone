// ReSharper disable RedundantUsingDirective

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Model;
using NzbDrone.Core.Providers;
using NzbDrone.Core.Providers.Core;
using NzbDrone.Core.Repository;
using NzbDrone.Core.Repository.Quality;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common.AutoMoq;

namespace NzbDrone.Core.Test.ProviderTests.MediaFileProviderTests
{
    [TestFixture]
    // ReSharper disable InconsistentNaming
    public class Anime_GetNewFilenameFixture : CoreTest
    {
        private Series _series;
        private Mock<ConfigProvider> _fakeConfig;
        private Episode _episodeOne;
        private Episode _episodeTwo;

        [SetUp]
        public void Setup()
        {
            _series = Builder<Series>
                    .CreateNew()
                    .With(s => s.Title = "Initial D")
                    .With(s => s.SeriesType = SeriesType.Anime)
                    .Build();

            _episodeOne = Builder<Episode>
                    .CreateNew()
                    .With(e => e.Title = "The Race")
                    .With(e => e.SeasonNumber = 1)
                    .With(e => e.EpisodeNumber = 5)
                    .With(e => e.AbsoluteEpisodeNumber = 5)
                    .Build();

            _episodeTwo = Builder<Episode>
                    .CreateNew()
                    .With(e => e.Title = "The End")
                    .With(e => e.SeasonNumber = 1)
                    .With(e => e.EpisodeNumber = 6)
                    .With(e => e.AbsoluteEpisodeNumber = 6)
                    .Build();

            _fakeConfig = Mocker.GetMock<ConfigProvider>();
            _fakeConfig.SetupGet(c => c.SortingIncludeSeriesName).Returns(true);
            _fakeConfig.SetupGet(c => c.SortingIncludeEpisodeTitle).Returns(true);
            _fakeConfig.SetupGet(c => c.SortingAppendQuality).Returns(true);
            _fakeConfig.SetupGet(c => c.SortingSeparatorStyle).Returns(0);
            _fakeConfig.SetupGet(c => c.SortingNumberStyle).Returns(2);
            _fakeConfig.SetupGet(c => c.SortingReplaceSpaces).Returns(false);
        }

        private List<Episode> WithOneEpisode()
        {
            return new List<Episode> { _episodeOne };
        }

        private List<Episode> WithTwoEpisodes()
        {
            return new List<Episode> { _episodeOne, _episodeTwo };
        }
            
        [Test]
        public void should_pad_to_three_digits_when_configured()
        {
            _fakeConfig.SetupGet(c => c.SortingAnimeNumberStyle).Returns(0);
            _fakeConfig.SetupGet(c => c.SortingAnimeNumberPadding).Returns(3);

            var result = Mocker.Resolve<MediaFileProvider>()
                               .GetNewFilename(WithOneEpisode(), _series, QualityTypes.HDTV, false, new EpisodeFile());
            result.Should().Be("Initial D - S01E05 - 005 - The Race [HDTV]");
        }

        [Test]
        public void should_pad_to_two_digits_when_configured()
        {
            _fakeConfig.SetupGet(c => c.SortingAnimeNumberStyle).Returns(0);
            _fakeConfig.SetupGet(c => c.SortingAnimeNumberPadding).Returns(2);

            var result = Mocker.Resolve<MediaFileProvider>()
                               .GetNewFilename(WithOneEpisode(), _series, QualityTypes.HDTV, false, new EpisodeFile());
            result.Should().Be("Initial D - S01E05 - 05 - The Race [HDTV]");
        }

        [Test]
        public void should_use_absolute_number_only()
        {
            _fakeConfig.SetupGet(c => c.SortingAnimeNumberStyle).Returns(2);
            _fakeConfig.SetupGet(c => c.SortingAnimeNumberPadding).Returns(2);

            var result = Mocker.Resolve<MediaFileProvider>()
                               .GetNewFilename(WithOneEpisode(), _series, QualityTypes.HDTV, false, new EpisodeFile());
            result.Should().Be("Initial D - 05 - The Race [HDTV]");
        }

        [Test]
        public void should_put_absolute_before_standard()
        {
            _fakeConfig.SetupGet(c => c.SortingAnimeNumberStyle).Returns(1);
            _fakeConfig.SetupGet(c => c.SortingAnimeNumberPadding).Returns(2);

            var result = Mocker.Resolve<MediaFileProvider>()
                               .GetNewFilename(WithOneEpisode(), _series, QualityTypes.HDTV, false, new EpisodeFile());
            result.Should().Be("Initial D - 05 - S01E05 - The Race [HDTV]");
        }

        [Test]
        public void should_append_subGroup_when_available()
        {
            _fakeConfig.SetupGet(c => c.SortingAnimeNumberStyle).Returns(0);
            _fakeConfig.SetupGet(c => c.SortingAnimeNumberPadding).Returns(2);
            _fakeConfig.SetupGet(c => c.SortingAnimeAppendSubGroup).Returns(true);

            var result = Mocker.Resolve<MediaFileProvider>()
                               .GetNewFilename(WithOneEpisode(), _series, QualityTypes.HDTV, false, new EpisodeFile{ SubGroup = "Lunar" });
            result.Should().Be("Initial D - S01E05 - 05 - The Race [HDTV] [Lunar]");
        }

        [Test]
        public void should_not_append_subGroup_when_it_is_null()
        {
            _fakeConfig.SetupGet(c => c.SortingAnimeNumberStyle).Returns(0);
            _fakeConfig.SetupGet(c => c.SortingAnimeNumberPadding).Returns(2);
            _fakeConfig.SetupGet(c => c.SortingAnimeAppendSubGroup).Returns(true);

            var result = Mocker.Resolve<MediaFileProvider>()
                               .GetNewFilename(WithOneEpisode(), _series, QualityTypes.HDTV, false, new EpisodeFile { SubGroup = null});
            result.Should().Be("Initial D - S01E05 - 05 - The Race [HDTV]");
        }

        [Test]
        public void should_not_append_subGroup_when_it_is_empty()
        {
            _fakeConfig.SetupGet(c => c.SortingAnimeNumberStyle).Returns(0);
            _fakeConfig.SetupGet(c => c.SortingAnimeNumberPadding).Returns(2);
            _fakeConfig.SetupGet(c => c.SortingAnimeAppendSubGroup).Returns(true);

            var result = Mocker.Resolve<MediaFileProvider>()
                               .GetNewFilename(WithOneEpisode(), _series, QualityTypes.HDTV, false, new EpisodeFile { SubGroup = "" });
            result.Should().Be("Initial D - S01E05 - 05 - The Race [HDTV]");
        }

        [Test]
        public void should_put_standard_before_absolute_for_multi_episode()
        {
            _fakeConfig.SetupGet(c => c.SortingAnimeNumberStyle).Returns(0);
            _fakeConfig.SetupGet(c => c.SortingAnimeNumberPadding).Returns(2);
            _fakeConfig.SetupGet(c => c.SortingAnimeMultiEpisodeStyle).Returns(3);

            var result = Mocker.Resolve<MediaFileProvider>()
                               .GetNewFilename(WithTwoEpisodes(), _series, QualityTypes.HDTV, false, new EpisodeFile());
            result.Should().Be("Initial D - S01E05-06 - 05 - 06 - The Race + The End [HDTV]");
        }

        [Test]
        public void should_use_absolute_number_only_for_multi_episode()
        {
            _fakeConfig.SetupGet(c => c.SortingAnimeNumberStyle).Returns(2);
            _fakeConfig.SetupGet(c => c.SortingAnimeNumberPadding).Returns(2);
            _fakeConfig.SetupGet(c => c.SortingAnimeMultiEpisodeStyle).Returns(3);

            var result = Mocker.Resolve<MediaFileProvider>()
                               .GetNewFilename(WithTwoEpisodes(), _series, QualityTypes.HDTV, false, new EpisodeFile());
            result.Should().Be("Initial D - 05 - 06 - The Race + The End [HDTV]");
        }

        [Test]
        public void should_put_absolute_before_standard_for_multi_episode()
        {
            _fakeConfig.SetupGet(c => c.SortingAnimeNumberStyle).Returns(1);
            _fakeConfig.SetupGet(c => c.SortingAnimeNumberPadding).Returns(2);
            _fakeConfig.SetupGet(c => c.SortingAnimeMultiEpisodeStyle).Returns(3);

            var result = Mocker.Resolve<MediaFileProvider>()
                               .GetNewFilename(WithTwoEpisodes(), _series, QualityTypes.HDTV, false, new EpisodeFile());
            result.Should().Be("Initial D - 05 - 06 - S01E05-06 - The Race + The End [HDTV]");
        }

        [Test]
        public void should_use_dash_for_multi_episode_separation()
        {
            _fakeConfig.SetupGet(c => c.SortingAnimeNumberStyle).Returns(1);
            _fakeConfig.SetupGet(c => c.SortingAnimeNumberPadding).Returns(2);
            _fakeConfig.SetupGet(c => c.SortingAnimeMultiEpisodeStyle).Returns(0);

            var result = Mocker.Resolve<MediaFileProvider>()
                               .GetNewFilename(WithTwoEpisodes(), _series, QualityTypes.HDTV, false, new EpisodeFile());
            result.Should().Be("Initial D - 05-06 - S01E05-06 - The Race + The End [HDTV]");
        }

        [Test]
        public void should_use_space_for_multi_episode_separation()
        {
            _fakeConfig.SetupGet(c => c.SortingAnimeNumberStyle).Returns(1);
            _fakeConfig.SetupGet(c => c.SortingAnimeNumberPadding).Returns(2);
            _fakeConfig.SetupGet(c => c.SortingAnimeMultiEpisodeStyle).Returns(1);

            var result = Mocker.Resolve<MediaFileProvider>()
                               .GetNewFilename(WithTwoEpisodes(), _series, QualityTypes.HDTV, false, new EpisodeFile());
            result.Should().Be("Initial D - 05 06 - S01E05-06 - The Race + The End [HDTV]");
        }

        [Test]
        public void should_use_period_for_multi_episode_separation()
        {
            _fakeConfig.SetupGet(c => c.SortingAnimeNumberStyle).Returns(1);
            _fakeConfig.SetupGet(c => c.SortingAnimeNumberPadding).Returns(2);
            _fakeConfig.SetupGet(c => c.SortingAnimeMultiEpisodeStyle).Returns(2);

            var result = Mocker.Resolve<MediaFileProvider>()
                               .GetNewFilename(WithTwoEpisodes(), _series, QualityTypes.HDTV, false, new EpisodeFile());
            result.Should().Be("Initial D - 05.06 - S01E05-06 - The Race + The End [HDTV]");
        }

        [Test]
        public void should_use_dash_with_spaces_for_multi_episode_separation()
        {
            _fakeConfig.SetupGet(c => c.SortingAnimeNumberStyle).Returns(1);
            _fakeConfig.SetupGet(c => c.SortingAnimeNumberPadding).Returns(2);
            _fakeConfig.SetupGet(c => c.SortingAnimeMultiEpisodeStyle).Returns(3);

            var result = Mocker.Resolve<MediaFileProvider>()
                               .GetNewFilename(WithTwoEpisodes(), _series, QualityTypes.HDTV, false, new EpisodeFile());
            result.Should().Be("Initial D - 05 - 06 - S01E05-06 - The Race + The End [HDTV]");
        }
    }
}