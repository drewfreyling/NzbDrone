﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Providers;
using NzbDrone.Core.Repository;
using NzbDrone.Core.Repository.Quality;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common.AutoMoq;
using PetaPoco;

namespace NzbDrone.Core.Test
{
    [TestFixture]
    [Explicit]
    [Category("Benchmark")]
    // ReSharper disable InconsistentNaming
    public class DbBenchmark : SqlCeTest
    {
        const int Episodes_Per_Season = 20;
        private readonly List<int> seasonsNumbers = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        private readonly List<int> seriesIds = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 };
        private readonly List<Episode> episodes = new List<Episode>();
        private readonly List<EpisodeFile> files = new List<EpisodeFile>();
        private IDatabase db;


        [TestFixtureSetUp]
        public void Setup()
        {
            WithRealDb();

            int currentFileId = 0;


            var qulityProfile = new QualityProfile
                                    {
                                        Name = "TestProfile",
                                        Allowed = new List<QualityTypes> { QualityTypes.DVD, QualityTypes.Bluray1080p },
                                        Cutoff = QualityTypes.DVD
                                    };
            Db.Insert(qulityProfile);

            foreach (var _seriesId in seriesIds)
            {
                int seriesId = _seriesId;
                var series = Builder<Series>.CreateNew()
                    .With(s => s.SeriesId = seriesId)
                    .With(s => s.Monitored = true)
                    .Build();

                Db.Insert(series);

                foreach (var _seasonNumber in seasonsNumbers)
                {
                    for (int i = 1; i <= Episodes_Per_Season; i++)
                    {
                        var epFileId = 0;

                        if (i < 10)
                        {
                            var epFile = Builder<EpisodeFile>.CreateNew()
                               .With(e => e.SeriesId = seriesId)
                                .And(e => e.SeasonNumber = _seasonNumber)
                               .And(e => e.Path = Guid.NewGuid().ToString())
                               .Build();

                            files.Add(epFile);

                            currentFileId++;
                            epFileId = currentFileId;

                        }


                        var episode = Builder<Episode>.CreateNew()
                            .With(e => e.SeriesId = seriesId)
                            .And(e => e.SeasonNumber = _seasonNumber)
                            .And(e => e.EpisodeNumber = i)
                            .And(e => e.Ignored = false)
                            .And(e => e.TvDbEpisodeId = episodes.Count + 1)
                            .And(e => e.EpisodeFileId = epFileId)
                            .And(e => e.AirDate = DateTime.Today.AddDays(-20))
                            .Build();

                        episodes.Add(episode);


                    }
                }

            }

            Db.InsertMany(episodes);
            Db.InsertMany(files);
        }


        [Test]
        public void get_episode_by_series_seasons_episode_x5000()
        {
            
            Mocker.SetConstant(db);
            Mocker.Resolve<SeriesProvider>();

            var epProvider = Mocker.Resolve<EpisodeProvider>();

            Thread.Sleep(1000);

            var random = new Random();
            Console.WriteLine("Starting Test");

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 5000; i++)
            {
                var ep = epProvider.GetEpisode(6, random.Next(2, 5), random.Next(2, Episodes_Per_Season - 10));
                ep.Series.Should().NotBeNull();
            }

            sw.Stop();

            Console.WriteLine("Took " + sw.Elapsed);
        }

        [Test]
        public void get_episode_by_series_seasons_x1000()
        {
            
            Mocker.SetConstant(db);
            Mocker.Resolve<SeriesProvider>();

            var epProvider = Mocker.Resolve<EpisodeProvider>();


            Thread.Sleep(1000);


            var random = new Random();
            Console.WriteLine("Starting Test");

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 1000; i++)
            {
                epProvider.GetEpisodesBySeason(6, random.Next(2, 5)).Should().NotBeNull();
            }


            sw.Stop();

            Console.WriteLine("Took " + sw.Elapsed);
        }

        [Test]
        public void get_episode_file_count_x100()
        {
            
            Mocker.SetConstant(db);
            Mocker.Resolve<SeriesProvider>();
            Mocker.Resolve<EpisodeProvider>();
            var mediaProvider = Mocker.Resolve<MediaFileProvider>();


            Thread.Sleep(1000);


            var random = new Random();
            Console.WriteLine("Starting Test");

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++)
            {
                mediaProvider.GetEpisodeFilesCount(random.Next(1, 5)).Should().NotBeNull();
            }


            sw.Stop();

            Console.WriteLine("Took " + sw.Elapsed);
        }

        [Test]
        public void get_episode_file_count_x1000()
        {
            
            Mocker.SetConstant(db);
            Mocker.Resolve<SeriesProvider>();
            Mocker.Resolve<EpisodeProvider>();
            var mediaProvider = Mocker.Resolve<MediaFileProvider>();


            Thread.Sleep(1000);


            var random = new Random();
            Console.WriteLine("Starting Test");

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 1000; i++)
            {
                mediaProvider.GetEpisodeFilesCount(random.Next(1, 5)).Should().NotBeNull();
            }


            sw.Stop();

            Console.WriteLine("Took " + sw.Elapsed);
        }


        [Test]
        public void get_season_count_x500()
        {
            
            Mocker.SetConstant(db);
            var provider = Mocker.Resolve<EpisodeProvider>();


            Thread.Sleep(1000);


            var random = new Random();
            Console.WriteLine("Starting Test");

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 500; i++)
            {
                provider.GetSeasons(random.Next(1, 10)).Should().HaveSameCount(seasonsNumbers);
            }


            sw.Stop();

            Console.WriteLine("Took " + sw.Elapsed);
        }


       
    }
}
