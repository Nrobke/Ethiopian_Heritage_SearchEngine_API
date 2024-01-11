using EngineAPI.Domain.DTOs;
using EngineAPI.Domain.Misc;
using EngineAPI.Repository;
using EngineAPI.Service.Interface;
using Microsoft.ML;

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

            return new ResponseModel<dynamic> { Data = tokens };
        }

        return new ResponseModel<dynamic> {Success = false, Message = "please add search text" };
    }
}
