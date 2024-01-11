using EngineAPI.Domain.DataModels;
using EngineAPI.Domain.DTOs;
using EngineAPI.Domain.Misc;
using EngineAPI.Repository;
using EngineAPI.Service.Interface;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Query.Datasets;

namespace EngineAPI.Service.Implementationl;

public class ConceptService : IConceptService
{
    private readonly IRepository _repository;

    public ConceptService(IRepository repository)
    {
        _repository = repository;
    }

    public async Task<ResponseModel<dynamic>> AddConcept()
    {
        try
        {
            IGraph graph = new Graph();
            FileLoader.Load(graph, "CulturalHeritage.rdf");

            ISparqlDataset dataset = new InMemoryDataset(graph);

            string sparqlQuery = @"
            PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#>
            PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>
            PREFIX owl: <http://www.w3.org/2002/07/owl#>
            PREFIX table: <http://cultural.heritage/Ethiopia#>

            SELECT ?class ?subclass ?parentClass WHERE {
                ?class rdf:type owl:Class .
                OPTIONAL {
                    ?subclass rdfs:subClassOf ?class .
                }
                OPTIONAL {
                    ?class rdfs:subClassOf ?parentClass .
                }
            }";

            SparqlQueryParser sparqlParser = new SparqlQueryParser();
            SparqlQuery query = sparqlParser.ParseFromString(sparqlQuery);

            LeviathanQueryProcessor queryProcessor = new LeviathanQueryProcessor(dataset);
            SparqlResultSet results = (SparqlResultSet)queryProcessor.ProcessQuery(query);

            if(results != null)
            {
                List<Concept> concepts = new();
                foreach (var result in results)
                {
                    var c = new Concept()
                    {
                        Concept1 = result["class"].ToString(),
                        ChildConcept = result.HasBoundValue("subclass") ? result["subclass"].ToString() : null,
                        ParentConcept = result.HasBoundValue("parentClass") ? result["parentClass"].ToString() : null
                    };


                    concepts.Add(c);
                }

                var returnedObj = await _repository.BulkSave(concepts);

                return new ResponseModel<dynamic> { Success = true, Data = returnedObj };
            }

            return new ResponseModel<dynamic> { Success = false, Data = null };

        }
        catch (Exception x)
        {
            return new ResponseModel<dynamic> { Success = false, Message = x.Message };
        }
    }
}
