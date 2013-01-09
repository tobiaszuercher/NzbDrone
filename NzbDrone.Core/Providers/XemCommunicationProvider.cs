using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NzbDrone.Common;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Model.Xem;

namespace NzbDrone.Core.Providers
{
    public class XemCommunicationProvider
    {
        private readonly HttpProvider _httpProvider;

        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private const string XEM_BASE_URL = "http://thexem.de/map/";

        public XemCommunicationProvider(HttpProvider httpProvider)
        {
            _httpProvider = httpProvider;
        }

        public XemCommunicationProvider()
        {
        }

        public virtual List<Int32> GetXemSeriesIds(string origin = "tvdb")
        {
            _logger.Trace("Fetching Series IDs from: {0}", origin);

            var url = String.Format("{0}havemap?origin={1}", XEM_BASE_URL, origin);
            var response =_httpProvider.DownloadString(url);

            CheckForFailureResult(response);

            var result = JsonConvert.DeserializeObject<XemResult<List<Int32>>>(response);

            return result.Data.ToList();
        }

        public virtual List<XemSceneTvdbMapping> GetSceneTvdbMappings(int id)
        {
            _logger.Trace("Fetching Mappings for: {0}", id);
            var url = String.Format("{0}all?id={1}&origin=tvdb", XEM_BASE_URL, id);
            var response = _httpProvider.DownloadString(url);

            CheckForFailureResult(response);

            var result = JsonConvert.DeserializeObject<List<XemSceneTvdbMapping>>(JObject.Parse(response).SelectToken("data").ToString());

            return result;
        }

        public virtual List<XemAlternateName> GetAlternateNames(int id, string origin = "tvdb")
        {
            _logger.Trace("Fetching Alternate Names for: {0} from: {1}", id, origin);

            var url = String.Format("{0}names?origin={1}&id={2}", XEM_BASE_URL, origin, id);
            var response = _httpProvider.DownloadString(url);

            CheckForFailureResult(response);

            var alternateNames = new List<XemAlternateName>();

            var result = JObject.Parse(response);
            var data = result.GetValue("data");
            var children = data.Children();

            foreach(JProperty child in children)
            {
                int seasonNumber;
                var season = child.Name;
                if(!Int32.TryParse(season, out seasonNumber)) seasonNumber = -1;

                var languages = JsonConvert.DeserializeObject<Dictionary<String, List<String>>>(child.Value.ToString());

                foreach(var language in languages)
                {
                    if (!language.Key.Equals("us", StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    foreach(var name in language.Value)
                    {
                        alternateNames.Add(new XemAlternateName{ SeasonNumber = seasonNumber, Name = name });
                    }
                }
            }

            return alternateNames;
        }

        public virtual void CheckForFailureResult(string response)
        {
            var result = JsonConvert.DeserializeObject<XemResult<dynamic>>(response);

            if (result != null && result.Result.Equals("failure", StringComparison.InvariantCultureIgnoreCase))
                throw new XemException("Error response received from Xem: " + result.Message);
        }
    }
}
