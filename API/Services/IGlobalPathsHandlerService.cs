namespace API.Services;

public interface IGlobalPathsHandlerService
{
    Task<bool> HandleAsync(HttpContext context);
}