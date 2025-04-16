using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.DTOs.Feedbacks
{
    public class FeedbackDto
    {
        public required string Id { get; set; }
        public required string Rate { get; set; }
        public required string Comment { get; set; }
        public required string UserName { get; set; }
        public required string UserAvartarUrl { get; set; }
        public required string Date { get; set; }
        public required string Type { get; set; }
        public required string LocationId { get; set; }
    }
}
