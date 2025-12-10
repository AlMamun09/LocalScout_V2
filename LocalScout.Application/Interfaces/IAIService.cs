namespace LocalScout.Application.Interfaces
{
    public interface IAIService
    {
        Task<string> GenerateDescriptionAsync(Dictionary<string, string> context, string type);
    }
}
