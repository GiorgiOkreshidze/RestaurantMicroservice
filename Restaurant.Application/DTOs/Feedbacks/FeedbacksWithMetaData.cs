using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.DTOs.Feedbacks
{
    public class FeedbacksWithMetaData
    {
        public List<FeedbackDto> Content { get; set; } = [];

        public required FeedbacksSortMetaData Sort { get; set; }

        public string? Token { get; set; } = string.Empty;
    }
}
