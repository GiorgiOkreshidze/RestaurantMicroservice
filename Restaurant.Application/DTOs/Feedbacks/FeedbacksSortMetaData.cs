using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.DTOs.Feedbacks
{
    public class FeedbacksSortMetaData
    {
        public required string Direction { get; set; }
        public required string NullHandling { get; set; }
        public bool Ascending { get; set; }
        public required string Property { get; set; }
        public bool IgnoreCase { get; set; }
    }
}
