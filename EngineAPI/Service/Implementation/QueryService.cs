using EngineAPI.Domain.DTOs;
using EngineAPI.Domain.Misc;
using EngineAPI.Repository;
using EngineAPI.Service.Interface;
using Microsoft.ML;
using VDS.RDF.Parsing;
using VDS.RDF.Query.Datasets;
using VDS.RDF;
using VDS.RDF.Query;

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
        

        if(textValue.Length > 2 && textValue is not null)
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

                SELECT ?concept
                WHERE {{
                  VALUES ?content {{ {contentValuesString} }}
                  ?instance rdf:type ?concept.
                  ?instance rdfs:label ?instanceLabel .
  
                  FILTER(REGEX(LCASE(?instanceLabel), LCASE(?content), ""i""))
                  FILTER(?concept != owl:NamedIndividual)
                  FILTER(LCASE(?instanceLabel) = ?instanceLabel)  # Exclude uppercase labels
                }}
                GROUP BY ?concept
                ";

            SparqlQueryParser sparqlParser = new();
            SparqlQuery query = sparqlParser.ParseFromString(sparqlQuery);

            LeviathanQueryProcessor queryProcessor = new(dataset);
            SparqlResultSet results = (SparqlResultSet)queryProcessor.ProcessQuery(query);

            HashSet<string> concepts = new();
            foreach (var result in results)
            {
                string concept = result["concept"].ToString();
                concepts.Add(concept);
            }

            var responses = await _repository.FindDocuments(concepts);

            return new ResponseModel<dynamic> { Success = true, Data = responses };
        }

        return new ResponseModel<dynamic> {Success = false, Message = "please add search text" };
    }
}
