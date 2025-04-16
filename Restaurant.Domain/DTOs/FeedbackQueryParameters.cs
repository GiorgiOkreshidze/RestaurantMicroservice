using Restaurant.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.DTOs.Feedbacks
{
    public class FeedbackQueryParameters
    {
        public string? Type { get; set; }
        public FeedbackType? EnumType { get; set; }
        public int Page { get; set; } = 0;
        public int PageSize { get; set; } = 20;
        public string? NextPageToken { get; set; }
        public string? Sort { get; set; } // Format: "property,direction"

        // Properties to hold parsed values from Sort
        public string SortProperty { get; set; } = "date";
        public string SortDirection { get; set; } = "asc";
    }
}
