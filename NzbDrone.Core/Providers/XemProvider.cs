using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Model;
using NzbDrone.Core.Repository;

namespace NzbDrone.Core.Providers
{
    public class XemProvider
    {
        private readonly SeriesProvider _seriesProvider;
        private readonly EpisodeProvider _episodeProvider;
        private readonly XemCommunicationProvider _xemCommunicationProvider;
        private readonly SceneMappingProvider _sceneMappingProvider;

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public XemProvider(SeriesProvider seriesProvider, EpisodeProvider episodeProvider,
                            XemCommunicationProvider xemCommunicationProvider, SceneMappingProvider sceneMappingProvider)
        {
            _seriesProvider = seriesProvider;
            _episodeProvider = episodeProvider;
            _xemCommunicationProvider = xemCommunicationProvider;
            _sceneMappingProvider = sceneMappingProvider;
        }

        public XemProvider()
        {
        }

        public virtual void UpdateMappings()
        {
            logger.Trace("Starting scene numbering update");
            try
            {
                var ids = _xemCommunicationProvider.GetXemSeriesIds();
                var series = _seriesProvider.GetAllSeries();
                var wantedSeries = series.Where(s => ids.Contains(s.SeriesId)).ToList();

                foreach(var ser in wantedSeries)
                {
                    PerformUpdate(ser);
                }

                logger.Trace("Completed scene numbering update");
            }

            catch(Exception ex)
            {
                logger.WarnException("Error updating Scene Mappings", ex);
                throw;
            }
        }

        public virtual void UpdateMappings(int seriesId)
        {
            var xemIds = _xemCommunicationProvider.GetXemSeriesIds();

            if (!xemIds.Contains(seriesId))
            {
                logger.Trace("Xem doesn't have a mapping for this series: {0}", seriesId);
                return;
            }

            var series = _seriesProvider.GetSeries(seriesId);

            if (series == null)
            {
                logger.Trace("Series could not be found: {0}", seriesId);
                return;
            }

            PerformUpdate(series);
        }

        public virtual void PerformUpdate(Series series)
        {
            logger.Trace("Updating scene numbering mapping for: {0}", series.Title);
            try
            {
                var episodesToUpdate = new List<Episode>();
                var mappings = _xemCommunicationProvider.GetSceneTvdbMappings(series.SeriesId);

                if (mappings == null)
                {
                    logger.Trace("Mappings for: {0} are null, skipping", series.Title);
                    return;
                }

                var episodes = _episodeProvider.GetEpisodeBySeries(series.SeriesId);

                foreach (var mapping in mappings)
                {
                    logger.Trace("Setting scene numbering mappings for {0} S{1:00}E{2:00}", series.Title, mapping.Tvdb.Season, mapping.Tvdb.Episode);

                    var episode = episodes.SingleOrDefault(e => e.SeasonNumber == mapping.Tvdb.Season && e.EpisodeNumber == mapping.Tvdb.Episode);

                    if (episode == null)
                    {
                        logger.Trace("Information hasn't been added to TheTVDB yet, skipping.");
                        continue;
                    }

                    episode.AbsoluteEpisodeNumber = mapping.Scene.Absolute;
                    episode.SceneSeasonNumber = mapping.Scene.Season;
                    episode.SceneEpisodeNumber = mapping.Scene.Episode;
                    episodesToUpdate.Add(episode);
                }

                logger.Trace("Committing scene numbering mappings to database for: {0}", series.Title);
                _episodeProvider.UpdateEpisodes(episodesToUpdate);

                logger.Trace("Setting UseSceneMapping for {0}", series.Title);
                series.UseSceneNumbering = true;
                _seriesProvider.UpdateSeries(series);
            }

            catch (Exception ex)
            {
                logger.WarnException("Error updating scene numbering mappings for: " + series, ex);
            }
        }

        public virtual void UpdateAlternateNames()
        {
            logger.Trace("Updating alternate names for all series");

            var ids = _xemCommunicationProvider.GetXemSeriesIds();
            var series = _seriesProvider.GetAllSeries();
            var wantedSeries = series.Where(s => ids.Contains(s.SeriesId)).ToList();

            foreach (var ser in wantedSeries)
            {
                UpdateAlternateNames(ser.SeriesId);
            }

            logger.Trace("Completed alternate name update.");
        }

        public virtual void UpdateAlternateNames(int seriesId)
        {
            logger.Trace("Updating alternate names for: {0}", seriesId);

            try
            {
                var alternateNames = _xemCommunicationProvider.GetAlternateNames(seriesId);

                var mappings = new List<SceneMapping>();

                foreach (var alternateName in alternateNames)
                {
                    mappings.Add(new SceneMapping
                    {
                        SeriesId = seriesId,
                        SeasonNumber = alternateName.SeasonNumber,
                        SceneName = alternateName.Name,
                        CleanTitle = Parser.NormalizeTitle(alternateName.Name),
                        Source = SceneMappingSourceType.Xem
                    });
                }

                _sceneMappingProvider.DeleteMappings(seriesId, SceneMappingSourceType.Xem);
                _sceneMappingProvider.InsertMappings(mappings);

                logger.Trace("Finished updating alternate names for: {0}", seriesId);
            }
            catch (XemException ex)
            {
                logger.Error("Error received from Xem when updating alternate names for: {0}", seriesId);
            }
            catch(Exception ex)
            {
                logger.ErrorException("Error updating alternate names for: " + seriesId, ex);
            }
        }
    }
}
