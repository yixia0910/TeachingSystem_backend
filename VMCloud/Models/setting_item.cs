namespace VMCloud.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("fxcloud.setting_item")]
    public partial class Setting_item
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int id { get; set; }

        [StringLength(50)]
        public string name { get; set; }
        [StringLength(200)]
        public string desc { get; set; }

        [Column("default")]
        [StringLength(1000)]
        public string _default { get; set; }
    }
}
