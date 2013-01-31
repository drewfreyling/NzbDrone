﻿// ReSharper disable RedundantUsingDirective

using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using FizzWare.NBuilder;
using FluentAssertions;
using NCrunch.Framework;
using NUnit.Framework;
using NzbDrone.Core.Jobs;
using NzbDrone.Core.Model;
using NzbDrone.Core.Repository;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;
using NzbDrone.Test.Common.AutoMoq;

namespace NzbDrone.Core.Test.ProviderTests.JobProviderTests
{
    [TestFixture]
    [ExclusivelyUses("JOB_PROVIDER")]
    public class JobProviderFixture : CoreTest
    {

        FakeJob fakeJob;
        SlowJob slowJob;
        BrokenJob brokenJob;
        DisabledJob disabledJob;

        [SetUp]
        public void Setup()
        {
            WithRealDb();
            fakeJob = new FakeJob();
            slowJob = new SlowJob();
            brokenJob = new BrokenJob();
            disabledJob = new DisabledJob();
        }

        [TearDown]
        public void TearDown()
        {
            Mocker.Resolve<JobProvider>().Queue.Should().BeEmpty();
        }

        private void ResetLastExecution()
        {
            var jobProvider = Mocker.Resolve<JobProvider>();

            var jobs = jobProvider.All();
            foreach (var jobDefinition in jobs)
            {
                jobDefinition.LastExecution = new DateTime(2000, 1, 1);
                jobProvider.SaveDefinition(jobDefinition);
            }
        }

        private void WaitForQueue()
        {
            Console.WriteLine("Waiting for queue to clear.");
            var stopWatch = Mocker.Resolve<JobProvider>().StopWatch;

            while (stopWatch.IsRunning)
            {
                Thread.Sleep(10);
            }
        }

        [Test]
        public void running_scheduled_jobs_should_updates_last_execution_time()
        {
            IEnumerable<IJob> BaseFakeJobs = new List<IJob> { fakeJob };
            Mocker.SetConstant(BaseFakeJobs);

            
            ResetLastExecution();
            Mocker.Resolve<JobProvider>().QueueScheduled();
            WaitForQueue();

            
            var settings = Mocker.Resolve<JobProvider>().All();
            settings.First().LastExecution.Should().BeWithin(TimeSpan.FromSeconds(10));
            fakeJob.ExecutionCount.Should().Be(1);
        }

        [Test]
        public void failing_scheduled_job_should_mark_job_as_failed()
        {
            IEnumerable<IJob> BaseFakeJobs = new List<IJob> { brokenJob };
            Mocker.SetConstant(BaseFakeJobs);

            
            ResetLastExecution();
            Mocker.Resolve<JobProvider>().QueueScheduled();
            WaitForQueue();

            
            var settings = Mocker.Resolve<JobProvider>().All();
            settings.First().LastExecution.Should().BeWithin(TimeSpan.FromSeconds(10));
            settings.First().Success.Should().BeFalse();
            brokenJob.ExecutionCount.Should().Be(1);
            ExceptionVerification.ExpectedErrors(1);
        }

        [Test]
        public void scheduler_skips_jobs_that_arent_mature_yet()
        {
            IEnumerable<IJob> BaseFakeJobs = new List<IJob> { fakeJob };
            Mocker.SetConstant(BaseFakeJobs);

            
            ResetLastExecution();

            Mocker.Resolve<JobProvider>().QueueScheduled();
            WaitForQueue();

            Mocker.Resolve<JobProvider>().QueueScheduled();
            WaitForQueue();

            
            fakeJob.ExecutionCount.Should().Be(1);
        }

        [Test]
        //This test will confirm that the concurrency checks are rest
        //after execution so the job can successfully run.
        public void can_run_async_job_again()
        {
            IEnumerable<IJob> BaseFakeJobs = new List<IJob> { fakeJob };
            Mocker.SetConstant(BaseFakeJobs);

            var jobProvider = Mocker.Resolve<JobProvider>();


            
            jobProvider.QueueJob(typeof(FakeJob));
            WaitForQueue();
            jobProvider.QueueJob(typeof(FakeJob));
            WaitForQueue();

            
            jobProvider.Queue.Should().BeEmpty();
            fakeJob.ExecutionCount.Should().Be(2);
        }

        [Test]
        public void no_concurent_jobs()
        {
            IEnumerable<IJob> BaseFakeJobs = new List<IJob> { slowJob };
            Mocker.SetConstant(BaseFakeJobs);

            var jobProvider = Mocker.Resolve<JobProvider>();
            jobProvider.QueueJob(typeof(SlowJob), 1);
            jobProvider.QueueJob(typeof(SlowJob), 2);
            jobProvider.QueueJob(typeof(SlowJob), 3);

            WaitForQueue();

            jobProvider.Queue.Should().BeEmpty();
            slowJob.ExecutionCount.Should().Be(3);
            ExceptionVerification.AssertNoUnexcpectedLogs();
        }


        [Test]
        public void can_run_broken_job_again()
        {
            IEnumerable<IJob> BaseFakeJobs = new List<IJob> { brokenJob };

            Mocker.SetConstant(BaseFakeJobs);

            var jobProvider = Mocker.Resolve<JobProvider>();

            
            jobProvider.QueueJob(typeof(BrokenJob));
            WaitForQueue();

            jobProvider.QueueJob(typeof(BrokenJob));
            WaitForQueue();

            
            brokenJob.ExecutionCount.Should().Be(2);
            ExceptionVerification.ExpectedErrors(2);
        }

        [Test]
        public void schedule_hit_should_be_ignored_if_queue_is_running()
        {
            IEnumerable<IJob> fakeJobs = new List<IJob> { slowJob, fakeJob };

            Mocker.SetConstant(fakeJobs);

            var jobProvider = Mocker.Resolve<JobProvider>();


            
            jobProvider.QueueJob(typeof(SlowJob));
            jobProvider.QueueScheduled();
            WaitForQueue();

            
            slowJob.ExecutionCount.Should().Be(1);
            fakeJob.ExecutionCount.Should().Be(0);
        }


        [Test]
        public void can_queue_jobs_at_the_same_time()
        {
            IEnumerable<IJob> BaseFakeJobs = new List<IJob> { slowJob, fakeJob };
            Mocker.SetConstant(BaseFakeJobs);

            var jobProvider = Mocker.Resolve<JobProvider>();


            jobProvider.QueueJob(typeof(SlowJob));
            var thread1 = new Thread(() => jobProvider.QueueJob(typeof(FakeJob)));
            var thread2 = new Thread(() => jobProvider.QueueJob(typeof(FakeJob)));

            thread1.Start();
            thread2.Start();

            thread1.Join();
            thread2.Join();

            WaitForQueue();

            fakeJob.ExecutionCount.Should().Be(1);
            slowJob.ExecutionCount.Should().Be(1);
            jobProvider.Queue.Should().BeEmpty();
        }

        [Test]
        public void Init_Jobs()
        {
            IEnumerable<IJob> BaseFakeJobs = new List<IJob> { fakeJob };

            Mocker.SetConstant(BaseFakeJobs);

            var jobProvider = Mocker.Resolve<JobProvider>();


            
            var timers = jobProvider.All();
            timers.Should().HaveCount(1);
            timers[0].Interval.Should().Be((Int32)fakeJob.DefaultInterval.TotalMinutes);
            timers[0].Name.Should().Be(fakeJob.Name);
            timers[0].TypeName.Should().Be(fakeJob.GetType().ToString());
            timers[0].LastExecution.Should().HaveYear(DateTime.Now.Year);
            timers[0].LastExecution.Should().HaveMonth(DateTime.Now.Month);
            timers[0].LastExecution.Should().HaveDay(DateTime.Today.Day);
            timers[0].Enable.Should().BeTrue();
        }

        [Test]
        public void inti_should_removed_jobs_that_no_longer_exist()
        {
            IEnumerable<IJob> fakeJobs = new List<IJob> { fakeJob };
            Mocker.SetConstant(fakeJobs);

            WithRealDb();
            var deletedJob = Builder<JobDefinition>.CreateNew().Build();
            Db.Insert(deletedJob);
            var jobProvider = Mocker.Resolve<JobProvider>();

            var registeredJobs = Db.Fetch<JobDefinition>();
            registeredJobs.Should().HaveCount(1);
            registeredJobs.Should().NotContain(c => c.TypeName == deletedJob.TypeName);
        }

        [Test]
        public void inti_should_removed_jobs_that_no_longer_exist_even_with_same_name()
        {
            IEnumerable<IJob> fakeJobs = new List<IJob> { fakeJob };
            Mocker.SetConstant(fakeJobs);

            WithRealDb();
            var deletedJob = Builder<JobDefinition>.CreateNew()
                .With(c => c.Name = fakeJob.Name).Build();

            Db.Insert(deletedJob);
            var jobProvider = Mocker.Resolve<JobProvider>();

            var registeredJobs = Db.Fetch<JobDefinition>();
            registeredJobs.Should().HaveCount(1);
            registeredJobs.Should().NotContain(c => c.TypeName == deletedJob.TypeName);
        }

        [Test]
        public void init_should_update_existing_job()
        {
            IEnumerable<IJob> fakeJobs = new List<IJob> { fakeJob };
            Mocker.SetConstant(fakeJobs);

            WithRealDb();
            var initialFakeJob = Builder<JobDefinition>.CreateNew()
                .With(c => c.Name = "NewName")
                .With(c => c.TypeName = fakeJob.GetType().ToString())
                .With(c => c.Interval = 0)
                .With(c => c.Enable = false)
                .With(c => c.Success = true)
                .With(c => c.LastExecution = DateTime.Now.AddDays(-7).Date)
                .Build();

            var id = Convert.ToInt32(Db.Insert(initialFakeJob));

            Mocker.Resolve<JobProvider>();


            
            var registeredJobs = Db.Fetch<JobDefinition>();
            registeredJobs.Should().HaveCount(1);
            registeredJobs.First().TypeName.Should().Be(fakeJob.GetType().ToString());
            registeredJobs.First().Name.Should().Be(fakeJob.Name);
            registeredJobs.First().Interval.Should().Be((Int32)fakeJob.DefaultInterval.TotalMinutes);

            registeredJobs.First().Enable.Should().Be(true);
            registeredJobs.First().Success.Should().Be(initialFakeJob.Success);
            registeredJobs.First().LastExecution.Should().Be(initialFakeJob.LastExecution);
        }

        [Test]
        public void jobs_with_zero_interval_are_registered_as_disabled()
        {
            IEnumerable<IJob> fakeJobs = new List<IJob> { disabledJob };
            Mocker.SetConstant(fakeJobs);

            var jobProvider = Mocker.Resolve<JobProvider>();

            
            jobProvider.All().Should().HaveCount(1);
            jobProvider.All().First().Enable.Should().BeFalse();
        }

        [Test]
        public void disabled_jobs_arent_run_by_scheduler()
        {
            IEnumerable<IJob> BaseFakeJobs = new List<IJob> { disabledJob };
            Mocker.SetConstant(BaseFakeJobs);

            var jobProvider = Mocker.Resolve<JobProvider>();
            jobProvider.QueueScheduled();

            WaitForQueue();

            
            disabledJob.ExecutionCount.Should().Be(0);
        }

        [Test]
        public void job_with_specific_target_should_not_update_last_execution()
        {
            IEnumerable<IJob> BaseFakeJobs = new List<IJob> { fakeJob };
            Mocker.SetConstant(BaseFakeJobs);

            
            var jobProvider = Mocker.Resolve<JobProvider>();
            ResetLastExecution();
            jobProvider.QueueJob(typeof(FakeJob), 10);

            WaitForQueue();

            
            jobProvider.All().First().LastExecution.Should().HaveYear(2000);
        }

        [Test]
        public void job_with_specific_target_should_not_set_success_flag()
        {
            IEnumerable<IJob> BaseFakeJobs = new List<IJob> { fakeJob };
            Mocker.SetConstant(BaseFakeJobs);

            
            var jobProvider = Mocker.Resolve<JobProvider>();
            jobProvider.QueueJob(typeof(FakeJob), 10);

            WaitForQueue();

            
            jobProvider.All().First().Success.Should().BeFalse();
        }


        [Test]
        public void duplicated_queue_item_should_start_queue_if_its_not_running()
        {
            IEnumerable<IJob> BaseFakeJobs = new List<IJob> { fakeJob };
            Mocker.SetConstant(BaseFakeJobs);

            var stuckQueueItem = new JobQueueItem
                                    {
                                        JobType = fakeJob.GetType(),
                                        Options = new { TargetId = 12 }
                                    };

            
            var jobProvider = Mocker.Resolve<JobProvider>();
            jobProvider.Queue.Add(stuckQueueItem);

            WaitForQueue();
            jobProvider.QueueJob(stuckQueueItem.JobType, stuckQueueItem.Options);
            WaitForQueue();

            
            fakeJob.ExecutionCount.Should().Be(1);
        }


        [Test]
        public void Item_added_to_queue_while_scheduler_runs_should_be_executed()
        {
            IEnumerable<IJob> BaseFakeJobs = new List<IJob> { slowJob, disabledJob };
            Mocker.SetConstant(BaseFakeJobs);

            ResetLastExecution();
            var _jobThread = new Thread(Mocker.Resolve<JobProvider>().QueueScheduled);
            _jobThread.Start();

            Thread.Sleep(200);

            Mocker.Resolve<JobProvider>().QueueJob(typeof(DisabledJob), 12);

            WaitForQueue();

            
            slowJob.ExecutionCount.Should().Be(1);
            disabledJob.ExecutionCount.Should().Be(1);
        }

        [Test]
        public void trygin_to_queue_unregistered_job_should_fail()
        {
            IEnumerable<IJob> BaseFakeJobs = new List<IJob> { slowJob, disabledJob };
            Mocker.SetConstant(BaseFakeJobs);

            var jobProvider = Mocker.Resolve<JobProvider>();

            jobProvider.QueueJob(typeof(string));

            WaitForQueue();
            ExceptionVerification.ExpectedErrors(1);
        }

        [Test]
        public void scheduled_job_should_have_scheduler_as_source()
        {
            IEnumerable<IJob> BaseFakeJobs = new List<IJob> { slowJob, fakeJob};
            Mocker.SetConstant(BaseFakeJobs);

            var jobProvider = Mocker.Resolve<JobProvider>();
            ResetLastExecution();
            jobProvider.QueueScheduled();

            jobProvider.Queue.Should().OnlyContain(c => c.Source == JobQueueItem.JobSourceType.Scheduler);

            WaitForQueue();
        }
    }
}
