﻿using System;
using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Tv;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.TvTests
{
    [TestFixture]
    public class RefreshEpisodeServiceFixture : CoreTest<RefreshEpisodeService>
    {
        private List<Episode> _insertedEpisodes;
        private List<Episode> _updatedEpisodes;
        private List<Episode> _deletedEpisodes;
        private Tuple<Series, List<Episode>> _gameOfThrones;


        [TestFixtureSetUp]
        public void TestFixture()
        {
            _gameOfThrones = Mocker.Resolve<TraktProxy>().GetSeriesInfo(121361);//Game of thrones
        }

        private List<Episode> GetEpisodes()
        {
            return _gameOfThrones.Item2.JsonClone();
        }

        private Series GetSeries()
        {
            return _gameOfThrones.Item1.JsonClone();
        }

        [SetUp]
        public void Setup()
        {
            _insertedEpisodes = new List<Episode>();
            _updatedEpisodes = new List<Episode>();
            _deletedEpisodes = new List<Episode>();

            Mocker.GetMock<IEpisodeService>().Setup(c => c.InsertMany(It.IsAny<List<Episode>>()))
                .Callback<List<Episode>>(e => _insertedEpisodes = e);


            Mocker.GetMock<IEpisodeService>().Setup(c => c.UpdateMany(It.IsAny<List<Episode>>()))
                .Callback<List<Episode>>(e => _updatedEpisodes = e);


            Mocker.GetMock<IEpisodeService>().Setup(c => c.DeleteMany(It.IsAny<List<Episode>>()))
                .Callback<List<Episode>>(e => _deletedEpisodes = e);
        }


        [Test]
        public void should_create_all_when_no_existing_episodes()
        {

            Mocker.GetMock<IEpisodeService>().Setup(c => c.GetEpisodeBySeries(It.IsAny<int>()))
                .Returns(new List<Episode>());

            Mocker.GetMock<ISeasonService>().Setup(c => c.GetSeasonsBySeries(It.IsAny<int>()))
                .Returns(new List<Season>());

            Subject.RefreshEpisodeInfo(GetSeries(), GetEpisodes());


            _insertedEpisodes.Should().HaveSameCount(GetEpisodes());
            _updatedEpisodes.Should().BeEmpty();
            _deletedEpisodes.Should().BeEmpty();

        }


        [Test]
        public void should_update_all_when_all_existing_episodes()
        {

            Mocker.GetMock<IEpisodeService>().Setup(c => c.GetEpisodeBySeries(It.IsAny<int>()))
                .Returns(GetEpisodes());

            Mocker.GetMock<ISeasonService>().Setup(c => c.GetSeasonsBySeries(It.IsAny<int>()))
                .Returns(new List<Season>());

            Subject.RefreshEpisodeInfo(GetSeries(), GetEpisodes());


            _insertedEpisodes.Should().BeEmpty();
            _updatedEpisodes.Should().HaveSameCount(GetEpisodes());
            _deletedEpisodes.Should().BeEmpty();

        }


        [Test]
        public void should_delete_all_when_all_existing_episodes_are_gone_from_trakt()
        {

            Mocker.GetMock<IEpisodeService>().Setup(c => c.GetEpisodeBySeries(It.IsAny<int>()))
                .Returns(GetEpisodes());

            Mocker.GetMock<ISeasonService>().Setup(c => c.GetSeasonsBySeries(It.IsAny<int>()))
                .Returns(new List<Season>());

            Subject.RefreshEpisodeInfo(GetSeries(), new List<Episode>());


            _insertedEpisodes.Should().BeEmpty();
            _updatedEpisodes.Should().BeEmpty();
            _deletedEpisodes.Should().HaveSameCount(GetEpisodes());

        }
    }
}                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             