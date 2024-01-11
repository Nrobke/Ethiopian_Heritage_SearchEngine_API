namespace EngineAPI.Domain.Misc;

public class ResponseModel<T>
{
    public T? Data { get; set; }
    public string? Ex { get; set; }
    public string? Message { get; set; }
    public bool Success { get; set; }
}
