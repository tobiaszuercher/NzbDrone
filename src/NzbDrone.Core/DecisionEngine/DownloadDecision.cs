using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine
{
    public class DownloadDecision
    {
        public RemoteEpisode RemoteEpisode { get; private set; }
        public IEnumerable<Rejection> Rejections { get; private set; }

        public bool Approved
        {
            get
            {
                return !Rejections.Any();
            }
        }

        public bool TemporarilyRejected
        {
            get
            {
                return Rejections.Any() && Rejections.All(r => r.Type == RejectionType.Temporary);
            }
        }

        public DownloadDecision(RemoteEpisode episode, params Rejection[] rejections)
        {
            RemoteEpisode = episode;
            Rejections = rejections.ToList();
        }
        
        public override string ToString()
        {
            if (Approved)
            {
                return "[OK] " + RemoteEpisode;
            }

            return "[Rejected " + Rejections.Count() + "]" + RemoteEpisode;
        }
    }
}