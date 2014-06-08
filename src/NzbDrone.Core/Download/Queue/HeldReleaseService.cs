using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Download.Queue
{
    public interface IQueuedReleaseService
    {
        List<DownloadDecision> Queue(List<DownloadDecision> decisions);
    }

    public class HeldReleaseService : IHandle<RssSyncCompleteEvent>, IQueuedReleaseService
    {
        private readonly IQueuedReleaseRepository _repository;
        private readonly ISeriesService _seriesService;
        private readonly IParsingService _parsingService;
        private readonly IMakeDownloadDecision _downloadDecisionMaker;
        private readonly IDownloadService _downloadService;
        private readonly Logger _logger;

        public HeldReleaseService(IQueuedReleaseRepository repository,
                                    ISeriesService seriesService,
                                    IParsingService parsingService,
                                    IMakeDownloadDecision downloadDecisionMaker,
                                    IDownloadService downloadService,
                                    Logger logger)
        {
            _repository = repository;
            _seriesService = seriesService;
            _parsingService = parsingService;
            _downloadDecisionMaker = downloadDecisionMaker;
            _downloadService = downloadService;
            _logger = logger;
        }

        public List<DownloadDecision> Queue(List<DownloadDecision> decisions)
        {
            var qualified = GetQualifiedReports(decisions);
            var queued = new List<DownloadDecision>();

            if (!qualified.Any())
            {
                _logger.Debug("No held reports to process");
            }

            var existingReports = GetReleases(_repository.All());

            foreach (var downloadDecision in qualified)
            {
                var decision = downloadDecision;
                var profile = decision.RemoteEpisode.Series.Profile.Value;
                var episodeIds = decision.RemoteEpisode.Episodes.Select(e => e.Id);

                if (queued.Any(q => q.RemoteEpisode.Episodes.Select(e => e.Id).Intersect(episodeIds).Any()))
                {
                    _logger.Debug("This episode was already queued");
                    continue;
                }

                var existingReport = existingReports.FirstOrDefault(r => r.SeriesId == decision.RemoteEpisode.Series.Id &&
                                                                r.RemoteEpisode.Episodes.Select(e => e.Id).Intersect(episodeIds)
                                                                 .Any());

                if (existingReport == null)
                {
                    _logger.Debug("Holding item");
                    Insert(decision, profile);
                    queued.Add(decision);

                    continue;
                }

                var compare = new QualityModelComparer(profile).Compare(existingReport.ParsedEpisodeInfo.Quality,
                                                                        decision.RemoteEpisode.ParsedEpisodeInfo.Quality);

                if (compare >= 0)
                {
                    _logger.Debug("Existing held item meets or exceeds quality");
                    continue;
                }

                _logger.Debug("Removing previously held release");
                _repository.Delete(existingReport);

                _logger.Debug("Holding item");
                Insert(decision, profile);
                queued.Add(decision);
            }

            return queued;
        }

        private List<DownloadDecision> GetQualifiedReports(IEnumerable<DownloadDecision> decisions)
        {
            return decisions.Where(c => c.TemporarilyRejected && c.RemoteEpisode.Episodes.Any())
                .GroupBy(c => c.RemoteEpisode.Series.Id, (i, s) => s
                    .OrderByDescending(c => c.RemoteEpisode.ParsedEpisodeInfo.Quality, new QualityModelComparer(s.First().RemoteEpisode.Series.Profile))
                    .ThenBy(c => c.RemoteEpisode.Episodes.Select(e => e.EpisodeNumber).MinOrDefault())
                    .ThenBy(c => c.RemoteEpisode.Release.Size.Round(200.Megabytes()) / c.RemoteEpisode.Episodes.Count)
                    .ThenBy(c => c.RemoteEpisode.Release.Age))
                .SelectMany(c => c)
                .ToList();
        }

        private List<HeldRelease> GetReleases(IEnumerable<HeldRelease> releases)
        {
            var result = new List<HeldRelease>();

            foreach (var release in releases)
            {
                var series = _seriesService.GetSeries(release.SeriesId);
                var episodes = _parsingService.GetEpisodes(release.ParsedEpisodeInfo, series, true);

                release.RemoteEpisode = new RemoteEpisode
                                                        {
                                                            Series = series,
                                                            Episodes = episodes,
                                                            ParsedEpisodeInfo = release.ParsedEpisodeInfo,
                                                            Release = release.Release
                                                        };

                result.Add(release);
            }

            return result;
        }

        private void Insert(DownloadDecision decision, Profile profile)
        {
            var expiry = decision.RemoteEpisode.Release.PublishDate.AddHours(profile.Delay);

            _repository.Insert(new HeldRelease
            {
                SeriesId = decision.RemoteEpisode.Series.Id,
                ParsedEpisodeInfo = decision.RemoteEpisode.ParsedEpisodeInfo,
                Release = decision.RemoteEpisode.Release,
                Title = decision.RemoteEpisode.Release.Title,
                Added = DateTime.UtcNow,
                Expiry = expiry
            });
        }

        public void Handle(RssSyncCompleteEvent message)
        {
            var expired = GetReleases(_repository.Expired());

            foreach (var queuedRelease in expired)
            {
                var decision = _downloadDecisionMaker.GetDecisionForReport(queuedRelease.RemoteEpisode);

                if (decision.Approved)
                {
                    _logger.Debug("Downloading previously held release");
                    _downloadService.DownloadReport(decision.RemoteEpisode);
                }

                else if (decision.TemporarilyRejected)
                {
                    _logger.Debug("Continuing to hold release");
                    continue;
                }

                _logger.Debug("Removing previously held release");
                _repository.Delete(queuedRelease);
            }
        }
    }
}
