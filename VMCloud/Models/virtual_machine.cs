namespace VMCloud.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity;
    using System.Data.Entity.Spatial;

    [Table("fxcloud.virtual_machine")]
    public partial class VirtualMachine
    {
        [StringLength(200)]
        [Key]
        public string vm_name { get; set; }
        [StringLength(11)]
        public string user_id { get; set; }
        [StringLength(11)]
        public string owner_id { get; set; }

        public int? course_id { get; set; }
        public int? apply_id { get; set; }

        [StringLength(50)]
        public string uuid { get; set; }

        public int image { get; set; }


        [StringLength(50)]
        public string due_time { get; set; }

        public int warn_times { get; set; }

    }
    public class VMContext : DbContext
    {
        public VMContext()
            : base("name=DataModels")
        {
        }

        public virtual DbSet<VirtualMachine> VirtualMachines { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<VirtualMachine>().HasKey(vm => vm.vm_name);
        }
    }
}