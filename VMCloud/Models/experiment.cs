namespace VMCloud.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("fxcloud.experiment")]
    public partial class Experiment
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity), Key]
        public int id { get; set; }

        public int? course_id { get; set; }

        [StringLength(50)]
        public string name { get; set; }

        public bool? type { get; set; }

        [StringLength(1000)]
        public string detail { get; set; }

        [StringLength(100)]
        public string resource { get; set; }

        [StringLength(50)]
        public string create_time { get; set; }

        [StringLength(50)]
        public string start_time { get; set; }

        [StringLength(50)]
        public string end_time { get; set; }

        [StringLength(50)]
        public string deadline { get; set; }

        public int? vm_status { get; set; }

        [StringLength(50)]
        public string vm_name { get; set; }

        public int? vm_apply_id { get; set; }

        [StringLength(50)]
        public string vm_passwd { get; set; }

        public bool? is_peer_assessment { get; set; }

        [StringLength(50)]
        public string peer_assessment_deadline { get; set; }

        [StringLength(50)]
        public string appeal_deadline { get; set; }

        [StringLength(1000)]
        public string peer_assessment_rules { get; set; }

        public bool? peer_assessment_start { get; set; }

        public int sent_email { get; set; }
    }
}
