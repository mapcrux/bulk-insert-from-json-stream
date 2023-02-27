using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace TiCRateParser
{
    public interface IRateReader
    {
        Task<JsonNode> ReadFile(string path);
    }
}