using System.Linq;
using System.Web.Mvc;
using NzbDrone.Services.Service.Providers;

namespace NzbDrone.Services.Service.Controllers
{
    public class AnimeController : Controller
    {
        private readonly AnimeProvider _animeProvider;

        public AnimeController(AnimeProvider animeProvider)
        {
            _animeProvider = animeProvider;
        }

        [HttpGet]
        [OutputCache(CacheProfile = "Cache1Hour")]
        public JsonResult All()
        {
            var all = _animeProvider.All();

            return Json(all, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [OutputCache(CacheProfile = "Cache1Hour")]
        public JsonResult AllIds()
        {
            var all = _animeProvider.AllSeriesIds();

            return Json(all, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [OutputCache(CacheProfile = "Cache1HourVaryBySeriesId")]
        public JsonResult Check(int seriesId)
        {
            var result = _animeProvider.IsDaily(seriesId);

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [OutputCache(CacheProfile = "Cache1HourVaryBySeriesId")]
        public JsonResult AnidbId(int tvdbId)
        {
            
        }
    }
}