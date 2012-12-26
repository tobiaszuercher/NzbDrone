using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Services.PetaPoco;


namespace NzbDrone.Services.Service.Repository
{
    [TableName("Anime")]
    [PrimaryKey("Id", autoIncrement = false)]
    public class Anime
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public int AnidbId { get; set; }
    }
}