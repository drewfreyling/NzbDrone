﻿using System;
using System.Collections.Generic;

using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common;
using NzbDrone.Core.Model;
using NzbDrone.Core.Providers;
using NzbDrone.Core.Providers.Core;
using NzbDrone.Core.Repository;
using NzbDrone.Core.Repository.Quality;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;
using NzbDrone.Test.Common.AutoMoq;

namespace NzbDrone.Core.Test.ProviderTests.DiskScanProviderTests
{
    // ReSharper disable InconsistentNaming
    public class ImportFileFixture : SqlCeTest
    {
        public static object[] ImportTestCases =
        {
            new object[] { QualityTypes.SDTV, false },
            new object[] { QualityTypes.DVD, true },
            new object[] { QualityTypes.HDTV720p, false }
        };

        private readonly long SIZE = 80.Megabytes();

        public void With80MBFile()
        {
            Mocker.GetMock<DiskProvider>()
                    .Setup(d => d.GetSize(It.IsAny<String>()))
                    .Returns(80.Megabytes());
        }

        [Test]
        public void import_new_file_should_succeed()
        {
            const string newFile = @"WEEDS.S03E01.DUAL.dvd.HELLYWOOD.avi";

            var fakeSeries = Builder<Series>.CreateNew().Build();
            var fakeEpisode = Builder<Episode>.CreateNew().Build();

            //Mocks
            With80MBFile();

            Mocker.GetMock<MediaFileProvider>()
                .Setup(p => p.Exists(It.IsAny<String>()))
                .Returns(false);

            Mocker.GetMock<EpisodeProvider>()
                .Setup(e => e.GetEpisodesByParseResult(It.IsAny<EpisodeParseResult>())).Returns(new List<Episode> { fakeEpisode });

            //Act
            var result = Mocker.Resolve<DiskScanProvider>().ImportFile(fakeSeries, newFile);

            //Assert
            VerifyFileImport(result, Mocker, fakeEpisode, SIZE);

        }

        [Test, TestCaseSource("ImportTestCases")]
        public void import_new_file_with_better_same_quality_should_succeed(QualityTypes currentFileQuality, bool currentFileProper)
        {
            const string newFile = @"WEEDS.S03E01.DUAL.1080p.HELLYWOOD.mkv";

            //Fakes
            var fakeSeries = Builder<Series>.CreateNew().Build();
            var fakeEpisode = Builder<Episode>.CreateNew()
                .With(e => e.EpisodeFile = Builder<EpisodeFile>.CreateNew()
                                               .With(g => g.Quality = (QualityTypes)currentFileQuality)
                                               .And(g => g.Proper = currentFileProper).Build()
                ).Build();
            

            With80MBFile();

            Mocker.GetMock<EpisodeProvider>()
                .Setup(e => e.GetEpisodesByParseResult(It.IsAny<EpisodeParseResult>())).Returns(new List<Episode> { fakeEpisode });

            //Act
            var result = Mocker.Resolve<DiskScanProvider>().ImportFile(fakeSeries, newFile);

            //Assert
            VerifyFileImport(result, Mocker, fakeEpisode, SIZE);
        }

        [TestCase("WEEDS.S03E01.DUAL.DVD.XviD.AC3.-HELLYWOOD.avi")]
        [TestCase("WEEDS.S03E01.DUAL.SDTV.XviD.AC3.-HELLYWOOD.avi")]
        public void import_new_file_episode_has_same_or_better_quality_should_skip(string fileName)
        {

            //Fakes
            var fakeSeries = Builder<Series>.CreateNew().Build();
            var fakeEpisode = Builder<Episode>.CreateNew()
                .With(c => c.EpisodeFile = Builder<EpisodeFile>.CreateNew()
                        .With(e => e.Quality = QualityTypes.Bluray720p).Build()
                     )
                .Build();

            //Mocks
            With80MBFile();

            Mocker.GetMock<MediaFileProvider>()
                .Setup(p => p.Exists(It.IsAny<String>()))
                .Returns(false);

            Mocker.GetMock<EpisodeProvider>()
                .Setup(e => e.GetEpisodesByParseResult(It.IsAny<EpisodeParseResult>())).Returns(new List<Episode> { fakeEpisode });

            //Act
            var result = Mocker.Resolve<DiskScanProvider>().ImportFile(fakeSeries, fileName);

            //Assert
            VerifySkipImport(result, Mocker);
        }

        [Test]
        public void import_unparsable_file_should_skip()
        {
            const string fileName = @"C:\Test\WEEDS.avi";

            var fakeSeries = Builder<Series>.CreateNew().Build();

            Mocker.GetMock<MediaFileProvider>()
                .Setup(p => p.Exists(It.IsAny<String>())).Returns(false);

            With80MBFile();

            //Act
            var result = Mocker.Resolve<DiskScanProvider>().ImportFile(fakeSeries, fileName);

            //Assert
            VerifySkipImport(result, Mocker);
            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void import_existing_file_should_skip()
        {
            const string fileName = "WEEDS.S03E01-06.DUAL.BDRip.XviD.AC3.-HELLYWOOD.avi";

            var fakeSeries = Builder<Series>.CreateNew().Build();

            WithStrictMocker();
            Mocker.GetMock<MediaFileProvider>()
                .Setup(p => p.Exists(It.IsAny<String>()))
                .Returns(true);

            With80MBFile();

            //Act
            var result = Mocker.Resolve<DiskScanProvider>().ImportFile(fakeSeries, fileName);

            //Assert
            VerifySkipImport(result, Mocker);
        }

        [Test]
        public void import_file_with_no_episode_in_db_should_skip()
        {
            //Constants
            const string fileName = "WEEDS.S03E01.DUAL.BDRip.XviD.AC3.-HELLYWOOD.avi";

            //Fakes
            var fakeSeries = Builder<Series>.CreateNew().Build();

            //Mocks
            Mocker.GetMock<DiskProvider>(MockBehavior.Strict)
                .Setup(e => e.IsChildOfPath(fileName, fakeSeries.Path)).Returns(false);

            With80MBFile();

            Mocker.GetMock<MediaFileProvider>()
                  .Setup(p => p.Exists(It.IsAny<String>()))
                  .Returns(false);

            Mocker.GetMock<EpisodeProvider>()
                .Setup(c => c.GetEpisodesByParseResult(It.IsAny<EpisodeParseResult>()))
                .Returns(new List<Episode>());


            //Act
            var result = Mocker.Resolve<DiskScanProvider>().ImportFile(fakeSeries, fileName);

            //Assert
            VerifySkipImport(result, Mocker);
        }

        [TestCase("WEEDS.S03E01.DUAL.DVD.XviD.AC3.-HELLYWOOD.avi")]
        [TestCase("WEEDS.S03E01.DUAL.bluray.x264.AC3.-HELLYWOOD.mkv")]
        public void import_new_file_episode_has_better_quality_than_existing(string fileName)
        {
            //Fakes
            var fakeSeries = Builder<Series>.CreateNew().Build();
            var fakeEpisode = Builder<Episode>.CreateNew()
                .With(c => c.EpisodeFile = Builder<EpisodeFile>.CreateNew()
                        .With(e => e.Quality = QualityTypes.SDTV).Build()
                     )
                .Build();

            //Mocks
            With80MBFile();

            Mocker.GetMock<MediaFileProvider>()
                .Setup(p => p.Exists(It.IsAny<String>()))
                .Returns(false);

            Mocker.GetMock<EpisodeProvider>()
                .Setup(e => e.GetEpisodesByParseResult(It.IsAny<EpisodeParseResult>())).Returns(new List<Episode> { fakeEpisode });

            //Act
            var result = Mocker.Resolve<DiskScanProvider>().ImportFile(fakeSeries, fileName);

            //Assert
            VerifyFileImport(result, Mocker, fakeEpisode, SIZE);
            Mocker.GetMock<RecycleBinProvider>().Verify(p => p.DeleteFile(It.IsAny<string>()), Times.Once());
        }

        [TestCase("WEEDS.S03E01.DUAL.hdtv.XviD.AC3.-HELLYWOOD.avi")]
        [TestCase("WEEDS.S03E01.DUAL.DVD.XviD.AC3.-HELLYWOOD.avi")]
        [TestCase("WEEDS.S03E01.DUAL.bluray.x264.AC3.-HELLYWOOD.mkv")]
        public void import_new_multi_part_file_episode_has_equal_or_better_quality_than_existing(string fileName)
        {
            //Fakes
            var fakeSeries = Builder<Series>.CreateNew().Build();

            var fakeEpisodes = Builder<Episode>.CreateListOfSize(2)
                .All()
                .With(e => e.EpisodeFile = Builder<EpisodeFile>.CreateNew()
                                               .With(f => f.Quality = QualityTypes.SDTV)
                                               .Build())
                .Build();

            With80MBFile();

            Mocker.GetMock<MediaFileProvider>()
                .Setup(p => p.Exists(It.IsAny<String>()))
                .Returns(false);

            Mocker.GetMock<EpisodeProvider>()
                .Setup(e => e.GetEpisodesByParseResult(It.IsAny<EpisodeParseResult>())).Returns(fakeEpisodes);

            //Act
            var result = Mocker.Resolve<DiskScanProvider>().ImportFile(fakeSeries, fileName);

            //Assert
            VerifyFileImport(result, Mocker, fakeEpisodes[0], SIZE);
            Mocker.GetMock<RecycleBinProvider>().Verify(p => p.DeleteFile(It.IsAny<string>()), Times.Once());
        }

        [TestCase("WEEDS.S03E01.DUAL.DVD.XviD.AC3.-HELLYWOOD.avi")]
        [TestCase("WEEDS.S03E01.DUAL.HDTV.XviD.AC3.-HELLYWOOD.avi")]
        public void skip_import_new_multi_part_file_episode_existing_has_better_quality(string fileName)
        {
            //Fakes
            var fakeSeries = Builder<Series>.CreateNew().Build();

            var fakeEpisodes = Builder<Episode>.CreateListOfSize(2)
                .All()
                .With(e => e.EpisodeFile = Builder<EpisodeFile>.CreateNew()
                                               .With(f => f.Quality = QualityTypes.Bluray720p)
                                               .Build())
                .Build();

            //Mocks
            
            With80MBFile();

            Mocker.GetMock<MediaFileProvider>()
                .Setup(p => p.Exists(It.IsAny<String>()))
                .Returns(false);

            Mocker.GetMock<EpisodeProvider>()
                .Setup(e => e.GetEpisodesByParseResult(It.IsAny<EpisodeParseResult>())).Returns(fakeEpisodes);

            //Act
            var result = Mocker.Resolve<DiskScanProvider>().ImportFile(fakeSeries, fileName);

            //Assert
            VerifySkipImport(result, Mocker);
        }

        [Test]
        public void import_new_multi_part_file_episode_replace_two_files()
        {
            const string fileName = "WEEDS.S03E01E02.DUAL.bluray.x264.AC3.-HELLYWOOD.mkv";

            //Fakes
            var fakeSeries = Builder<Series>.CreateNew().Build();

            var fakeEpisodeFiles = Builder<EpisodeFile>.CreateListOfSize(2)
                .All()
                .With(e => e.Quality = QualityTypes.SDTV)
                .Build();

            var fakeEpisode1 = Builder<Episode>.CreateNew()
                .With(c => c.EpisodeFile = fakeEpisodeFiles[0])
                .Build();

            var fakeEpisode2 = Builder<Episode>.CreateNew()
                .With(c => c.EpisodeFile = fakeEpisodeFiles[1])
                .Build();

            //Mocks
            With80MBFile();

            Mocker.GetMock<MediaFileProvider>()
                .Setup(p => p.Exists(It.IsAny<String>()))
                .Returns(false);

            Mocker.GetMock<EpisodeProvider>()
                .Setup(e => e.GetEpisodesByParseResult(It.IsAny<EpisodeParseResult>())).Returns(new List<Episode> { fakeEpisode1, fakeEpisode2 });

            //Act
            var result = Mocker.Resolve<DiskScanProvider>().ImportFile(fakeSeries, fileName);

            //Assert
            VerifyFileImport(result, Mocker, fakeEpisode1, SIZE);
            Mocker.GetMock<RecycleBinProvider>().Verify(p => p.DeleteFile(It.IsAny<string>()), Times.Exactly(2));
        }

        [Test]
        public void should_import_new_episode_no_existing_episode_file()
        {
            const string fileName = "WEEDS.S03E01E02.DUAL.bluray.x264.AC3.-HELLYWOOD.mkv";

            //Fakes
            var fakeSeries = Builder<Series>.CreateNew().Build();

            var fakeEpisode = Builder<Episode>.CreateNew()
                .With(e => e.EpisodeFileId = 0)
                .With(e => e.EpisodeFile = null)
                .Build();

            //Mocks
            With80MBFile();

            Mocker.GetMock<MediaFileProvider>()
                .Setup(p => p.Exists(It.IsAny<String>()))
                .Returns(false);

            Mocker.GetMock<EpisodeProvider>()
                .Setup(e => e.GetEpisodesByParseResult(It.IsAny<EpisodeParseResult>())).Returns(new List<Episode> { fakeEpisode});

            //Act
            var result = Mocker.Resolve<DiskScanProvider>().ImportFile(fakeSeries, fileName);

            //Assert
            VerifyFileImport(result, Mocker, fakeEpisode, SIZE);
            Mocker.GetMock<DiskProvider>().Verify(p => p.DeleteFile(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_set_parseResult_SceneSource_if_not_in_series_Path()
        {
            var series = Builder<Series>
                    .CreateNew()
                    .With(s => s.Path == @"C:\Test\TV\30 Rock")
                    .Build();

            const string path = @"C:\Test\Unsorted TV\30 Rock\30.rock.s01e01.pilot.mkv";
            
            With80MBFile();

            Mocker.GetMock<EpisodeProvider>().Setup(s => s.GetEpisodesByParseResult(It.IsAny<EpisodeParseResult>()))
                .Returns(new List<Episode>());

            Mocker.GetMock<DiskProvider>().Setup(s => s.IsChildOfPath(path, series.Path))
                    .Returns(false);

            Mocker.Resolve<DiskScanProvider>().ImportFile(series, path);

            Mocker.Verify<EpisodeProvider>(s => s.GetEpisodesByParseResult(It.Is<EpisodeParseResult>(p => p.SceneSource)), Times.Once());
        }

        [Test]
        public void should_not_set_parseResult_SceneSource_if_in_series_Path()
        {
            var series = Builder<Series>
                    .CreateNew()
                    .With(s => s.Path == @"C:\Test\TV\30 Rock")
                    .Build();

            const string path = @"C:\Test\TV\30 Rock\30.rock.s01e01.pilot.mkv";

            With80MBFile();

            Mocker.GetMock<EpisodeProvider>().Setup(s => s.GetEpisodesByParseResult(It.IsAny<EpisodeParseResult>()))
                .Returns(new List<Episode>());

            Mocker.GetMock<DiskProvider>().Setup(s => s.IsChildOfPath(path, series.Path))
                    .Returns(true);

            Mocker.Resolve<DiskScanProvider>().ImportFile(series, path);

            Mocker.Verify<EpisodeProvider>(s => s.GetEpisodesByParseResult(It.Is<EpisodeParseResult>(p => p.SceneSource == false)), Times.Once());
        }

        [Test]
        public void should_return_null_if_file_size_is_under_70MB_and_runTime_under_3_minutes()
        {
            var series = Builder<Series>
                    .CreateNew()
                    .Build();

            const string path = @"C:\Test\TV\30.rock.s01e01.pilot.avi";

            Mocker.GetMock<MediaFileProvider>()
                    .Setup(m => m.Exists(path))
                    .Returns(false);

            Mocker.GetMock<DiskProvider>()
                    .Setup(d => d.GetSize(path))
                    .Returns(20.Megabytes());

            Mocker.GetMock<MediaInfoProvider>()
                  .Setup(s => s.GetRunTime(path))
                  .Returns(60);

            Mocker.Resolve<DiskScanProvider>().ImportFile(series, path).Should().BeNull();
        }

        [Test]
        public void should_import_if_file_size_is_under_70MB_but_runTime_over_3_minutes()
        {
            var fakeSeries = Builder<Series>.CreateNew().Build();

            var fakeEpisode = Builder<Episode>.CreateNew()
                .With(e => e.EpisodeFileId = 0)
                .With(e => e.EpisodeFile = null)
                .Build();

            const string path = @"C:\Test\TV\30.rock.s01e01.pilot.avi";

            Mocker.GetMock<MediaFileProvider>()
                    .Setup(m => m.Exists(path))
                    .Returns(false);

            Mocker.GetMock<DiskProvider>()
                    .Setup(d => d.GetSize(path))
                    .Returns(20.Megabytes());

            Mocker.GetMock<MediaInfoProvider>()
                  .Setup(s => s.GetRunTime(path))
                  .Returns(600);

            Mocker.GetMock<EpisodeProvider>()
                .Setup(e => e.GetEpisodesByParseResult(It.IsAny<EpisodeParseResult>())).Returns(new List<Episode> { fakeEpisode });

            var result = Mocker.Resolve<DiskScanProvider>().ImportFile(fakeSeries, path);

            VerifyFileImport(result, Mocker, fakeEpisode, 20.Megabytes());
            Mocker.GetMock<DiskProvider>().Verify(p => p.DeleteFile(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_import_if_file_size_is_over_70MB_but_runTime_under_3_minutes()
        {
            With80MBFile();

            var fakeSeries = Builder<Series>.CreateNew().Build();

            var fakeEpisode = Builder<Episode>.CreateNew()
                .With(e => e.EpisodeFileId = 0)
                .With(e => e.EpisodeFile = null)
                .Build();

            const string path = @"C:\Test\TV\30.rock.s01e01.pilot.avi";

            Mocker.GetMock<MediaFileProvider>()
                    .Setup(m => m.Exists(path))
                    .Returns(false);

            Mocker.GetMock<MediaInfoProvider>()
                  .Setup(s => s.GetRunTime(path))
                  .Returns(60);

            Mocker.GetMock<EpisodeProvider>()
                .Setup(e => e.GetEpisodesByParseResult(It.IsAny<EpisodeParseResult>())).Returns(new List<Episode> { fakeEpisode });

            var result = Mocker.Resolve<DiskScanProvider>().ImportFile(fakeSeries, path);

            VerifyFileImport(result, Mocker, fakeEpisode, SIZE);
            Mocker.GetMock<DiskProvider>().Verify(p => p.DeleteFile(It.IsAny<string>()), Times.Never());
        }

        private static void VerifyFileImport(EpisodeFile result, AutoMoqer Mocker, Episode fakeEpisode, long size)
        {
            result.Should().NotBeNull();
            result.SeriesId.Should().Be(fakeEpisode.SeriesId);
            result.Size.Should().Be(size);
            result.DateAdded.Should().HaveDay(DateTime.Now.Day);
            Mocker.GetMock<MediaFileProvider>().Verify(p => p.Add(It.IsAny<EpisodeFile>()), Times.Once());

            //Get the count of episodes linked
            var count = Mocker.GetMock<EpisodeProvider>().Object.GetEpisodesByParseResult(null).Count;

            Mocker.GetMock<EpisodeProvider>().Verify(p => p.UpdateEpisode(It.Is<Episode>(e => e.EpisodeFileId == result.EpisodeFileId)), Times.Exactly(count));
        }

        private static void VerifySkipImport(EpisodeFile result, AutoMoqer Mocker)
        {
            result.Should().BeNull();
            Mocker.GetMock<MediaFileProvider>().Verify(p => p.Add(It.IsAny<EpisodeFile>()), Times.Never());
            Mocker.GetMock<EpisodeProvider>().Verify(p => p.UpdateEpisode(It.IsAny<Episode>()), Times.Never());
            Mocker.GetMock<DiskProvider>().Verify(p => p.DeleteFile(It.IsAny<string>()), Times.Never());
        }
    }
}
