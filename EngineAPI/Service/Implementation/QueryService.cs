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
using Newtonsoft.Json.Linq;
using Microsoft.Data.SqlClient.DataClassification;
using Microsoft.AspNetCore.Http;
using VDS.RDF.Parsing.Tokens;

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
        try
        {
            if (!queryParam.TryGetValue("query", out string textValue))
                return new ResponseModel<dynamic> { Success = false, Message = "The 'query' key is missing in the queryParams." };


            if (textValue is not null && textValue.Length > 2)
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

                if (!tokens.Tokens.Any())
                    return new ResponseModel<dynamic> { Success = false, Message = "Please enter correct words." };

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

                    SELECT ?concept ?instanceLabel ?similarInstanceLabel ?subPartInstanceLabel ?mainPartInstanceLabel ?csinstanceLabel ?domainInstanceLabel ?tempInstanceLabel ?altInstanceLabel
					       ?typeInstanceLabel ?regionInstaceLabel ?founderInstanceLabel ?mainPartConcept ?csconcept ?domainConcept ?tempConcept ?altConcept ?typeConcept ?regionConcept ?founderConcept 
					       ?founderInstaSimLabel ?regionInstaSimLabel ?typeInstaSimLabel ?altInstaSimLabel ?tempInstaSimLabel ?domainInstaSimLabel ?csinstasimlabel
                    WHERE {{{{
                      VALUES ?content {{{ contentValuesString }}}
                      ?instance rdf:type ?concept.
                      ?instance rdfs:label ?instanceLabel .
                    
                      OPTIONAL {{{{
                        # Similar instances connected by table:similar_to
                        ?similarInstance table:similar_to ?instance.
                        ?similarInstance rdfs:label ?similarInstanceLabel.
                      }}}}
                      OPTIONAL {{{{
                        # Instances that are part of ?instance
                        ?subPartInstance table:is_part_of ?instance.
                        ?subPartInstance rdfs:label ?subPartInstanceLabel.
                      }}}}

                      OPTIONAL {{{{
                        # Instances where ?instance is part of
                        ?instance table:is_part_of ?mainPartInstance.
                        ?mainPartInstance rdfs:label ?mainPartInstanceLabel.
        			    ?mainPartInstance rdf:type ?mainPartConcept.
                      }}}}
    			
    			     OPTIONAL {{{{
                        ?csinstance table:current_status ?instance.
                        ?csinstance rdfs:label ?csinstanceLabel.
        			    ?csinstance rdf:type ?csconcept.
        			    optional{{{{
            			    ?csinstance table:similar_to ?csinstasim.
        				    ?csinstasim rdfs:label ?csinstasimlabel.
          			    }}}}
        			
                      }}}}
    
    			     Optional{{{{
    				    ?domainInstance table:has_domain ?instance.
        			    ?domainInstance rdfs:label ?domainInstanceLabel.
        			    ?domainInstance rdf:type ?domainConcept.
        			    optional{{{{
            			    ?domainInstance table:similar_to ?domainInstaSim.
        				    ?domainInstaSim rdfs:label ?domainInstaSimLabel.
          			    }}}}
        			
      			     }}}}
    
    			     Optional{{{{
                        ?tempInstance table:has_temperature ?instance.
                        ?tempInstance rdfs:label ?tempInstanceLabel.
        			    ?tempInstance rdf:type ?tempConcept.
        			    optional {{{{
            			    ?tempInstance table:similar_to ?tempInstaSim.
        				    ?tempInstaSim rdfs:label ?tempInstaSimLabel.
          			    }}}}
        			
      			     }}}}
    
    			     Optional{{{{
        			    ?altInstance table:has_altitude ?instance.
        			    ?altInstance rdfs:label ?altInstanceLabel.
        			    ?altInstance rdf:type ?altConcept.
        			    optional{{{{
            			    ?altInstance table:similar_to ?altInstaSim.
        				    ?altInstaSim rdfs:label ?altInstaSimLabel.
          			    }}}}
        			
      			     }}}}
    
    			     Optional{{{{
        			    ?typeInstance table:type_of ?instance.
        			    ?typeInstance rdfs:label ?typeInstanceLabel.
        			    ?typeInstance rdf:type ?typeConcept.
        			    optional {{{{
            			    ?typeInstance table:similar_to ?typeInstaSim.
        				    ?typeInstaSim rdfs:label ?typeInstaSimLabel.
          			    }}}}
        			
      			     }}}}
    			
    			     Optional{{{{
        			    ?regionInstance table:region ?instance.
        			    ?regionInstance rdfs:label ?regionInstaceLabel.
        			    ?regionInstance rdf:type ?regionConcept.
        			    optional{{{{
            			    ?regionInstance table:similar_to ?regionInstaSim.
        				    ?regionInstaSim rdfs:label ?regionInstaSimLabel.
          			    }}}}
        			
      			     }}}}
    
    			     Optional {{{{
        			    ?founderInstance table:has_founder ?instance.
        			    ?founderInstance rdfs:label ?founderInstanceLabel.
        			    ?founderInstance rdf:type ?founderConcept.
        			    optional{{{{
            			    ?founderInstance table:similar_to ?founderInstaSim.
        				    ?founderInstaSim rdfs:label ?founderInstaSimLabel.
          			    }}}}
        			
      			     }}}}
                      
                      FILTER(!BOUND(?mainPartConcept) || ?mainPartConcept != owl:NamedIndividual)
                      FILTER(REGEX(LCASE(?instanceLabel), LCASE(?content), ""i""))
                      FILTER(?concept != owl:NamedIndividual)
    			      FILTER(!BOUND(?csconcept) || ?csconcept != owl:NamedIndividual)
    			      FILTER(!BOUND(?mainPartConcept) || ?mainPartConcept != owl:NamedIndividual)
    			      FILTER(!BOUND(?domainConcept) || ?domainConcept != owl:NamedIndividual)
    			      FILTER(!BOUND(?tempConcept) || ?tempConcept != owl:NamedIndividual)
    			      FILTER(!BOUND(?altConcept) || ?altConcept != owl:NamedIndividual)
    			      FILTER(!BOUND(?typeConcept) || ?typeConcept != owl:NamedIndividual)
    			      FILTER(!BOUND(?regionConcept) || ?regionConcept != owl:NamedIndividual)
    			      FILTER(!BOUND(?founderConcept) || ?founderConcept != owl:NamedIndividual)
                      FILTER(LCASE(?instanceLabel) = ?instanceLabel)  # Exclude uppercase labels

                    }}}}
                    GROUP BY ?concept ?instanceLabel ?similarInstanceLabel ?subPartInstanceLabel ?mainPartInstanceLabel ?csinstanceLabel ?domainInstanceLabel ?tempInstanceLabel ?altInstanceLabel
						     ?typeInstanceLabel ?regionInstaceLabel ?founderInstanceLabel ?csconcept ?mainPartConcept ?domainConcept ?tempConcept ?altConcept ?typeConcept ?regionConcept
						     ?founderConcept ?founderInstaSimLabel ?regionInstaSimLabel ?typeInstaSimLabel ?altInstaSimLabel ?tempInstaSimLabel ?domainInstaSimLabel ?csinstasimlabel
                ";

                SparqlResultSet results = Functions.ExecuteSparqlQuery(dataset, sparqlQuery);

                if (results is not null)
                {
                    var filterParam = "http://cultural.heritage/Ethiopia#Geographical_area";

                    HashSet<string?> concepts = new(results.Select(result => result["concept"].ToString()));
                    HashSet<string?> instances = new(results.Select(result => Functions.CleanUpString(result["instanceLabel"].ToString())));
                    instances.UnionWith(results
                                .SelectMany(result => new[]
                                {
                                    result.HasBoundValue("similarInstanceLabel") ? Functions.CleanUpString(result["similarInstanceLabel"].ToString()) : null,
                                    result.HasBoundValue("subPartInstanceLabel") ? Functions.CleanUpString(result["subPartInstanceLabel"].ToString()) : null,
                                    result.HasBoundValue("mainPartInstanceLabel") ? Functions.CleanUpString(result["mainPartInstanceLabel"].ToString()) : null,
                                    result.HasBoundValue("domainInstanceLabel") ? Functions.CleanUpString(result["domainInstanceLabel"].ToString()) : null,
                                    result.HasBoundValue("csinstanceLabel") ? Functions.CleanUpString(result["csinstanceLabel"].ToString()) : null,
                                    result.HasBoundValue("tempInstanceLabel") ? Functions.CleanUpString(result["tempInstanceLabel"].ToString()) : null,
                                    result.HasBoundValue("altInstanceLabel") ? Functions.CleanUpString(result["altInstanceLabel"].ToString()) : null,
                                    result.HasBoundValue("typeInstanceLabel") ? Functions.CleanUpString(result["typeInstanceLabel"].ToString()) : null,
                                    result.HasBoundValue("regionInstaceLabel") ? Functions.CleanUpString(result["regionInstaceLabel"].ToString()) : null,
                                    result.HasBoundValue("founderInstanceLabel") ? Functions.CleanUpString(result["founderInstanceLabel"].ToString()) : null,
                                    result.HasBoundValue("founderInstaSimLabel") ? Functions.CleanUpString(result["founderInstaSimLabel"].ToString()) : null,
                                    result.HasBoundValue("regionInstaSimLabel") ? Functions.CleanUpString(result["regionInstaSimLabel"].ToString()) : null,
                                    result.HasBoundValue("typeInstaSimLabel") ? Functions.CleanUpString(result["typeInstaSimLabel"].ToString()) : null,
                                    result.HasBoundValue("altInstaSimLabel") ? Functions.CleanUpString(result["altInstaSimLabel"].ToString()) : null,
                                    result.HasBoundValue("tempInstaSimLabel") ? Functions.CleanUpString(result["tempInstaSimLabel"].ToString()) : null,
                                    result.HasBoundValue("domainInstaSimLabel") ? Functions.CleanUpString(result["domainInstaSimLabel"].ToString()) : null,
                                    result.HasBoundValue("csinstasimlabel") ? Functions.CleanUpString(result["csinstasimlabel"].ToString()) : null,

                                })
                                .Where(label => label != null));

                    concepts.UnionWith(results
                               .SelectMany(result => new[]
                               {
                                    result.HasBoundValue("csconcept") ? Functions.CleanUpString(result["csconcept"].ToString()) : null,
                                    result.HasBoundValue("mainPartConcept") ? Functions.CleanUpString(result["mainPartConcept"].ToString()) : null,
                                    result.HasBoundValue("domainConcept") ? Functions.CleanUpString(result["domainConcept"].ToString()) : null,
                                    result.HasBoundValue("tempConcept") ? Functions.CleanUpString(result["tempConcept"].ToString()) : null,
                                    result.HasBoundValue("altConcept") ? Functions.CleanUpString(result["altConcept"].ToString()) : null,
                                    result.HasBoundValue("typeConcept") ? Functions.CleanUpString(result["typeConcept"].ToString()) : null,
                                    result.HasBoundValue("regionConcept") ? Functions.CleanUpString(result["regionConcept"].ToString()) : null,
                                    result.HasBoundValue("founderConcept") ? Functions.CleanUpString(result["founderConcept"].ToString()) : null,
                               })
                                .Where(label => label != null));

                    var relevantInstances = GetRelevantInstances(dataset, instances);
                    var responses = await _repository.FindDocuments(concepts, instances, filterParam);

                    foreach (var response in responses)
                    {
                        var splitedTitle = response.Title.Split(",")[0];

                        if (relevantInstances.Any(instance => response.Title.ToLower().Contains(instance.ToLower()) || splitedTitle.ToLower().Contains(instance.Split(",")[0].ToLower())))
                            response.Tf += Constants.TitleFactor;

                        else if (response.Title.Contains(Constants.RootDocument))
                            response.Tf += Constants.RootDocFactor;
                    }

                    return new ResponseModel<dynamic> { Success = true, Data = responses.OrderByDescending(r => r.Tf) };
                }

            }

            return new ResponseModel<dynamic> { Success = false, Message = "please add search text" };
        }
        catch (Exception x)
        {
            return new ResponseModel<dynamic> { Success = false, Message = x.Message };
        }
    }

    private HashSet<string?> GetRelevantInstances(ISparqlDataset dataSet, HashSet<string> instances)
    {
        if (!instances.Any()) return new HashSet<string>();

        var contentValues = instances.Select(i => $"\"{Functions.EscapeString(i)}\"").ToList();
        string contentValuesString = string.Join(" ", contentValues);

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
                    ?instanceType rdfs:subClassOf ?parentClass.
            
                    ?instance rdfs:label ?instanceLabel .

                    FILTER(?parentClass != table:Geographical_area)
                    FILTER(REGEX(LCASE(?instanceLabel), LCASE(?content), ""i""))
                    FILTER(?instanceType != owl:NamedIndividual)
                    FILTER(LCASE(?instanceLabel) = ?instanceLabel)  # Exclude uppercase labels
                }}
                GROUP BY ?content ?instanceLabel ?instanceType
            ";

        SparqlResultSet resultSet = Functions.ExecuteSparqlQuery(dataSet, sparqlQuery);

        if (resultSet is not null)
        {
            HashSet<string?> relevantInstances = new(resultSet.Select(result => Functions.CleanUpString(result["instanceLabel"].ToString())));

            return relevantInstances;
        }

        return new HashSet<string>();
    }

}
