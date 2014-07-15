using System;
using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class NotRestrictedReleaseSpecification : IDecisionEngineSpecification
    {
        private readonly IConfigService _configService;
        private readonly Logger _logger;

        public NotRestrictedReleaseSpecification(IConfigService configService, Logger logger)
        {
            _configService = configService;
            _logger = logger;
        }

        public string RejectionReason
        {
            get
            {
                return "Contains restricted term.";
            }
        }

        public RejectionType Type { get { return RejectionType.Permanent; } }

        public virtual bool IsSatisfiedBy(RemoteEpisode subject, SearchCriteriaBase searchCriteria)
        {
            _logger.Debug("Checking if release contains any restricted terms: {0}", subject);

            var restrictionsString = _configService.ReleaseRestrictions;

            if (String.IsNullOrWhiteSpace(restrictionsString))
            {
                _logger.Debug("No restrictions configured, allowing: {0}", subject);
                return true;
            }

            var restrictions = restrictionsString.Split(new []{ '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var restriction in restrictions)
            {
                if (subject.Release.Title.ToLowerInvariant().Contains(restriction.ToLowerInvariant()))
                {
                    _logger.Debug("{0} is restricted: {1}", subject, restriction);
                    return false;
                }
            }

            _logger.Debug("No restrictions apply, allowing: {0}", subject);
            return true;
        }
    }
}
