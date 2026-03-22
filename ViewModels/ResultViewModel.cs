namespace BibleReader.Api.ViewModels;

public class ResultViewModel<T>
{
    public ResultViewModel(T data)
    {
        Data = data;
    }

    public ResultViewModel(string message)
    {
        Message = message;
    }

    public T? Data { get; set; }
    public string? Message { get; set; }
}
