using EngineAPI.Domain.DTOs;
using EngineAPI.Domain.Misc;
using EngineAPI.Repository;
using EngineAPI.Service.Interface;
using Microsoft.ML;
using VDS.RDF.Parsing;
using VDS.RDF.Query.Datasets;
using VDS.RDF;
using VDS.RDF.Query;
using EngineAPI.Domain.DataModels;

namespace EngineAPI.Service.Implementation;

public class QueryService : IQueryService
{
    private readonly IRepository _repository;

    public QueryService(IRepository repository)
    {
        _repository = repository;
    }
    public async Task<ResponseModel<dynamic>> QueryAnnotator(Dictionary<string, string> queryParam)
    {
        if (!queryParam.TryGetValue("query", out string textValue))
            return new ResponseModel<dynamic> { Success = false, Message = "The 'query' key is missing in the queryParams." };
        

        if(textValue is not null && textValue.Length > 3)
        {
            var context = new MLContext();
            var emptyData = new List<TextData>();
            var data = context.Data.LoadFromEnumerable(emptyData);

            var tokenization = context.Transforms.Text.TokenizeIntoWords("Tokens", "Text", separators: new[] { ' ', '.', ',' })
                .Append(context.Transforms.Text.RemoveDefaultStopWords("Tokens", "Tokens",
                    Microsoft.ML.Transforms.Text.StopWordsRemovingEstimator.Language.English));

            var tokenModel = tokenization.Fit(data);
            var engine = context.Model.CreatePredictionEngine<TextData, TextTokens>(tokenModel);
            var tokens = engine.Predict(new TextData { Text = textValue });

            List<string> contentValues = new();
            foreach (var token in tokens.Tokens)
            {
                if (token.Length < 3)
                    continue;

                // Escape and add each content value to the list
                contentValues.Add($"\"{Functions.EscapeString(token)}\"");
            }

            string contentValuesString = string.Join(" ", contentValues);

            IGraph graph = new Graph();
            FileLoader.Load(graph, "CulturalHeritage.rdf");
            ISparqlDataset dataset = new InMemoryDataset(graph);

            string sparqlQuery = $@"
                PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#>
                PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>
                PREFIX owl: <http://www.w3.org/2002/07/owl#>
                PREFIX xsd: <http://www.w3.org/2001/XMLSchema#>
                PREFIX table: <http://cultural.heritage/Ethiopia#>

                SELECT ?concept ?instanceLabel ?similarInstanceLabel ?subPartInstanceLabel ?mainPartInstanceLabel
                WHERE {{
                  VALUES ?content {{ {contentValuesString} }}
                  ?instance rdf:type ?concept.
                  ?instance rdfs:label ?instanceLabel .
                    
                  OPTIONAL {{
                    # Similar instances connected by table:similar_to
                    ?similarInstance table:similar_to ?instance.
                    ?similarInstance rdfs:label ?similarInstanceLabel.
                  }}
                  OPTIONAL {{
                    # Instances that are part of ?instance
                    ?subPartInstance table:is_part_of ?instance.
                    ?subPartInstance rdfs:label ?subPartInstanceLabel.
                  }}

                  OPTIONAL {{
                    # Instances where ?instance is part of
                    ?instance table:is_part_of ?mainPartInstance.
                    ?mainPartInstance rdfs:label ?mainPartInstanceLabel.
                  }}
  
                  FILTER(REGEX(LCASE(?instanceLabel), LCASE(?content), ""i""))
                  FILTER(?concept != owl:NamedIndividual)
                  FILTER(LCASE(?instanceLabel) = ?instanceLabel)  # Exclude uppercase labels
                }}
                GROUP BY ?concept ?instanceLabel ?similarInstanceLabel ?subPartInstanceLabel ?mainPartInstanceLabel
                ";

            SparqlQueryParser sparqlParser = new();
            SparqlQuery query = sparqlParser.ParseFromString(sparqlQuery);

            LeviathanQueryProcessor queryProcessor = new(dataset);
            SparqlResultSet results = (SparqlResultSet)queryProcessor.ProcessQuery(query);

            if(results is not null)
            {
                var filterParam = "http://cultural.heritage/Ethiopia#Geographical_area";

                HashSet<string> concepts = new(results.Select(result => result["concept"].ToString()));
                HashSet<string> instances = new(results.Select(result => Functions.CleanUpString(result["instanceLabel"].ToString())));
                instances.UnionWith(results
                            .SelectMany(result => new[]
                            {
                                result.HasBoundValue("similarInstanceLabel") ? Functions.CleanUpString(result["similarInstanceLabel"]?.ToString()) : null,
                                result.HasBoundValue("subPartInstanceLabel") ? Functions.CleanUpString(result["subPartInstanceLabel"]?.ToString()) : null,
                                result.HasBoundValue("mainPartInstanceLabel") ? Functions.CleanUpString(result["mainPartInstanceLabel"]?.ToString()) : null
                            })
                            .Where(label => label != null));


                var responses = await _repository.FindDocuments(concepts, instances, filterParam);

                foreach (var response in responses)
                {
                    if (instances.Any(instance => response.Title.ToLower().Contains(instance.ToLower())))
                    {
                        response.Tf += 10;
                    }
                }


                return new ResponseModel<dynamic> { Success = true, Data = responses.OrderByDescending(r => r.Tf) };
            }

            
        }

        return new ResponseModel<dynamic> {Success = false, Message = "please add search text" };
    }
}
