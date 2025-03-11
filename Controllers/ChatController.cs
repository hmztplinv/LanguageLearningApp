using LanguageLearningApp.Api.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LanguageLearningApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly ILlamaService _llamaService;

        public ChatController(ILlamaService llamaService)
        {
            _llamaService = llamaService;
        }

        [HttpPost("generate")]
        public async Task<IActionResult> Chat([FromBody] ChatRequestDto request)
        {
            // İsteğe göre önce kısa vadeli hafıza (konuşma geçmişi) burada toplanabilir.
            var response = await _llamaService.GetChatResponseAsync(request.Prompt, request.ConversationHistory);
            return Ok(new { response });
        }
    }

    public class ChatRequestDto
    {
        public string Prompt { get; set; }
        public List<string> ConversationHistory { get; set; }
    }
}
