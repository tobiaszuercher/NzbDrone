using System;
using System.Data;
using Migrator.Framework;
using NzbDrone.Common;

namespace NzbDrone.Core.Datastore.Migrations
{
    [Migration(20121224)]
    public class Migration20121224 : NzbDroneMigration
    {
        protected override void MainDbUpgrade()
        {
            Database.AddColumn("Series", new Column("SeriesType", DbType.Int32, ColumnProperty.Null));

            Database.ExecuteNonQuery("UPDATE Series SET SeriesType = 0 WHERE IsDaily = 'false'");
            Database.ExecuteNonQuery("UPDATE Series SET SeriesType = 1 WHERE IsDaily = 'true'");

            Database.RemoveColumn("Series", "IsDaily");
        }
    }
}