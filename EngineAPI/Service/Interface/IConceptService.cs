using EngineAPI.Domain.DTOs;
using EngineAPI.Domain.Misc;
using VDS.RDF.Parsing.Tokens;

namespace EngineAPI.Service.Interface;

public interface IConceptService
{
    Task<ResponseModel<dynamic>> AddConcept();
}
