namespace VMCloud.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("fxcloud.system_log")]
    public partial class System_log
    {
        public int id { get; set; }

        [StringLength(11)]
        public string operate_target_type { get; set; }

        [StringLength(50)]
        public string operate_target_id { get; set; }

        [StringLength(11)]
        public string operator_id { get; set; }

        public int? operator_role { get; set; }

        [StringLength(50)]
        public string time { get; set; }

        [StringLength(500)]
        public string state { get; set; }

        [StringLength(5000)]
        public string content { get; set; }

        [StringLength(50)]
        public string complete_time { get; set; }

        [StringLength(50)]
        public string user_ip { get; set; }
    }
}
