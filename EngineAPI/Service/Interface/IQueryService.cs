using EngineAPI.Domain.Misc;

namespace EngineAPI.Service.Interface;

public interface IQueryService
{
    Task<ResponseModel<dynamic>> QueryAnnotator(Dictionary<string, string> queryParam);
}
