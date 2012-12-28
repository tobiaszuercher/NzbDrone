using System;
using System.Data;
using Migrator.Framework;
using NzbDrone.Common;

namespace NzbDrone.Core.Datastore.Migrations
{
    [Migration(20121231)]
    public class Migration20121231 : NzbDroneMigration
    {
        protected override void MainDbUpgrade()
        {
            Database.AddColumn("Series", new Column("SeriesType", DbType.Int32, ColumnProperty.Null));

            Database.ExecuteNonQuery("UPDATE Series SET SeriesType = 0 WHERE IsDaily = 'false'");
            Database.ExecuteNonQuery("UPDATE Series SET SeriesType = 1 WHERE IsDaily = 'true'");

            Database.RemoveColumn("Series", "IsDaily");

            Database.AddColumn("SceneMappings", new Column("Source", DbType.Int32, ColumnProperty.Null));

            Database.AddColumn("EpisodeFiles", new Column("SubGroup", DbType.String, ColumnProperty.Null));
        }
    }
}