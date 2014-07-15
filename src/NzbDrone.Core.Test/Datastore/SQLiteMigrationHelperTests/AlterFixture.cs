﻿using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Datastore.Migration.Framework;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;
using System.Linq;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Datastore.SQLiteMigrationHelperTests
{
    [TestFixture]
    public class AlterFixture : DbTest
    {
        private SqLiteMigrationHelper _subject;

        [SetUp]
        public void SetUp()
        {
            _subject = Mocker.Resolve<SqLiteMigrationHelper>();
        }

        [Test]
        public void should_parse_existing_columns()
        {
            var columns = _subject.GetColumns("Series");

            columns.Should().NotBeEmpty();

            columns.Values.Should().NotContain(c => string.IsNullOrWhiteSpace(c.Name));
            columns.Values.Should().NotContain(c => string.IsNullOrWhiteSpace(c.Schema));
        }

        [Test]
        public void should_create_table_from_column_list()
        {
            var columns = _subject.GetColumns("Series");
            columns.Remove("Title");

            _subject.CreateTable("Series_New", columns.Values, new List<SQLiteIndex>());

            var newColumns = _subject.GetColumns("Series_New");

            newColumns.Values.Should().HaveSameCount(columns.Values);
            newColumns.Should().NotContainKey("Title");
        }


        [Test]
        public void should_be_able_to_transfer_empty_tables()
        {
            var columns = _subject.GetColumns("Series");
            var indexes = _subject.GetIndexes("Series");
            columns.Remove("Title");

            _subject.CreateTable("Series_New", columns.Values, indexes);


            _subject.CopyData("Series", "Series_New", columns.Values);
        }

        [Test]
        public void should_transfer_table_with_data()
        {
            var originalEpisodes = Builder<Episode>.CreateListOfSize(10).BuildListOfNew();

            Mocker.Resolve<EpisodeRepository>().InsertMany(originalEpisodes);

            var columns = _subject.GetColumns("Episodes");
            var indexes = _subject.GetIndexes("Episodes");

            columns.Remove("Title");

            _subject.CreateTable("Episodes_New", columns.Values, indexes);

            _subject.CopyData("Episodes", "Episodes_New", columns.Values);

            _subject.GetRowCount("Episodes_New").Should().Be(originalEpisodes.Count);
        }

        [Test]
        public void should_read_existing_indexes()
        {
            var indexes = _subject.GetIndexes("QualityDefinitions");

            indexes.Should().NotBeEmpty();

            indexes.Should().OnlyContain(c => c != null);
            indexes.Should().OnlyContain(c => !string.IsNullOrWhiteSpace(c.Column));
            indexes.Should().OnlyContain(c => c.Table == "QualityDefinitions");
            indexes.Should().OnlyContain(c => c.Unique);
        }

        [Test]
        public void should_add_indexes_when_creating_new_table()
        {
            var columns = _subject.GetColumns("QualityDefinitions");
            var indexes = _subject.GetIndexes("QualityDefinitions");

            _subject.CreateTable("QualityDefinitionsB", columns.Values, indexes);

            var newIndexes = _subject.GetIndexes("QualityDefinitionsB");

            newIndexes.Should().HaveSameCount(indexes);
            newIndexes.Select(c=>c.Column).Should().BeEquivalentTo(indexes.Select(c=>c.Column));
        }

        [Test]
        public void should_be_able_to_create_table_with_new_indexes()
        {
            var columns = _subject.GetColumns("Series");
            columns.Remove("Title");

            _subject.CreateTable("Series_New", columns.Values, new List<SQLiteIndex>{new SQLiteIndex{Column = "AirTime", Table = "Series_New", Unique = true}});

            var newColumns = _subject.GetColumns("Series_New");
            var newIndexes = _subject.GetIndexes("Series_New");

            newColumns.Values.Should().HaveSameCount(columns.Values);
            newIndexes.Should().Contain(i=>i.Column == "AirTime");
        }

        [Test]
        public void should_create_indexes_with_the_same_uniqueness()
        {
            var columns = _subject.GetColumns("Series");
            var indexes = _subject.GetIndexes("Series");

            var tempIndexes = indexes.JsonClone();

            tempIndexes[0].Unique = false;
            tempIndexes[1].Unique = true;

            _subject.CreateTable("Series_New", columns.Values, tempIndexes);
            var newIndexes = _subject.GetIndexes("Series_New");

            newIndexes.Should().HaveSameCount(tempIndexes);
            newIndexes.ShouldAllBeEquivalentTo(tempIndexes, options  => options.Excluding(o => o.IndexName).Excluding(o => o.Table));
        }
    }
}