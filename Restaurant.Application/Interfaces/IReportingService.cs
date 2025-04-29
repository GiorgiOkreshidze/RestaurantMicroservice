using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Restaurant.Application.Interfaces
{
    public interface IReportingService
    {
        Task SendReportEmailAsync();
    }
}
