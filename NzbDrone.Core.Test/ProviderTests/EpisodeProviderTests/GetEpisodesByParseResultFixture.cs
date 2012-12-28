// ReSharper disable RedundantUsingDirective

using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Model;
using NzbDrone.Core.Providers;
using NzbDrone.Core.Repository;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.ProviderTests.EpisodeProviderTests
{
    [TestFixture]
    // ReSharper disable InconsistentNaming
    public class GetEpisodesByParseResultFixture : CoreTest
    {
        private EpisodeProvider episodeProvider;

        private Series fakeSeries;
        private Series fakeDailySeries;

        private Episode fakeEpisode;
        private Episode fakeDailyEpisode;
        private Episode fakeEpisode2;

        private Episode fakeAnimeEpisode;
        private Episode fakeAnimeEpisode2;

        [SetUp]
        public void Setup()
        {
            fakeSeries = Builder<Series>.CreateNew().Build();

            fakeDailySeries = Builder<Series>.CreateNew()
                .With(c => c.SeriesType = SeriesType.Daily)
                .Build();

            fakeEpisode = Builder<Episode>.CreateNew()
                     .With(e => e.SeriesId = fakeSeries.SeriesId)
                     .With(e => e.Title = "Episode (1)")
                     .Build();   

            fakeEpisode2 = Builder<Episode>.CreateNew()
                     .With(e => e.SeriesId = fakeSeries.SeriesId)
                     .With(e => e.SeasonNumber = fakeEpisode.SeasonNumber)
                     .With(e => e.EpisodeNumber = fakeEpisode.EpisodeNumber + 1)
                     .With(e => e.Title = "Episode (2)")
                     .Build();

            fakeDailyEpisode = Builder<Episode>.CreateNew()
                     .With(e => e.SeriesId = fakeSeries.SeriesId)
                     .With(e => e.AirDate = DateTime.Now.Date)
                     .With(e => e.Title = "Daily Episode 1")
                     .Build();

            fakeAnimeEpisode = Builder<Episode>.CreateNew()
                     .With(e => e.SeriesId = fakeSeries.SeriesId)
                     .With(e => e.Title = "Episode (1)")
                     .Build();

            fakeAnimeEpisode2 = Builder<Episode>.CreateNew()
                     .With(e => e.SeriesId = fakeSeries.SeriesId)
                     .With(e => e.SeasonNumber = fakeEpisode.SeasonNumber)
                     .With(e => e.EpisodeNumber = fakeEpisode.EpisodeNumber + 1)
                     .With(e => e.AbsoluteEpisodeNumber = fakeAnimeEpisode.AbsoluteEpisodeNumber + 1)
                     .With(e => e.Title = "Episode (2)")
                     .Build();

            WithRealDb();

            episodeProvider = Mocker.Resolve<EpisodeProvider>();
        }

        [Test]
        public void existing_single_episode_should_return_single_existing_episode()
        {
            Db.Insert(fakeEpisode);
            Db.Insert(fakeSeries);

            var parseResult = new EpisodeParseResult
                                  {
                                      Series = fakeSeries,
                                      SeasonNumber = fakeEpisode.SeasonNumber,
                                      EpisodeNumbers = new List<int> { fakeEpisode.EpisodeNumber }
                                  };

            var ep = episodeProvider.GetEpisodesByParseResult(parseResult);

            ep.Should().HaveCount(1);
            parseResult.EpisodeTitle.Should().Be(fakeEpisode.Title);
            VerifyEpisode(ep[0], fakeEpisode);
            Db.Fetch<Episode>().Should().HaveCount(1);
        }

        [Test]
        public void single_none_existing_episode_should_return_nothing_and_add_nothing()
        {
            var parseResult = new EpisodeParseResult
                                  {
                                      Series = fakeSeries,
                                      SeasonNumber = fakeEpisode.SeasonNumber,
                                      EpisodeNumbers = new List<int> { 10 }
                                  };

            var episode = episodeProvider.GetEpisodesByParseResult(parseResult);

            episode.Should().BeEmpty();
            Db.Fetch<Episode>().Should().HaveCount(0);
        }

        [Test]
        public void single_none_existing_series_should_return_nothing_and_add_nothing()
        {
            var parseResult = new EpisodeParseResult
            {
                Series = fakeSeries,
                SeasonNumber = 10,
                EpisodeNumbers = new List<int> { 10 }
            };

            var episode = episodeProvider.GetEpisodesByParseResult(parseResult);

            episode.Should().BeEmpty();
            Db.Fetch<Episode>().Should().HaveCount(0);
        }

        [Test]
        public void existing_multi_episode_should_return_all_episodes()
        {
            Db.Insert(fakeSeries);
            Db.Insert(fakeEpisode);
            Db.Insert(fakeEpisode2);


            var parseResult = new EpisodeParseResult
                                  {
                                      Series = fakeSeries,
                                      SeasonNumber = fakeEpisode.SeasonNumber,
                                      EpisodeNumbers = new List<int> { fakeEpisode.EpisodeNumber, fakeEpisode2.EpisodeNumber }
                                  };

            var ep = episodeProvider.GetEpisodesByParseResult(parseResult);

            ep.Should().HaveCount(2);
            Db.Fetch<Episode>().Should().HaveCount(2);

            VerifyEpisode(ep[0], fakeEpisode);
            VerifyEpisode(ep[1], fakeEpisode2);
            parseResult.EpisodeTitle.Should().Be("Episode");
        }

        [Test]
        public void none_existing_multi_episode_should_not_return_or_add_anything()
        {
            var parseResult = new EpisodeParseResult
            {
                Series = fakeSeries,
                SeasonNumber = fakeEpisode.SeasonNumber,
                EpisodeNumbers = new List<int> { fakeEpisode.EpisodeNumber, fakeEpisode2.EpisodeNumber }
            };

            var ep = episodeProvider.GetEpisodesByParseResult(parseResult);

            ep.Should().BeEmpty();
            Db.Fetch<Episode>().Should().BeEmpty();
        }

        [Test]
        public void should_return_empty_list_if_episode_list_is_null()
        {
            //Act
            var episodes = episodeProvider.GetEpisodesByParseResult(new EpisodeParseResult());
            //Assert
            episodes.Should().NotBeNull();
            episodes.Should().BeEmpty();
        }

        [Test]
        public void should_return_empty_list_if_episode_list_is_empty()
        {
            //Act
            var episodes = episodeProvider.GetEpisodesByParseResult(new EpisodeParseResult { EpisodeNumbers = new List<int>() });
            //Assert
            episodes.Should().NotBeNull();
            episodes.Should().BeEmpty();
        }

        [Test]
        public void should_return_single_episode_when_air_date_is_provided()
        {

            Db.Insert(fakeSeries);
            Db.Insert(fakeDailyEpisode);

            //Act
            var episodes = episodeProvider.GetEpisodesByParseResult(new EpisodeParseResult { AirDate = DateTime.Today, Series = fakeDailySeries });

            //Assert
            episodes.Should().HaveCount(1);
            VerifyEpisode(episodes[0], fakeDailyEpisode);

            Db.Fetch<Episode>().Should().HaveCount(1);
        }

        [Test]
        public void should_not_add_episode_when_episode_doesnt_exist()
        {
            var episodes = episodeProvider.GetEpisodesByParseResult(new EpisodeParseResult { AirDate = DateTime.Today, Series = fakeDailySeries });

            //Assert
            episodes.Should().HaveCount(0);
            Db.Fetch<Episode>().Should().HaveCount(0);
        }

        [Test]
        public void should_return_single_title_for_multiple_episodes()
        {
            Db.Insert(fakeSeries);
            Db.Insert(fakeEpisode);
            Db.Insert(fakeEpisode2);
  
            var parseResult = new EpisodeParseResult
            {
                Series = fakeSeries,
                SeasonNumber = fakeEpisode.SeasonNumber,
                EpisodeNumbers = new List<int> { fakeEpisode.EpisodeNumber, fakeEpisode2.EpisodeNumber }
            };

            var ep = episodeProvider.GetEpisodesByParseResult(parseResult);

            ep.Should().HaveCount(2);
            Db.Fetch<Episode>().Should().HaveCount(2);

            VerifyEpisode(ep[0], fakeEpisode);
            VerifyEpisode(ep[1], fakeEpisode2);

            parseResult.EpisodeTitle.Should().Be("Episode");
        }

        [Test]
        public void should_return_single_title_for_single_episode()
        {
            Db.Insert(fakeEpisode);
            Db.Insert(fakeSeries);

            var parseResult = new EpisodeParseResult
            {
                Series = fakeSeries,
                SeasonNumber = fakeEpisode.SeasonNumber,
                EpisodeNumbers = new List<int> { fakeEpisode.EpisodeNumber }
            };

            var ep = episodeProvider.GetEpisodesByParseResult(parseResult);

            ep.Should().HaveCount(1);
            Db.Fetch<Episode>().Should().HaveCount(1);
            ep.First().ShouldHave().AllPropertiesBut(e => e.Series);
            parseResult.EpisodeTitle.Should().Be(fakeEpisode.Title);
        }

        [Test]
        public void should_return_nothing_when_series_is_not_daily_but_parsed_daily()
        {
            Db.Insert(fakeSeries);

            var parseResult = new EpisodeParseResult
            {
                Series = fakeSeries,
                AirDate = DateTime.Today
            };

            var ep = episodeProvider.GetEpisodesByParseResult(parseResult);

            ep.Should().BeEmpty();
            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_return_nothing_when_series_is_not_anime_but_parsed_anime()
        {
            Db.Insert(fakeSeries);

            var parseResult = new EpisodeParseResult
            {
                Series = fakeSeries,
                AbsoluteEpisodeNumbers =  new List<int>{ 215, 216 }
            };

            var ep = episodeProvider.GetEpisodesByParseResult(parseResult);

            ep.Should().BeEmpty();
            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_find_anime_episode_by_absolute_episode_number()
        {
            fakeSeries.SeriesType = SeriesType.Anime;
            Db.Insert(fakeSeries);
            Db.Insert(fakeAnimeEpisode);

            var parseResult = new EpisodeParseResult
            {
                Series = fakeSeries,
                AbsoluteEpisodeNumbers = new List<int> { fakeAnimeEpisode.AbsoluteEpisodeNumber }
            };

            var ep = episodeProvider.GetEpisodesByParseResult(parseResult);

            ep.Should().HaveCount(1);

            VerifyEpisode(ep[0], fakeAnimeEpisode);
        }

        [Test]
        public void should_return_multi_anime_episodes_when_multiple_were_parsed()
        {
            fakeSeries.SeriesType = SeriesType.Anime;
            Db.Insert(fakeSeries);
            Db.Insert(fakeAnimeEpisode);
            Db.Insert(fakeAnimeEpisode2);

            var parseResult = new EpisodeParseResult
            {
                Series = fakeSeries,
                AbsoluteEpisodeNumbers = new List<int> { fakeAnimeEpisode.AbsoluteEpisodeNumber, fakeAnimeEpisode2.AbsoluteEpisodeNumber }
            };

            var ep = episodeProvider.GetEpisodesByParseResult(parseResult);

            ep.Should().HaveCount(2);

            VerifyEpisode(ep[0], fakeAnimeEpisode);
            VerifyEpisode(ep[1], fakeAnimeEpisode2);
        }

        [Test]
        public void should_return_nothing_when_absoluteEpisode_doesnt_exist()
        {
            fakeSeries.SeriesType = SeriesType.Anime;
            Db.Insert(fakeSeries);

            var parseResult = new EpisodeParseResult
            {
                Series = fakeSeries,
                AbsoluteEpisodeNumbers = new List<int> { fakeAnimeEpisode.AbsoluteEpisodeNumber }
            };

            var ep = episodeProvider.GetEpisodesByParseResult(parseResult);

            ep.Should().BeEmpty();
        }

        private void VerifyEpisode(Episode actual, Episode excpected)
        {
            actual.ShouldHave().AllProperties().But(e => e.Series).But(e => e.EpisodeFile).EqualTo(excpected);
        }
    }
}
