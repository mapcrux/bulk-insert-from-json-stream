using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace TiCRateParser
{
    public interface IRateFileReader
    {
        Task<ReportingEntity> ReadFile(string path, ITargetBlock<Provider> providerTarget, ITargetBlock<Rate> rate);
    }

    public interface IRateUrlReader
    {
        //Task<ReportingEntity> DownloadFile(string path);
    }
}