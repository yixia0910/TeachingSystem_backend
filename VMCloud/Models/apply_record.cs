namespace VMCloud.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("fxcloud.apply_record")]
    public partial class Apply_record
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }

        [StringLength(11)]
        public string sender_id { get; set; }

        [StringLength(50)]
        public string apply_time { get; set; }

        [StringLength(50)]
        public string finish_time { get; set; }
        
        [StringLength(50)]
        public string due_time { get; set; }

        public int? status { get; set; }

        [StringLength(50)]
        public string vm_name { get; set; }

        public int? operate_type { get; set; }

        [StringLength(1000)]
        public string detail { get; set; }

        [StringLength(1000)]
        public string apply_msg { get; set; }

        [StringLength(1000)]
        public string reply_msg { get; set; }

    }
}
