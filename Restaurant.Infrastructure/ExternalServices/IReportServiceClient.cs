using System.Threading.Tasks;

namespace Restaurant.Infrastructure.ExternalServices
{
    public interface IReportServiceClient
    {
        Task<HttpResponseMessage> SendReportEmailAsync(string baseUrl);
    }
}
