using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Model;
using NzbDrone.Core.Model.Xem;
using NzbDrone.Core.Providers;
using NzbDrone.Core.Repository;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.ProviderTests.XemProvidedTests
{
    [TestFixture]
    public class UpdateAlternateNamesFixture : CoreTest
    {
        private IList<Series> _series;
        private List<XemAlternateName> _alternateNames;

        [SetUp]
        public void Setup()
        {
            _series = Builder<Series>
                    .CreateListOfSize(5)
                    .Build();

            _alternateNames = Builder<XemAlternateName>
                    .CreateListOfSize(5)
                    .Build()
                    .ToList();

            Mocker.GetMock<SeriesProvider>()
                  .Setup(s => s.GetAllSeries())
                  .Returns(_series);

            
        }

        private void WithMappings()
        {
            Mocker.GetMock<XemCommunicationProvider>()
                  .Setup(s => s.GetAlternateNames(It.IsAny<Int32>(), "tvdb"))
                  .Returns(_alternateNames);
        }

        [Test]
        public void should_log_error_when_xemException_is_thrown()
        {
            Mocker.GetMock<XemCommunicationProvider>()
                  .Setup(s => s.GetAlternateNames(It.IsAny<Int32>(), "tvdb"))
                  .Throws(new XemException());

            Mocker.Resolve<XemProvider>().UpdateAlternateNames(1);

            ExceptionVerification.ExpectedErrors(1);
        }

        [Test]
        public void should_delete_existing_mappings_when_updating()
        {
            WithMappings();

            Mocker.Resolve<XemProvider>().UpdateAlternateNames(1);

            Mocker.GetMock<SceneMappingProvider>()
                .Verify(v => v.DeleteMappings(1, SceneMappingSourceType.Xem), Times.Once());
        }

        [Test]
        public void should_insert_new_mappings()
        {
            WithMappings();

            Mocker.Resolve<XemProvider>().UpdateAlternateNames(1);

            Mocker.GetMock<SceneMappingProvider>()
                .Verify(v => v.InsertMappings(It.IsAny<List<SceneMapping>>()), Times.Once());
        }

        [Test]
        public void should_insert_mappings_with_source_of_xem()
        {
            WithMappings();

            Mocker.Resolve<XemProvider>().UpdateAlternateNames(1);

            Mocker.GetMock<SceneMappingProvider>()
                .Verify(v => v.InsertMappings(It.Is<List<SceneMapping>>(s => s.All(n => n.Source == SceneMappingSourceType.Xem))), Times.Once());
        }

        [Test]
        public void should_set_clean_title()
        {
            WithMappings();

            Mocker.Resolve<XemProvider>().UpdateAlternateNames(1);

            Mocker.GetMock<SceneMappingProvider>()
                .Verify(v => v.InsertMappings(It.Is<List<SceneMapping>>(s => s.All(n => n.CleanTitle == Parser.NormalizeTitle(n.SceneName)))), Times.Once());
        }

        [Test]
        public void should_get_names_for_intersecting_series_only()
        {
            WithMappings();

            Mocker.GetMock<XemCommunicationProvider>()
                  .Setup(s => s.GetXemSeriesIds("tvdb"))
                  .Returns(new List<int> {_series.First().SeriesId, _series.Last().SeriesId});

            Mocker.Resolve<XemProvider>().UpdateAlternateNames();

            Mocker.GetMock<XemCommunicationProvider>()
                .Verify(v => v.GetAlternateNames(It.IsAny<Int32>(), "tvdb"), Times.Exactly(2));
        }
    }
}
