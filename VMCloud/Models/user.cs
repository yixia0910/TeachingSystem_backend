namespace VMCloud.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity;
    using System.Data.Entity.Spatial;

    [Table("fxcloud.user")]
    public partial class User
    {
        [StringLength(11)]
        public string id { get; set; }

        [StringLength(100)]
        public string name { get; set; }

        [StringLength(50)]
        public string nick_name { get; set; }

        [Required]
        [StringLength(100)]
        public string passwd { get; set; }

        [StringLength(50)]
        public string email { get; set; }

        public int? role { get; set; }

        [StringLength(11)]
        public string department_id { get; set; }

        public bool? is_accept { get; set; }

        [StringLength(50)]
        public string accept_time { get; set; }
    }
}
