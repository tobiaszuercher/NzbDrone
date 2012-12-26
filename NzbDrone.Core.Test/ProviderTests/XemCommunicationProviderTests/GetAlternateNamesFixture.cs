using System;
using System.IO;
using System.Linq;
using System.Net;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Model.Notification;
using NzbDrone.Core.Providers;
using NzbDrone.Core.Repository;
using NzbDrone.Core.Repository.Quality;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common.AutoMoq;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.ProviderTests.XemCommunicationProviderTests
{
    [TestFixture]
    // ReSharper disable InconsistentNaming
    public class GetAlternateNamesFixture : CoreTest
    {
        private void WithFailureJson()
        {
            Mocker.GetMock<HttpProvider>().Setup(s => s.DownloadString(It.IsAny<String>()))
                    .Returns(File.ReadAllText(@".\Files\Xem\Failure.txt"));
        }

        private void WithNamesJson()
        {
            Mocker.GetMock<HttpProvider>().Setup(s => s.DownloadString(It.IsAny<String>()))
                    .Returns(File.ReadAllText(@".\Files\Xem\Names.txt"));
        }

        private void WithNames2Json()
        {
            Mocker.GetMock<HttpProvider>().Setup(s => s.DownloadString(It.IsAny<String>()))
                    .Returns(File.ReadAllText(@".\Files\Xem\Names2.txt"));
        }

        [Test]
        public void should_throw_when_failure_is_found()
        {
            WithFailureJson();
            Assert.Throws<XemException>(() => Mocker.Resolve<XemCommunicationProvider>().GetAlternateNames(12345));
        }

        [Test]
        public void should_get_list_of_alternateNames()
        {
            WithNamesJson();
            Mocker.Resolve<XemCommunicationProvider>().GetAlternateNames(12345).Should().NotBeEmpty();
        }

        [Test]
        public void should_only_get_us_names()
        {
            WithNames2Json();
            Mocker.Resolve<XemCommunicationProvider>().GetAlternateNames(12345).Should().HaveCount(6);
        }
    }
}