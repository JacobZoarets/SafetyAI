using System;
using System.Threading.Tasks;
using System.Web.Http;
using SafetyAI.Models.DTOs;
using SafetyAI.Services.Interfaces;
using SafetyAI.Web.App_Start;
using SafetyAI.Services.Infrastructure;

namespace SafetyAI.Web.Controllers
{
    [RoutePrefix("api/v1/safety")]
    public class SafetyChatController : ApiController
    {
        private readonly IChatService _chatService;

        public SafetyChatController()
        {
            _chatService = DependencyConfig.GetService<IChatService>();
        }

        /// <summary>
        /// Process a safety-related chat query
        /// </summary>
        /// <param name="request">Chat request</param>
        /// <returns>Chat response</returns>
        [HttpPost]
        [Route("chat")]
        public async Task<IHttpActionResult> ProcessChatQuery([FromBody] ChatRequest request)
        {
            try
            {
                // Validate request
                if (request == null)
                {
                    return BadRequest("Request body is required");
                }

                if (string.IsNullOrWhiteSpace(request.Query))
                {
                    return BadRequest("Query is required");
                }

                if (request.Query.Length > 1000)
                {
                    return BadRequest("Query exceeds maximum length of 1000 characters");
                }

                // Create user context
                var userContext = new UserContext
                {
                    UserId = request.UserId ?? "api_user",
                    UserRole = request.UserRole ?? "Employee",
                    Location = request.Location,
                    Language = request.Language ?? "en"
                };

                // Generate session ID if not provided
                var sessionId = request.SessionId ?? $"api_session_{DateTime.Now.Ticks}";

                // Process the chat query
                using (var chatService = DependencyConfig.GetService<IChatService>())
                {
                    var chatResponse = await chatService.ProcessQueryAsync(request.Query, sessionId, userContext);

                    var response = new
                    {
                        success = true,
                        data = new
                        {
                            sessionId = chatResponse.SessionId,
                            response = chatResponse.Response,
                            confidence = chatResponse.ConfidenceScore,
                            requiresHumanReview = chatResponse.RequiresHumanReview,
                            suggestedActions = chatResponse.SuggestedActions,
                            referencedDocuments = chatResponse.ReferencedDocuments?.Select(doc => new
                            {
                                title = doc.Title,
                                source = doc.Source,
                                documentType = doc.DocumentType
                            }),
                            timestamp = DateTime.UtcNow
                        }
                    };

                    return Ok(response);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "SafetyChatAPI");
                return InternalServerError(new Exception("An error occurred while processing the chat query"));
            }
        }

        /// <summary>
        /// Get chat session history
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        /// <returns>Chat history</returns>
        [HttpGet]
        [Route("chat/{sessionId}/history")]
        public async Task<IHttpActionResult> GetChatHistory(string sessionId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    return BadRequest("Session ID is required");
                }

                // This would typically retrieve from a database or cache
                // For now, return a placeholder response
                var response = new
                {
                    success = true,
                    data = new
                    {
                        sessionId = sessionId,
                        messages = new object[] { }, // Placeholder - would contain actual chat history
                        startTime = DateTime.UtcNow.AddHours(-1),
                        lastActivity = DateTime.UtcNow
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "ChatHistoryAPI");
                return InternalServerError(new Exception("An error occurred while retrieving chat history"));
            }
        }

        /// <summary>
        /// Get suggested safety questions
        /// </summary>
        /// <returns>List of suggested questions</returns>
        [HttpGet]
        [Route("chat/suggestions")]
        public IHttpActionResult GetSuggestedQuestions()
        {
            try
            {
                var suggestions = new[]
                {
                    "What PPE is required for working at heights?",
                    "How do I report a safety incident?",
                    "What should I do in case of a chemical spill?",
                    "What are the lockout/tagout procedures?",
                    "How often should safety training be conducted?",
                    "What are the emergency evacuation procedures?",
                    "How do I conduct a risk assessment?",
                    "What are the requirements for confined space entry?",
                    "How should I handle electrical safety?",
                    "What are the proper lifting techniques?"
                };

                var response = new
                {
                    success = true,
                    data = new
                    {
                        suggestions = suggestions,
                        categories = new[]
                        {
                            "PPE & Equipment",
                            "Incident Reporting",
                            "Emergency Procedures",
                            "Training & Compliance",
                            "Risk Assessment"
                        }
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "ChatSuggestionsAPI");
                return InternalServerError(new Exception("An error occurred while retrieving suggestions"));
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _chatService?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public class ChatRequest
    {
        public string Query { get; set; }
        public string SessionId { get; set; }
        public string UserId { get; set; }
        public string UserRole { get; set; }
        public string Location { get; set; }
        public string Language { get; set; }
    }
}