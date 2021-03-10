/*
*	Author:       Xing Zhihuan
*   Email:        1@roycent.cn	
*	Created:      2019/7/11 17:09:40
*   Description:  
 */
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace VMCloud.Models
{
    public class Logger : DbContext
    {
        public Logger()
            : base("name=DataModels")
        {
        }

        public virtual DbSet<System_log> Logs { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<System_log>().HasKey(log => log.id);
        }
    }
}