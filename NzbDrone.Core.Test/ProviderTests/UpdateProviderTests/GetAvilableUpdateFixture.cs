﻿using System;

using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common;
using NzbDrone.Core.Providers;
using NzbDrone.Core.Providers.Core;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common.AutoMoq;

namespace NzbDrone.Core.Test.ProviderTests.UpdateProviderTests
{
    class GetAvilableUpdateFixture : SqlCeTest
    {
        private static Version _latestsTestVersion = new Version("0.6.0.3");
        private static string _latestsTestUrl = "http://update.nzbdrone.com/_test/NzbDrone.master.0.6.0.3.zip";
        private static string _latestsTestFileName = "NzbDrone.master.0.6.0.3.zip";

        [SetUp]
        public void Setup()
        {
            WithStrictMocker();

            Mocker.GetMock<ConfigProvider>().SetupGet(c => c.UpdateUrl).Returns("http://update.nzbdrone.com/_test/");
            Mocker.Resolve<HttpProvider>();
        }

        [TestCase("0.6.0.9")]
        [TestCase("0.7.0.1")]
        [TestCase("1.0.0.0")]
        public void should_return_null_if_latests_is_lower_than_current_version(string currentVersion)
        {

            var updatePackage = Mocker.Resolve<UpdateProvider>().GetAvilableUpdate(new Version(currentVersion));

            updatePackage.Should().BeNull();
        }

        [Test]
        public void should_return_null_if_latests_is_equal_to_current_version()
        {
            var updatePackage = Mocker.Resolve<UpdateProvider>().GetAvilableUpdate(_latestsTestVersion);

            updatePackage.Should().BeNull();
        }

        [TestCase("0.0.0.0")]
        [TestCase("0.0.0.1")]
        [TestCase("0.0.10.10")]
        public void should_return_update_if_latests_is_higher_than_current_version(string currentVersion)
        {
            var updatePackage = Mocker.Resolve<UpdateProvider>().GetAvilableUpdate(new Version(currentVersion));

            updatePackage.Should().NotBeNull();
            updatePackage.Version.Should().Be(_latestsTestVersion);
            updatePackage.FileName.Should().BeEquivalentTo(_latestsTestFileName);
            updatePackage.Url.Should().BeEquivalentTo(_latestsTestUrl);
        }
    }
}
