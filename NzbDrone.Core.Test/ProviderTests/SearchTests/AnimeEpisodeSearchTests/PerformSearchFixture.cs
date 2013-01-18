using System;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Providers.Search;
using NzbDrone.Core.Repository;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.ProviderTests.SearchTests.AnimeEpisodeSearchTests
{
    [TestFixture]
    public class PerformSearchFixture : PerformSearchTestBase
    {
        [Test]
        public void should_throw_if_episode_is_null()
        {
            Episode nullEpisode = null;
            Assert.Throws<ArgumentException>(() => 
                                                Mocker.Resolve<AnimeEpisodeSearch>()
                                                      .PerformSearch(_series, new { Episode = nullEpisode }, notification));
        }

        [Test]
        public void should_fetch_results_from_indexers()
        {
            WithValidIndexers();

            Mocker.Resolve<AnimeEpisodeSearch>()
                  .PerformSearch(_series, new {Episode = _episode}, notification)
                  .Should()
                  .HaveCount(20);
        }

        [Test]
        public void should_log_error_when_fetching_from_indexer_fails()
        {
            WithInvalidIndexers();

            Mocker.Resolve<AnimeEpisodeSearch>()
                  .PerformSearch(_series, new { Episode = _episode }, notification)
                  .Should()
                  .HaveCount(0);

            ExceptionVerification.ExpectedErrors(2);
        }

        [Test]
        public void should_use_scene_numbering_when_available()
        {
            _series.UseSceneNumbering = true;
            _episode.SceneAbsoluteEpisodeNumber = 5;

            WithValidIndexers();

            Mocker.Resolve<AnimeEpisodeSearch>()
                  .PerformSearch(_series, new { Episode = _episode }, notification)
                  .Should()
                  .HaveCount(20);

            _indexer1.Verify(v => v.FetchAnime(_series.Title, 5), Times.Once());
            _indexer2.Verify(v => v.FetchAnime(_series.Title, 5), Times.Once());
        }

        [Test]
        public void should_use_standard_numbering_when_scene_series_set_but_info_is_not_available()
        {
            _series.UseSceneNumbering = true;
            _episode.SceneAbsoluteEpisodeNumber = 0;

            WithValidIndexers();

            Mocker.Resolve<AnimeEpisodeSearch>()
                  .PerformSearch(_series, new { Episode = _episode }, notification)
                  .Should()
                  .HaveCount(20);

            _indexer1.Verify(v => v.FetchAnime(_series.Title, _episode.AbsoluteEpisodeNumber), Times.Once());
            _indexer2.Verify(v => v.FetchAnime(_series.Title, _episode.AbsoluteEpisodeNumber), Times.Once());
        }

        [Test]
        public void should_use_standard_numbering_when_not_scene_series()
        {
            _series.UseSceneNumbering = false;

            WithValidIndexers();

            Mocker.Resolve<AnimeEpisodeSearch>()
                  .PerformSearch(_series, new { Episode = _episode }, notification)
                  .Should()
                  .HaveCount(20);

            _indexer1.Verify(v => v.FetchAnime(_series.Title, _episode.AbsoluteEpisodeNumber), Times.Once());
            _indexer2.Verify(v => v.FetchAnime(_series.Title, _episode.AbsoluteEpisodeNumber), Times.Once());
        }
    }
}
