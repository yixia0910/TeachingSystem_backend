namespace VMCloud.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity;
    using System.Data.Entity.Spatial;

    [Table("fxcloud.file")]
    public partial class File
    {
        [Key]
        [StringLength(50)]
        public string id { get; set; }
        [StringLength(100)]
        public string name { get; set; }
        [StringLength(20)]
        public string upload_time { get; set; }
        [StringLength(100)]
        public string preview { get; set; }
        [StringLength(20)]
        public string type { get; set; }
        [StringLength(20)]
        public string size { get; set; }
        [StringLength(20)]
        public string uploader { get; set; }
        [StringLength(200)]
        public string path { get; set; }
    }
}
