using System.Linq;
using NLog;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.Clients.Sabnzbd;
using NzbDrone.Core.History;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications.RssSync
{
    public class HistorySpecification : IDecisionEngineSpecification
    {
        private readonly IHistoryService _historyService;
        private readonly QualityUpgradableSpecification _qualityUpgradableSpecification;
        private readonly IProvideDownloadClient _downloadClientProvider;
        private readonly Logger _logger;

        public HistorySpecification(IHistoryService historyService,
                                           QualityUpgradableSpecification qualityUpgradableSpecification,
                                           IProvideDownloadClient downloadClientProvider,
                                           Logger logger)
        {
            _historyService = historyService;
            _qualityUpgradableSpecification = qualityUpgradableSpecification;
            _downloadClientProvider = downloadClientProvider;
            _logger = logger;
        }

        public string RejectionReason
        {
            get
            {
                return "Existing file in history is of equal or higher quality";
            }
        }

        public RejectionType Type { get { return RejectionType.Permanent; } }

        public virtual bool IsSatisfiedBy(RemoteEpisode subject, SearchCriteriaBase searchCriteria)
        {
            if (searchCriteria != null)
            {
                _logger.Debug("Skipping history check during search");
                return true;
            }

            var downloadClients = _downloadClientProvider.GetDownloadClients();

            foreach (var downloadClient in downloadClients.OfType<Sabnzbd>())
            {
                _logger.Debug("Performing history status check on report");
                foreach (var episode in subject.Episodes)
                {
                    _logger.Debug("Checking current status of episode [{0}] in history", episode.Id);
                    var mostRecent = _historyService.MostRecentForEpisode(episode.Id);

                    if (mostRecent != null && mostRecent.EventType == HistoryEventType.Grabbed)
                    {
                        _logger.Debug("Latest history item is downloading, rejecting.");
                        return false;
                    }
                }
                return true;
            }

            foreach (var episode in subject.Episodes)
            {
                var bestQualityInHistory = _historyService.GetBestQualityInHistory(subject.Series.Profile, episode.Id);
                if (bestQualityInHistory != null)
                {
                    _logger.Debug("Comparing history quality with report. History is {0}", bestQualityInHistory);
                    if (!_qualityUpgradableSpecification.IsUpgradable(subject.Series.Profile, bestQualityInHistory, subject.ParsedEpisodeInfo.Quality))
                        return false;
                }
            }

            return true;
        }
    }
}
