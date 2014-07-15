using NLog;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class LanguageSpecification : IDecisionEngineSpecification
    {
        private readonly Logger _logger;

        public LanguageSpecification(Logger logger)
        {
            _logger = logger;
        }

        public string RejectionReason
        {
            get
            {
                return "Language is not wanted";
            }
        }

        public RejectionType Type { get { return RejectionType.Permanent; } }

        public virtual bool IsSatisfiedBy(RemoteEpisode subject, SearchCriteriaBase searchCriteria)
        {
            var wantedLanguage = subject.Series.Profile.Value.Language;
            
            _logger.Debug("Checking if report meets language requirements. {0}", subject.ParsedEpisodeInfo.Language);

            if (subject.ParsedEpisodeInfo.Language != wantedLanguage)
            {
                _logger.Debug("Report Language: {0} rejected because it is not wanted, wanted {1}", subject.ParsedEpisodeInfo.Language, wantedLanguage);
                return false;
            }

            return true;
        }
    }
}
