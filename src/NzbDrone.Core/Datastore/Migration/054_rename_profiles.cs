using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(54)]
    public class rename_profiles : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Rename.Table("QualityProfiles").To("Profiles");

            Alter.Table("Profiles").AddColumn("Language").AsInt32().Nullable();
            Alter.Table("Profiles").AddColumn("Delay").AsInt32().Nullable();
            Execute.Sql("UPDATE Profiles SET Language = 0, Delay = 0");

            //Rename QualityProfileId in Series
            Alter.Table("Series").AddColumn("ProfileId").AsInt32().Nullable();
            Execute.Sql("UPDATE Series SET ProfileId = QualityProfileId");

            //Add HeldReleases
            Create.TableForModel("HeldReleases")
                  .WithColumn("SeriesId").AsInt32()
                  .WithColumn("Title").AsString()
                  .WithColumn("Added").AsDateTime()
                  .WithColumn("Expiry").AsDateTime()
                  .WithColumn("ParsedEpisodeInfo").AsString()
                  .WithColumn("Release").AsString();
        }
    }
}
