namespace VMCloud.Models
{
    using System;
    using System.Data.Entity;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    public partial class DataModels : DbContext
    {
        public DataModels()
            : base("name=DataModels")
        {
        }

        public virtual DbSet<Apply_record> Apply_record { get; set; }
        public virtual DbSet<Assignment> Assignments { get; set; }
        public virtual DbSet<Assistant> Assistants { get; set; }
        public virtual DbSet<Course> Courses { get; set; }
        public virtual DbSet<Course_student_mapping> course_student_mapping { get; set; }
        public virtual DbSet<Department> Departments { get; set; }
        public virtual DbSet<Experiment> Experiments { get; set; }
        public virtual DbSet<Peer_assessment> Peer_assessment { get; set; }
        public virtual DbSet<Setting_item> Setting_item { get; set; }
        public virtual DbSet<Term> Terms { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<User_setting_item_mapping> User_setting_item_mapping { get; set; }
        public virtual DbSet<VirtualMachine> VirtualMachines { get; set; }
        public virtual DbSet<SangforInfo> SangforInfos { get; set; }
        public virtual DbSet<File> Files { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Apply_record>()
                .Property(e => e.sender_id)
                .IsUnicode(false);

            modelBuilder.Entity<Apply_record>()
                .Property(e => e.apply_time)
                .IsUnicode(false);

            modelBuilder.Entity<Apply_record>()
                .Property(e => e.finish_time)
                .IsUnicode(false);

            modelBuilder.Entity<Apply_record>()
                .Property(e => e.vm_name)
                .IsUnicode(false);

            modelBuilder.Entity<Apply_record>()
                .Property(e => e.detail)
                .IsUnicode(false);

            modelBuilder.Entity<Apply_record>()
                .Property(e => e.apply_msg)
                .IsUnicode(false);

            modelBuilder.Entity<Apply_record>()
                .Property(e => e.reply_msg)
                .IsUnicode(false);

           

            modelBuilder.Entity<Assignment>()
                .Property(e => e.student_id)
                .IsUnicode(false);

            modelBuilder.Entity<Assignment>()
                .Property(e => e.submit_time)
                .IsUnicode(false);

            modelBuilder.Entity<Assignment>()
                .Property(e => e.file)
                .IsUnicode(false);

            modelBuilder.Entity<Assistant>()
                .Property(e => e.student_id)
                .IsUnicode(false);

            modelBuilder.Entity<Assistant>()
                .Property(e => e.course_id);

            modelBuilder.Entity<Assistant>()
                .Property(e => e.create_time)
                .IsUnicode(false);

            modelBuilder.Entity<Course>()
                .Property(e => e.teacher_id)
                .IsUnicode(false);

            modelBuilder.Entity<Course>()
                .Property(e => e.name)
                .IsUnicode(false);

            modelBuilder.Entity<Course>()
                .Property(e => e.create_time)
                .IsUnicode(false);

            modelBuilder.Entity<Course>()
                .Property(e => e.department_id)
                .IsUnicode(false);

            modelBuilder.Entity<Course>()
                .Property(e => e.resource_folder)
                .IsUnicode(false);

            modelBuilder.Entity<Course_student_mapping>()
                .Property(e => e.student_id)
                .IsUnicode(false);

            modelBuilder.Entity<Department>()
                .Property(e => e.id)
                .IsUnicode(false);

            modelBuilder.Entity<Department>()
                .Property(e => e.name)
                .IsUnicode(false);

            modelBuilder.Entity<Experiment>()
                .Property(e => e.name)
                .IsUnicode(false);

            modelBuilder.Entity<Experiment>()
                .Property(e => e.detail)
                .IsUnicode(false);

            modelBuilder.Entity<Experiment>()
                .Property(e => e.resource)
                .IsUnicode(false);

            modelBuilder.Entity<Experiment>()
                .Property(e => e.create_time)
                .IsUnicode(false);

            modelBuilder.Entity<Experiment>()
                .Property(e => e.start_time)
                .IsUnicode(false);

            modelBuilder.Entity<Experiment>()
                .Property(e => e.end_time)
                .IsUnicode(false);

            modelBuilder.Entity<Experiment>()
                .Property(e => e.deadline)
                .IsUnicode(false);

            modelBuilder.Entity<Experiment>()
                .Property(e => e.vm_name)
                .IsUnicode(false);

            modelBuilder.Entity<Experiment>()
                .Property(e => e.vm_passwd)
                .IsUnicode(false);

            modelBuilder.Entity<Experiment>()
                .Property(e => e.peer_assessment_deadline)
                .IsUnicode(false);

            modelBuilder.Entity<Experiment>()
                .Property(e => e.appeal_deadline)
                .IsUnicode(false);

            modelBuilder.Entity<Experiment>()
                .Property(e => e.peer_assessment_rules)
                .IsUnicode(false);

            modelBuilder.Entity<Peer_assessment>()
                .Property(e => e.student_id)
                .IsUnicode(false);

            modelBuilder.Entity<Peer_assessment>()
                .Property(e => e.assessor_id)
                .IsUnicode(false);

            modelBuilder.Entity<Peer_assessment>()
                .Property(e => e.reason)
                .IsUnicode(false);

            modelBuilder.Entity<Peer_assessment>()
                .Property(e => e.appeal_reason)
                .IsUnicode(false);

            modelBuilder.Entity<Setting_item>()
                .Property(e => e.name)
                .IsUnicode(false);

            modelBuilder.Entity<Setting_item>()
                .Property(e => e._default)
                .IsUnicode(false);

            modelBuilder.Entity<Term>()
                .Property(e => e.name)
                .IsUnicode(false);

            modelBuilder.Entity<User>()
                .Property(e => e.id)
                .IsUnicode(false);

            modelBuilder.Entity<User>()
                .Property(e => e.name)
                .IsUnicode(false);

            modelBuilder.Entity<User>()
                .Property(e => e.nick_name)
                .IsUnicode(false);

            modelBuilder.Entity<User>()
                .Property(e => e.passwd)
                .IsUnicode(false);

            modelBuilder.Entity<User>()
                .Property(e => e.email)
                .IsUnicode(false);

            modelBuilder.Entity<User>()
                .Property(e => e.department_id)
                .IsUnicode(false);

            modelBuilder.Entity<User>()
                .Property(e => e.accept_time)
                .IsUnicode(false);

            modelBuilder.Entity<User_setting_item_mapping>()
                .Property(e => e.user_id)
                .IsUnicode(false);

            modelBuilder.Entity<User_setting_item_mapping>()
                .Property(e => e.value)
                .IsUnicode(false);

            modelBuilder.Entity<VirtualMachine>()
                .Property(e => e.vm_name)
                .IsUnicode(false);

            modelBuilder.Entity<VirtualMachine>()
                .Property(e => e.user_id)
                .IsUnicode(false);

            modelBuilder.Entity<VirtualMachine>()
                .Property(e => e.owner_id)
                .IsUnicode(false);

            modelBuilder.Entity<VirtualMachine>()
                .Property(e => e.uuid)
                .IsUnicode(false);


            modelBuilder.Entity<VirtualMachine>()
                .Property(e => e.due_time)
                .IsUnicode(false);


            modelBuilder.Entity<File>()
                .Property(e => e.id)
                .IsUnicode(false);

            modelBuilder.Entity<File>()
                .Property(e => e.name)
                .IsUnicode(false);

            modelBuilder.Entity<File>()
                .Property(e => e.upload_time)
                .IsUnicode(false);

            modelBuilder.Entity<File>()
                .Property(e => e.preview)
                .IsUnicode(false);

            modelBuilder.Entity<File>()
                .Property(e => e.type)
                .IsUnicode(false);

            modelBuilder.Entity<File>()
                .Property(e => e.size)
                .IsUnicode(false);


            modelBuilder.Entity<File>()
                .Property(e => e.uploader)
                .IsUnicode(false);

            modelBuilder.Entity<File>()
                .Property(e => e.path)
                .IsUnicode(false);

            modelBuilder.Entity<SangforInfo>()
                .Property(e => e.id)
                .IsUnicode(false);

            modelBuilder.Entity<SangforInfo>()
                .Property(e => e.Name)
                .IsUnicode(false);

            modelBuilder.Entity<SangforInfo>()
                .Property(e => e.IsTemplate)
                ;

            modelBuilder.Entity<SangforInfo>()
                .Property(e => e.TemplateName)
                .IsUnicode(false);

            modelBuilder.Entity<SangforInfo>()
                .Property(e => e.TemplateId)
                .IsUnicode(false);

            modelBuilder.Entity<SangforInfo>()
                .Property(e => e.student_id)
                .IsUnicode(false);

            modelBuilder.Entity<SangforInfo>()
                .Property(e => e.admin_id)
                .IsUnicode(false);

            modelBuilder.Entity<SangforInfo>()
                .Property(e => e.teacher_id)
                .IsUnicode(false);

            modelBuilder.Entity<SangforInfo>()
                .Property(e => e.is_exps)
                .IsUnicode(false);

            modelBuilder.Entity<SangforInfo>()
                .Property(e => e.is_exp)
                ;

            modelBuilder.Entity<SangforInfo>()
                .Property(e => e.image_disk)
                ;
        }
    }
}
