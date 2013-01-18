using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Model;
using NzbDrone.Core.Providers;
using NzbDrone.Core.Providers.Search;
using NzbDrone.Core.Repository;
using NzbDrone.Core.Repository.Quality;
using NzbDrone.Core.Repository.Search;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.ProviderTests.SearchTests.AnimeEpisodeSearchTests
{
    [TestFixture]
    public class CheckReportFixture : TestBase
    {
        private Series _series;
        private Episode _episode;
        private EpisodeParseResult _episodeParseResult;
        private SearchHistoryItem _searchHistoryItem;
            
        [SetUp]
        public void Setup()
        {
            _series = Builder<Series>
                    .CreateNew()
                    .Build();

            _episode = Builder<Episode>
                    .CreateNew()
                    .With(e => e.SeriesId = _series.SeriesId)
                    .With(e => e.Series = _series)
                    .Build();

            _episodeParseResult = Builder<EpisodeParseResult>
                    .CreateNew()
                    .With(p => p.SeasonNumber = 1)
                    .With(p => p.EpisodeNumbers = new List<int>{ _episode.EpisodeNumber })
                    .With(p => p.Episodes = new List<Episode> { _episode })
                    .With(p => p.Series = _series)
                    .Build();

            _searchHistoryItem = new SearchHistoryItem();
        }

        [Test]
        public void should_skip_if_absoluteEpisodeNumber_not_found_in_report()
        {
            _series.SeriesType = SeriesType.Anime;
            _episodeParseResult.AbsoluteEpisodeNumbers = new List<int>();

            //Act
            var result = Mocker.Resolve<AnimeEpisodeSearch>().CheckReport(_series, new { Episode = _episode }, _episodeParseResult, _searchHistoryItem);

            //Assert
            result.SearchError.Should().Be(ReportRejectionType.WrongEpisode);
        }

        [Test]
        public void should_skip_if_sceneAbsoluteEpisodeNumber_doesnt_match()
        {
            _series.SeriesType = SeriesType.Anime;
            _series.UseSceneNumbering = true;
            _episodeParseResult.AbsoluteEpisodeNumbers = new List<int> { 100 };
            _episode.SceneAbsoluteEpisodeNumber = 5;

            //Act
            var result = Mocker.Resolve<AnimeEpisodeSearch>().CheckReport(_series, new { Episode = _episode }, _episodeParseResult, _searchHistoryItem);

            //Assert
            result.SearchError.Should().Be(ReportRejectionType.WrongEpisode);
        }

        [Test]
        public void should_skip_if_absoluteEpisodeNumber_doesnt_match()
        {
            _series.SeriesType = SeriesType.Anime;
            _episodeParseResult.AbsoluteEpisodeNumbers = new List<int> { 100 };

            //Act
            var result = Mocker.Resolve<AnimeEpisodeSearch>().CheckReport(_series, new { Episode = _episode }, _episodeParseResult, _searchHistoryItem);

            //Assert
            result.SearchError.Should().Be(ReportRejectionType.WrongEpisode);
        }
    }
}
