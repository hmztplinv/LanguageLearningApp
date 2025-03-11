namespace LanguageLearningApp.Api.Domain.Entities
{
    public class Message
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string Topic { get; set; }
        public string ErrorAnalysis { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }
    }
}
