namespace API.Services;

public interface IGlobalPathsHandlerService
{
    Task HandleAsync(HttpContext context);
}