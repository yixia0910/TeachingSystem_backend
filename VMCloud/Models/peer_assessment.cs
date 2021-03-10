namespace VMCloud.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("fxcloud.peer_assessment")]
    public partial class Peer_assessment
    {
        [Key]
        [Column(Order = 0)]
        [StringLength(11)]
        public string student_id { get; set; }

        [Key]
        [Column(Order = 1)]
        [StringLength(11)]
        public string assessor_id { get; set; }

        [Key]
        [Column(Order = 2)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int experiment_id { get; set; }

        public float? score { get; set; }

        [StringLength(1000)]
        public string reason { get; set; }

        public int? appeal_status { get; set; }

        [StringLength(1000)]
        public string appeal_reason { get; set; }

        public float? origin_score { get; set; }
    }
}
