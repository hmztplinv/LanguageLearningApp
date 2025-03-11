namespace LanguageLearningApp.Api.Application.Interfaces
{
    public interface ILlamaService
    {
        Task<string> GetChatResponseAsync(string prompt, List<string> conversationHistory);
    }
}
