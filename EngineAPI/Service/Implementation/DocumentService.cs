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

            if (File.Exists(jsonFilePath))
            {
                string jsonContent = File.ReadAllText(jsonFilePath);

                var jsonObj = JsonConvert.DeserializeObject<List<CrawledDocumentDTO>>(jsonContent);

                var concepts = await _repository.FindAll<Concept>(false);

                if (concepts.Any())
                {

                    if (jsonObj != null)
                    {
                        IGraph graph = new Graph();
                        FileLoader.Load(graph, "CulturalHeritage.rdf");
                        ISparqlDataset dataset = new InMemoryDataset(graph);

                        int successCount = 0, resultCount = 0;
                        List<IndexDTO> savedIndices = new();

                        foreach (var obj in jsonObj)
                        {
                            List<string> contentValues = new();
                            foreach (var c in obj.Content)
                            {
                                if (c.Length < 3)
                                    continue;

                                // Escape and add each content value to the list
                                contentValues.Add($"\"{EscapeString(c)}\"");
                            }
                            // Join the content values into a string
                            string contentValuesString = string.Join(" ", contentValues);

                            string sparqlQuery = $@"
                            PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#>
                            PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>
                            PREFIX owl: <http://www.w3.org/2002/07/owl#>
                            PREFIX xsd: <http://www.w3.org/2001/XMLSchema#>
                            PREFIX table: <http://cultural.heritage/Ethiopia#>

                            SELECT ?content ?instanceLabel ?instanceType (xsd:integer(COUNT(?instanceLabel)) AS ?conceptCount)
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


                            SparqlQueryParser sparqlParser = new SparqlQueryParser();
                            SparqlQuery query = sparqlParser.ParseFromString(sparqlQuery);

                            LeviathanQueryProcessor queryProcessor = new LeviathanQueryProcessor(dataset);
                            SparqlResultSet results = (SparqlResultSet)queryProcessor.ProcessQuery(query);

                            if (results != null)
                            {
                                var doc = new Document
                                {
                                    Link = obj.Site,
                                    Title = obj.Title,
                                    Description = obj.Description,
                                };

                                var savedDoc = await _repository.Create(doc);
                                resultCount++;

                                List<Domain.DataModels.Index> indices = new();
                                foreach (var item in results)
                                {

                                    var conceptId = concepts
                                        .Where(c => c.Concept1 == item["instanceType"].ToString())
                                        .Select(c => c.Id)
                                        .FirstOrDefault();

                                    if (conceptId == 0)
                                        continue;

                                    string conceptCountLiteral = item["conceptCount"].ToString();
                                    Match match = Regex.Match(conceptCountLiteral, @"(\d+)");
                                    int conceptCount = 0;
                                    if (match.Success)
                                    {
                                        string numericValue = match.Groups[1].Value;

                                        conceptCount = Convert.ToInt32(numericValue);

                                    }
                                    var conceptIndex = new Domain.DataModels.Index
                                    {
                                        Document = savedDoc.Id,
                                        Concept = conceptId,
                                        ConceptWeight = conceptCount,
                                        Instance = item["instanceLabel"].ToString(),
                                        Keyword = item["content"].ToString()
                                    };

                                    indices.Add(conceptIndex);
                                    successCount++;
                                }

                                var returnedObj = await _repository.BulkSave(indices);
                                
                                savedIndices.AddRange(returnedObj.Adapt<List<IndexDTO>>());
                            }
                        }

                        return new ResponseModel<dynamic> { Success = true, Data = savedIndices, Message = $"{successCount} indices out of {resultCount} documents saved." };
                    }
                }


            }

            return new ResponseModel<dynamic> { Success = false, Data = null };
        }
        catch (Exception x)
        {
            return new ResponseModel<dynamic> { Success = false, Message = x.Message };
        }
    }

    // Function to escape special characters in SPARQL values
    private string EscapeString(string value)
    {
        // Implement your logic to escape special characters if needed
        return value.Replace("\"", "\\\"");
    }
}
