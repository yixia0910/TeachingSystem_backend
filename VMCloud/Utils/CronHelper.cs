using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VMCloud.Utils
{
    public static class CronHelper
    {
        public static void AddTask<T>(string cronString) where T : IJob
        {
            ISchedulerFactory factory = new StdSchedulerFactory();
            IScheduler scheduler = factory.GetScheduler();
            scheduler.Start();

            IJobDetail job = JobBuilder.Create<T>().Build();

            ITrigger trigger = TriggerBuilder.Create()
                .WithCronSchedule(cronString)
                .Build();
            scheduler.ScheduleJob(job, trigger);
            scheduler.Start();
        }
    }
}