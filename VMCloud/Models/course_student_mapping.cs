namespace VMCloud.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("fxcloud.course_student_mapping")]
    public partial class Course_student_mapping
    {
        [Key]
        [Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int course_id { get; set; }

        [Key]
        [Column(Order = 1)]
        [StringLength(11)]
        public string student_id { get; set; }
    }
}
