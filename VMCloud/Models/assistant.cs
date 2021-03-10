namespace VMCloud.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity;
    using System.Data.Entity.Spatial;

    [Table("fxcloud.assistant")]
    public partial class Assistant
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int id { get; set; }

        [StringLength(11)]
        public string student_id { get; set; }

        public int course_id { get; set; }

        [StringLength(50)]
        public string create_time { get; set; }
    }
}
