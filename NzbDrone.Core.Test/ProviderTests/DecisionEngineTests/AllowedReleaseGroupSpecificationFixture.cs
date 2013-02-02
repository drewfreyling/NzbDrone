﻿// ReSharper disable RedundantUsingDirective

using System.Linq;
using System;
using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Model;
using NzbDrone.Core.Providers;
using NzbDrone.Core.Providers.Core;
using NzbDrone.Core.Providers.DecisionEngine;
using NzbDrone.Core.Repository;
using NzbDrone.Core.Repository.Quality;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ProviderTests.DecisionEngineTests
{
    [TestFixture]
    // ReSharper disable InconsistentNaming
    public class AllowedReleaseGroupSpecificationFixture : SqlCeTest
    {
        private EpisodeParseResult parseResult;

        [SetUp]
        public void Setup()
        {
            parseResult = new EpisodeParseResult
                                    {
                                        SeriesTitle = "Title",
                                        Language = LanguageType.English,
                                        Quality = new QualityModel(QualityTypes.SDTV, true),
                                        EpisodeNumbers = new List<int> { 3 },
                                        SeasonNumber = 12,
                                        AirDate = DateTime.Now.AddDays(-12).Date,
                                        ReleaseGroup = "2HD"
                                    };
        }

        [Test]
        public void should_be_true_when_allowedReleaseGroups_is_empty()
        {
            Mocker.GetMock<ConfigProvider>().SetupGet(s => s.AllowedReleaseGroups).Returns(String.Empty);
            Mocker.Resolve<AllowedReleaseGroupSpecification>().IsSatisfiedBy(parseResult).Should().BeTrue();
        }

        [Test]
        public void should_be_true_when_allowedReleaseGroups_is_nzbs_releaseGroup()
        {
            Mocker.GetMock<ConfigProvider>().SetupGet(s => s.AllowedReleaseGroups).Returns("2HD");
            Mocker.Resolve<AllowedReleaseGroupSpecification>().IsSatisfiedBy(parseResult).Should().BeTrue();
        }

        [Test]
        public void should_be_true_when_allowedReleaseGroups_contains_nzbs_releaseGroup()
        {
            Mocker.GetMock<ConfigProvider>().SetupGet(s => s.AllowedReleaseGroups).Returns("2HD, LOL");
            Mocker.Resolve<AllowedReleaseGroupSpecification>().IsSatisfiedBy(parseResult).Should().BeTrue();
        }

        [Test]
        public void should_be_false_when_allowedReleaseGroups_does_not_contain_nzbs_releaseGroup()
        {
            Mocker.GetMock<ConfigProvider>().SetupGet(s => s.AllowedReleaseGroups).Returns("LOL,DTD");
            Mocker.Resolve<AllowedReleaseGroupSpecification>().IsSatisfiedBy(parseResult).Should().BeFalse();
        }
    }
}