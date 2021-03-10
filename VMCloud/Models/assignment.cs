namespace VMCloud.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("fxcloud.assignment")]
    public partial class Assignment
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int id { get; set; }

        [StringLength(50)]
        public string student_id { get; set; }

        public int? experiment_id { get; set; }

        [StringLength(50)]
        public string submit_time { get; set; }

        [StringLength(50)]
        public string file { get; set; }

        public int is_standard { get; set; }

        public float? score { get; set; }
    }
}
