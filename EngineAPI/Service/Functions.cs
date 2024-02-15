using System.Text.RegularExpressions;
using VDS.RDF.Parsing;
using VDS.RDF.Query.Datasets;
using VDS.RDF.Query;

namespace EngineAPI.Service
{
    public static class Functions
    {
        public static string EscapeString(string value)
        {
            // Implement your logic to escape special characters if needed
            return value.Replace("\"", "\\\"");
        }

        public static string CleanUpString(string value)
        {
            string cleanedString = Regex.Replace(value, @"\^\^.*$", "");
            return cleanedString;
        }

        public static SparqlResultSet ExecuteSparqlQuery(ISparqlDataset dataSet, string sparqlQuery)
        {
            SparqlQueryParser parser = new();
            SparqlQuery query = parser.ParseFromString(sparqlQuery);

            ISparqlQueryProcessor processor = new LeviathanQueryProcessor(dataSet);
            SparqlResultSet resultSet = processor.ProcessQuery(query) as SparqlResultSet;

            return resultSet ?? new SparqlResultSet();
        }
    }
 
}
