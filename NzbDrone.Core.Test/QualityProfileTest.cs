// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;

using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Providers;
using NzbDrone.Core.Repository;
using NzbDrone.Core.Repository.Quality;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common.AutoMoq;

namespace NzbDrone.Core.Test
{
    [TestFixture]
    // ReSharper disable InconsistentNaming
    public class QualityProfileTest : CoreTest
    {
        [Test]
        public void Test_Storage()
        {
            //Arrange
            var database = TestDbHelper.GetEmptyDatabase();
            var testProfile = new QualityProfile
                                  {
                                      Name = Guid.NewGuid().ToString(),
                                      Cutoff = QualityTypes.SDTV,
                                      Allowed = new List<QualityTypes> { QualityTypes.HDTV720p, QualityTypes.DVD },
                                  };

            //Act
            var id = Convert.ToInt32(database.Insert(testProfile));
            var fetch = database.SingleOrDefault<QualityProfile>(id);

            //Assert
            Assert.AreEqual(id, fetch.QualityProfileId);
            Assert.AreEqual(testProfile.Name, fetch.Name);
            Assert.AreEqual(testProfile.Cutoff, fetch.Cutoff);
            Assert.AreEqual(testProfile.Allowed, fetch.Allowed);
        }


        [Test]
        public void Test_Storage_no_allowed()
        {
            //Arrange
            var database = TestDbHelper.GetEmptyDatabase();
            var testProfile = new QualityProfile
            {
                Name = Guid.NewGuid().ToString(),
                Cutoff = QualityTypes.SDTV
            };

            //Act
            var id = Convert.ToInt32(database.Insert(testProfile));
            var fetch = database.SingleOrDefault<QualityProfile>(id);

            //Assert
            Assert.AreEqual(id, fetch.QualityProfileId);
            Assert.AreEqual(testProfile.Name, fetch.Name);
            Assert.AreEqual(testProfile.Cutoff, fetch.Cutoff);
            fetch.Allowed.Should().HaveCount(0);
        }


        [Test]
        public void Update_Success()
        {
            //Arrange
            
            var db = TestDbHelper.GetEmptyDatabase();
            Mocker.SetConstant(db);

            var testProfile = new QualityProfile
            {
                Name = Guid.NewGuid().ToString(),
                Cutoff = QualityTypes.SDTV
            };

            //Act
            var id = Convert.ToInt32(db.Insert(testProfile));
            var currentProfile = db.SingleOrDefault<QualityProfile>(id);


            //Update
            currentProfile.Cutoff = QualityTypes.Bluray720p;
            Mocker.Resolve<QualityProvider>().Update(currentProfile);

            var updated = Mocker.Resolve<QualityProvider>().Get(currentProfile.QualityProfileId);

            //Assert
            updated.Name.Should().Be(currentProfile.Name);
            updated.Cutoff.Should().Be(QualityTypes.Bluray720p);
            updated.AllowedString.Should().Be(currentProfile.AllowedString);

        }

        [Test]
        public void Test_Series_Quality()
        {
            //Arrange
            var database = TestDbHelper.GetEmptyDatabase();

            var testProfile = new QualityProfile
                                  {
                                      Name = Guid.NewGuid().ToString(),
                                      Cutoff = QualityTypes.SDTV,
                                      Allowed = new List<QualityTypes> { QualityTypes.HDTV720p, QualityTypes.DVD },
                                  };


            var profileId = Convert.ToInt32(database.Insert(testProfile));

            var series = Builder<Series>.CreateNew().Build();
            series.QualityProfileId = profileId;

            database.Insert(testProfile);
            database.Insert(series);

            var result = database.Fetch<Series>();

            result.Should().HaveCount(1);
            var profile = database.SingleOrDefault<QualityProfile>(result[0].QualityProfileId);
            Assert.AreEqual(profileId, result[0].QualityProfileId);
            Assert.AreEqual(testProfile.Name, profile.Name);
        }


        [Test]
        public void SetupInitial_should_add_two_profiles()
        {
            
            var db = TestDbHelper.GetEmptyDatabase();
            Mocker.SetConstant(db);

            //Act
            Mocker.Resolve<QualityProvider>();

            //Assert
            var profiles = Mocker.Resolve<QualityProvider>().All();


            profiles.Should().HaveCount(2);
            profiles.Should().Contain(e => e.Name == "HD");
            profiles.Should().Contain(e => e.Name == "SD");

        }

        [Test]
        //This confirms that new profiles are added only if no other profiles exists.
        //We don't want to keep adding them back if a user deleted them on purpose.
        public void SetupInitial_should_skip_if_any_profile_exists()
        {
            
            var db = TestDbHelper.GetEmptyDatabase();
            Mocker.SetConstant(db);
            var fakeProfile = Builder<QualityProfile>.CreateNew().With(p => p.Cutoff = QualityTypes.DVD).With(p => p.Allowed = new List<QualityTypes> { QualityTypes.SDTV, QualityTypes.DVD }).Build();

            //Act
            Mocker.Resolve<QualityProvider>().Add(fakeProfile);

            //Assert
            var profiles = Mocker.Resolve<QualityProvider>().All();


            profiles.Should().HaveCount(1);
        }
    }
}