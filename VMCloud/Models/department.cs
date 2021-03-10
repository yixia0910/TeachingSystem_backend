namespace VMCloud.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("fxcloud.department")]
    public partial class Department
    {
        [StringLength(11)]
        public string id { get; set; }

        [StringLength(50)]
        public string name { get; set; }
    }
}
