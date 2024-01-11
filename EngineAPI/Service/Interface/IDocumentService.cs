using EngineAPI.Domain.Misc;

namespace EngineAPI.Service.Interface;

public interface IDocumentService
{
    Task<ResponseModel<dynamic>> DocumentAnnotator();
}
