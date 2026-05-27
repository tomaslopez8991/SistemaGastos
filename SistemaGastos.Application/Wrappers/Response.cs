namespace SistemaGastos.Application.Wrappers;

public class Response<T>
{
    public bool Success { get; set; }
    public T Data { get; set; }
    public string Message { get; set; }
    public List<string> Errors { get; set; } = new();

    public Response() { }

    public Response(T data, string message = null)
    {
        Success = true;
        Data = data;
        Message = message;
    }

    public Response(string message)
    {
        Success = false;
        Message = message;
        Errors = new List<string> { message };
    }

    public static Response<T> Ok(T data, string message = null)
    {
        return new Response<T>
        {
            Success = true,
            Data = data,
            Message = message ?? "Operation completed successfully"
        };
    }

    public static Response<T> Fail(string error)
    {
        return new Response<T>
        {
            Success = false,
            Message = error,
            Errors = new List<string> { error }
        };
    }

    public static Response<T> Fail(List<string> errors)
    {
        return new Response<T>
        {
            Success = false,
            Message = "Validation failed",
            Errors = errors
        };
    }
}
