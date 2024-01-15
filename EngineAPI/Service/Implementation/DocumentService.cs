using EngineAPI.Domain.DataModels;
using EngineAPI.Domain.DTOs;
using EngineAPI.Domain.Misc;
using EngineAPI.Repository;
using EngineAPI.Service.Interface;
using Newtonsoft.Json;
using VDS.RDF.Parsing;
using VDS.RDF.Query.Datasets;
using VDS.RDF;
using VDS.RDF.Query;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using System.Text.RegularExpressions;
using Mapster;

namespace EngineAPI.Service.Implementation;

public class DocumentService : IDocumentService
{
    private readonly IRepository _repository;

    public DocumentService(IRepository repository)
    {
        _repository = repository;
    }
    public async Task<ResponseModel<dynamic>> DocumentAnnotator()
    {
        try
        {
            string jsonFilePath = "CrawledDocuments.json";

            if (!File.Exists(jsonFilePath)) return new ResponseModel<dynamic> { Success = false, Data = null };

            string jsonContent = File.ReadAllText(jsonFilePath);
            var jsonObj = JsonConvert.DeserializeObject<List<CrawledDocumentDTO>>(jsonContent);

            var concepts = await _repository.FindAll<Concept>(false);

            if (!concepts.Any() || jsonObj == null) return new ResponseModel<dynamic> { Success = false, Data = null };

            var graph = new Graph();
            FileLoader.Load(graph, "CulturalHeritage.rdf");
            var dataset = new InMemoryDataset(graph);

            int successCount = 0, resultCount = 0;
            List<IndexDTO> savedIndices = new();

            foreach (var obj in jsonObj)
            {
                var contentValues = obj.Content.Where(c => c.Length > 3).Select(c => $"\"{Functions.EscapeString(c)}\"");
                var contentValuesString = string.Join(" ", contentValues);

                var sparqlQuery = $@"
                PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#>
                PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>
                PREFIX owl: <http://www.w3.org/2002/07/owl#>
                PREFIX xsd: <http://www.w3.org/2001/XMLSchema#>
                PREFIX table: <http://cultural.heritage/Ethiopia#>

                SELECT ?content ?instanceLabel ?instanceType
                WHERE {{
                    VALUES ?content {{ {contentValuesString} }}
                    ?instance rdf:type ?instanceType .
                    ?instance rdfs:label ?instanceLabel .

                    FILTER(REGEX(LCASE(?instanceLabel), LCASE(?content), ""i""))
                    FILTER(?instanceType != owl:NamedIndividual)
                    FILTER(LCASE(?instanceLabel) = ?instanceLabel)  # Exclude uppercase labels
                }}
                GROUP BY ?content ?instanceLabel ?instanceType
            ";

                var query = new SparqlQueryParser().ParseFromString(sparqlQuery);
                var queryProcessor = new LeviathanQueryProcessor(dataset);
                var results = (SparqlResultSet)queryProcessor.ProcessQuery(query);

                if (results != null)
                {
                    var doc = new Document { Link = obj.Site, Title = obj.Title, Description = obj.Description };
                    var savedDoc = await _repository.Create(doc);
                    resultCount++;

                    var conceptFrequency = results.GroupBy(item => item["instanceType"].ToString())
                                                 .ToDictionary(group => group.Key, group => group.Count());
                   

                    var indices = results.Select(item =>
                    {
                        var concept = item["instanceType"].ToString();
                        var conceptId = concepts.FirstOrDefault(c => c.Concept1 == concept)?.Id ?? 0;
                        var tf = (double)conceptFrequency[concept] / conceptFrequency.Count;
                        return conceptId == 0 ? null : new Domain.DataModels.Index
                        {
                            Document = savedDoc.Id,
                            Concept = conceptId,
                            Tf = tf,
                            Instance = item["instanceLabel"].ToString(),
                            Keyword = item["content"].ToString()
                        };
                    }).Where(index => index != null).ToList();
                    savedIndices.AddRange((await _repository.BulkSave(indices)).Adapt<List<IndexDTO>>());
                    successCount += indices.Count;
                }
            }

            return new ResponseModel<dynamic> { Success = true, Data = savedIndices, Message = $"{successCount} indices out of {resultCount} documents saved." };
        }
        catch (Exception x)
        {
            return new ResponseModel<dynamic> { Success = false, Message = x.Message };
        }
    }


}
