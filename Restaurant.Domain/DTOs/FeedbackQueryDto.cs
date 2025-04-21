using Restaurant.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Domain.DTOs
{
    public class FeedbackQueryDto
    {
        public string? Type { get; set; }
        public int Page { get; set; } = 0;
        public int PageSize { get; set; } = 20;
        public string? NextPageToken { get; set; } // Canditate to be deprecated
        public string? Sort { get; set; } // Format: "property,direction"
    }
}
