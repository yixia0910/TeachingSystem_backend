namespace VMCloud.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("fxcloud.user_setting_item_mapping")]
    public partial class User_setting_item_mapping
    {
        [Key]
        [Column(Order = 0)]
        [StringLength(11)]
        public string user_id { get; set; }

        [Key]
        [Column(Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int setting_item_id { get; set; }

        [StringLength(50)]
        public string value { get; set; }
    }
}
