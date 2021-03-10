namespace VMCloud.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity;
    using System.Data.Entity.Spatial;

    [Table("fxcloud.course")]
    public partial class Course
    {
        public int id { get; set; }

        [StringLength(11)]
        public string teacher_id { get; set; }

        [StringLength(50)]
        public string name { get; set; }

        public int? term_id { get; set; }

        [StringLength(50)]
        public string create_time { get; set; }

        [StringLength(50)]
        public string department_id { get; set; }

        [StringLength(1000)]
        public string resource_folder { get; set; }

    }
}
