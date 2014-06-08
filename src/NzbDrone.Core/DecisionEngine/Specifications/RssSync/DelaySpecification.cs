using System.Linq;
using NLog;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.DecisionEngine.Specifications.RssSync
{
    public class DelaySpecification : IDecisionEngineSpecification
    {
        private readonly Logger _logger;

        public DelaySpecification(Logger logger)
        {
            _logger = logger;
        }

        public string RejectionReason
        {
            get
            {
                return "Waiting for better quality release";
            }
        }

        public RejectionType Type { get { return RejectionType.Temporary; } }

        public virtual bool IsSatisfiedBy(RemoteEpisode subject, SearchCriteriaBase searchCriteria)
        {
            //How do we want to handle drone being off and the automatic search being triggered?
            //TODO: Add a flag to the search to state it is a "scheduled" search

            if (searchCriteria != null)
            {
                _logger.Debug("Ignore delay for searches");
                return true;
            }

            var profile = subject.Series.Profile.Value;

            if (profile.Delay == 0)
            {
                _logger.Debug("Profile does not delay before download");
                return true;
            }

            //If quality meets or exceeds the best allowed quality in the profile accept it immediately
            var bestQualityInProfile = new QualityModel(profile.Items.Last(q => q.Allowed).Quality);

            var compare = new QualityModelComparer(profile).Compare(subject.ParsedEpisodeInfo.Quality, bestQualityInProfile);

            if (compare >= 0)
            {
                return true;
            }

            if (subject.Release.AgeHours < profile.Delay)
            {
                return false;
            }

            return true;
        }
    }
}
