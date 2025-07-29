using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SafetyAI.Models.DTOs;
using SafetyAI.Services.Infrastructure;
using SafetyAI.Services.Interfaces;

namespace SafetyAI.Services.Implementation
{
    public class ChatService : IChatService, IDisposable
    {
        private readonly IGeminiAPIClient _geminiClient;
        private readonly Dictionary<string, ChatSession> _activeSessions;
        private bool _disposed = false;

        public ChatService(IGeminiAPIClient geminiClient)
        {
            _geminiClient = geminiClient ?? throw new ArgumentNullException(nameof(geminiClient));
            _activeSessions = new Dictionary<string, ChatSession>();
        }

        public async Task<ChatResponse> ProcessQueryAsync(string query, string sessionId, SafetyAI.Models.DTOs.UserContext context)
        {
            using (var logContext = new LogContext($"ProcessChatQuery_{sessionId}"))
            {
                try
                {
                    logContext.LogProgress($"Processing chat query: {query.Substring(0, Math.Min(50, query.Length))}...");

                    // Validate input
                    if (string.IsNullOrWhiteSpace(query))
                    {
                        return new ChatResponse
                        {
                            Response = "I'm sorry, but I didn't receive a question. Could you please ask me something about safety?",
                            SessionId = sessionId,
                            ConfidenceScore = 1.0,
                            RequiresHumanReview = false
                        };
                    }

                    // Get or create session
                    var session = GetOrCreateSession(sessionId, context);
                    
                    // Check if query is safety-related
                    //if (!IsSafetyRelatedQuery(query))
                    //{
                    //    return new ChatResponse
                    //    {
                    //        Response = "I'm a safety expert assistant. I can help you with workplace safety questions, incident procedures, safety regulations, and best practices. Could you please ask me something related to safety?",
                    //        SessionId = sessionId,
                    //        ConfidenceScore = 1.0,
                    //        RequiresHumanReview = false
                    //    };
                    //}

                    // Add query to session history
                    session.AddQuery(query);

                    // Build context-aware query
                    var contextualQuery = BuildContextualQuery(query, session, context);

                    logContext.LogProgress("Calling Gemini API for chat response");

                    // Get response from Gemini
                    var geminiResponse = await _geminiClient.CallWithRetryAsync(async () =>
                    {
                        return await _geminiClient.ProcessChatQueryAsync(contextualQuery, sessionId);
                    });

                    // Enhance response with additional information
                    var enhancedResponse = await EnhanceResponseAsync(geminiResponse, query, context);

                    // Add response to session history
                    session.AddResponse(enhancedResponse.Response);

                    // Check if escalation is needed
                    //enhancedResponse.RequiresHumanReview = await RequiresEscalationAsync(query, enhancedResponse);

                    logContext.LogProgress($"Chat response generated. Confidence: {enhancedResponse.ConfidenceScore:P2}");

                    return enhancedResponse;
                }
                catch (Exception ex)
                {
                    logContext.LogError($"Chat processing failed: {ex.Message}");
                    Logger.LogError(ex, "ChatService");

                    return new ChatResponse
                    {
                        Response = "I apologize, but I'm experiencing technical difficulties right now. Please try again in a moment, or contact a safety professional if you have an urgent safety concern.",
                        SessionId = sessionId,
                        ConfidenceScore = 0.0,
                        RequiresHumanReview = true
                    };
                }
            }
        }

        public async Task<List<SafetyDocument>> RetrieveRelevantDocumentsAsync(string query)
        {
            using (var logContext = new LogContext("RetrieveRelevantDocuments"))
            {
                try
                {
                    logContext.LogProgress($"Retrieving documents for query: {query.Substring(0, Math.Min(30, query.Length))}...");

                    var documents = new List<SafetyDocument>();

                    // Analyze query to determine relevant document types
                    var queryLower = query.ToLowerInvariant();

                    // OSHA-related documents
                    if (queryLower.Contains("osha") || queryLower.Contains("regulation") || queryLower.Contains("standard"))
                    {
                        documents.AddRange(GetOSHADocuments(query));
                    }

                    // Emergency procedures
                    if (queryLower.Contains("emergency") || queryLower.Contains("evacuation") || queryLower.Contains("fire"))
                    {
                        documents.AddRange(GetEmergencyProcedures(query));
                    }

                    // PPE-related documents
                    if (queryLower.Contains("ppe") || queryLower.Contains("personal protective") || queryLower.Contains("helmet") || queryLower.Contains("gloves"))
                    {
                        documents.AddRange(GetPPEDocuments(query));
                    }

                    // Training materials
                    if (queryLower.Contains("training") || queryLower.Contains("procedure") || queryLower.Contains("how to"))
                    {
                        documents.AddRange(GetTrainingDocuments(query));
                    }

                    // Chemical safety
                    if (queryLower.Contains("chemical") || queryLower.Contains("hazardous") || queryLower.Contains("msds") || queryLower.Contains("sds"))
                    {
                        documents.AddRange(GetChemicalSafetyDocuments(query));
                    }

                    logContext.LogProgress($"Retrieved {documents.Count} relevant documents");
                    return documents.Take(10).ToList(); // Limit to top 10 most relevant
                }
                catch (Exception ex)
                {
                    logContext.LogError($"Document retrieval failed: {ex.Message}");
                    Logger.LogError(ex, "ChatService");
                    return new List<SafetyDocument>();
                }
            }
        }

        public async Task<bool> RequiresEscalationAsync(string query, ChatResponse response)
        {
            try
            {
                var queryLower = query.ToLowerInvariant();
                var responseLower = response.Response.ToLowerInvariant();

                // Escalate for emergency situations
                if (queryLower.Contains("emergency") || queryLower.Contains("urgent") || queryLower.Contains("immediate"))
                {
                    return true;
                }

                // Escalate for serious incidents
                if (queryLower.Contains("injury") || queryLower.Contains("accident") || queryLower.Contains("incident"))
                {
                    return true;
                }

                // Escalate if response suggests contacting someone
                if (responseLower.Contains("contact") || responseLower.Contains("consult") || responseLower.Contains("speak with"))
                {
                    return true;
                }

                // Escalate for low confidence responses
                if (response.ConfidenceScore < 0.7)
                {
                    return true;
                }

                // Escalate for complex regulatory questions
                if (queryLower.Contains("legal") || queryLower.Contains("liability") || queryLower.Contains("lawsuit"))
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error determining escalation need: {ex.Message}", "ChatService");
                return true; // Err on the side of caution
            }
        }

        private ChatSession GetOrCreateSession(string sessionId, SafetyAI.Models.DTOs.UserContext context)
        {
            if (!_activeSessions.ContainsKey(sessionId))
            {
                _activeSessions[sessionId] = new ChatSession
                {
                    SessionId = sessionId,
                    UserId = context?.UserId,
                    UserRole = context?.UserRole,
                    StartTime = DateTime.UtcNow,
                    LastActivity = DateTime.UtcNow
                };
            }
            else
            {
                _activeSessions[sessionId].LastActivity = DateTime.UtcNow;
            }

            return _activeSessions[sessionId];
        }

        private bool IsSafetyRelatedQuery(string query)
        {
            var safetyKeywords = new[]
            {
                "safety", "hazard", "risk", "accident", "incident", "injury", "emergency",
                "ppe", "personal protective", "osha", "regulation", "procedure", "training",
                "fire", "chemical", "electrical", "fall", "slip", "trip", "lockout", "tagout",
                "evacuation", "first aid", "msds", "sds", "ventilation", "noise", "ergonomic"
            };

            var queryLower = query.ToLowerInvariant();
            return safetyKeywords.Any(keyword => queryLower.Contains(keyword));
        }

        private string BuildContextualQuery(string query, ChatSession session, SafetyAI.Models.DTOs.UserContext context)
        {
            var contextualQuery = query;

            // Add user role context
            if (!string.IsNullOrEmpty(context?.UserRole))
            {
                contextualQuery = $"As a {context.UserRole}, {query}";
            }

            // Add location context if available
            if (!string.IsNullOrEmpty(context?.Location))
            {
                contextualQuery += $" (Location: {context.Location})";
            }

            // Add conversation history for context (last 2 exchanges)
            if (session.QueryHistory.Count > 1)
            {
                var recentHistory = session.QueryHistory.Skip(Math.Max(0, session.QueryHistory.Count - 2)).ToList();
                var historyContext = string.Join(" ", recentHistory.Select(h => $"Previous: {h}"));
                contextualQuery = $"{historyContext}\n\nCurrent question: {contextualQuery}";
            }

            return contextualQuery;
        }

        private async Task<ChatResponse> EnhanceResponseAsync(ChatResponse baseResponse, string originalQuery, SafetyAI.Models.DTOs.UserContext context)
        {
            try
            {
                // Retrieve relevant documents
                var relevantDocs = await RetrieveRelevantDocumentsAsync(originalQuery);
                baseResponse.ReferencedDocuments = relevantDocs;

                // Extract suggested actions from response
                baseResponse.SuggestedActions = ExtractSuggestedActions(baseResponse.Response);

                // Add role-specific guidance
                if (!string.IsNullOrEmpty(context?.UserRole))
                {
                    var roleGuidance = GetRoleSpecificGuidance(originalQuery, context.UserRole);
                    if (!string.IsNullOrEmpty(roleGuidance))
                    {
                        baseResponse.Response += $"\n\n**For {context.UserRole}:** {roleGuidance}";
                    }
                }

                return baseResponse;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error enhancing chat response: {ex.Message}", "ChatService");
                return baseResponse; // Return base response if enhancement fails
            }
        }

        private List<string> ExtractSuggestedActions(string response)
        {
            var actions = new List<string>();
            var lines = response.Split('\n');

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (trimmedLine.StartsWith("-") || trimmedLine.StartsWith("•") || 
                    trimmedLine.ToLower().Contains("should") || trimmedLine.ToLower().Contains("recommend"))
                {
                    actions.Add(trimmedLine);
                }
            }

            return actions.Take(5).ToList(); // Limit to 5 actions
        }

        private string GetRoleSpecificGuidance(string query, string userRole)
        {
            var queryLower = query.ToLowerInvariant();
            
            var roleLower = userRole.ToLowerInvariant();
            switch (roleLower)
            {
                case "safety manager":
                case "safety officer":
                    return GetSafetyManagerGuidance(queryLower);
                case "supervisor":
                case "team lead":
                    return GetSupervisorGuidance(queryLower);
                case "worker":
                case "employee":
                    return GetWorkerGuidance(queryLower);
                case "maintenance":
                    return GetMaintenanceGuidance(queryLower);
                default:
                    return "";
            }
        }

        private string GetSafetyManagerGuidance(string queryLower)
        {
            if (queryLower.Contains("incident") || queryLower.Contains("accident"))
            {
                return "Ensure proper incident investigation procedures are followed and consider root cause analysis.";
            }
            if (queryLower.Contains("training"))
            {
                return "Document all training activities and maintain training records for compliance purposes.";
            }
            return "Consider the broader safety program implications and regulatory compliance requirements.";
        }

        private string GetSupervisorGuidance(string queryLower)
        {
            if (queryLower.Contains("worker") || queryLower.Contains("employee"))
            {
                return "Ensure your team understands the safety requirements and provide immediate feedback on unsafe behaviors.";
            }
            return "Lead by example and reinforce safety expectations with your team.";
        }

        private string GetWorkerGuidance(string queryLower)
        {
            if (queryLower.Contains("unsafe") || queryLower.Contains("hazard"))
            {
                return "Report any unsafe conditions to your supervisor immediately and don't hesitate to stop work if you feel unsafe.";
            }
            return "Remember that you have the right to a safe workplace and the responsibility to work safely.";
        }

        private string GetMaintenanceGuidance(string queryLower)
        {
            if (queryLower.Contains("equipment") || queryLower.Contains("machine"))
            {
                return "Always follow lockout/tagout procedures and ensure proper PPE is worn during maintenance activities.";
            }
            return "Prioritize safety over speed and ensure all safety systems are functional after maintenance.";
        }

        private List<SafetyDocument> GetOSHADocuments(string query)
        {
            return new List<SafetyDocument>
            {
                new SafetyDocument
                {
                    Title = "OSHA General Duty Clause",
                    Content = "Section 5(a)(1) requires employers to provide a workplace free from recognized hazards.",
                    DocumentType = "Regulation",
                    Source = "OSHA"
                },
                new SafetyDocument
                {
                    Title = "OSHA Recordkeeping Requirements",
                    Content = "29 CFR 1904 outlines requirements for recording and reporting workplace injuries and illnesses.",
                    DocumentType = "Regulation",
                    Source = "OSHA"
                }
            };
        }

        private List<SafetyDocument> GetEmergencyProcedures(string query)
        {
            return new List<SafetyDocument>
            {
                new SafetyDocument
                {
                    Title = "Emergency Action Plan",
                    Content = "Procedures for workplace emergencies including evacuation routes, assembly points, and emergency contacts.",
                    DocumentType = "Procedure",
                    Source = "Company Policy"
                },
                new SafetyDocument
                {
                    Title = "Fire Emergency Response",
                    Content = "Steps to take in case of fire: RACE (Rescue, Alarm, Confine, Evacuate/Extinguish).",
                    DocumentType = "Procedure",
                    Source = "Fire Safety Manual"
                }
            };
        }

        private List<SafetyDocument> GetPPEDocuments(string query)
        {
            return new List<SafetyDocument>
            {
                new SafetyDocument
                {
                    Title = "PPE Selection Guide",
                    Content = "Guidelines for selecting appropriate personal protective equipment based on hazard assessment.",
                    DocumentType = "Guide",
                    Source = "Safety Manual"
                },
                new SafetyDocument
                {
                    Title = "PPE Inspection and Maintenance",
                    Content = "Procedures for inspecting, maintaining, and replacing personal protective equipment.",
                    DocumentType = "Procedure",
                    Source = "Safety Manual"
                }
            };
        }

        private List<SafetyDocument> GetTrainingDocuments(string query)
        {
            return new List<SafetyDocument>
            {
                new SafetyDocument
                {
                    Title = "New Employee Safety Orientation",
                    Content = "Comprehensive safety training program for new employees covering basic safety principles and company policies.",
                    DocumentType = "Training Material",
                    Source = "HR Department"
                },
                new SafetyDocument
                {
                    Title = "Job-Specific Safety Training",
                    Content = "Specialized safety training requirements based on specific job functions and associated hazards.",
                    DocumentType = "Training Material",
                    Source = "Safety Department"
                }
            };
        }

        private List<SafetyDocument> GetChemicalSafetyDocuments(string query)
        {
            return new List<SafetyDocument>
            {
                new SafetyDocument
                {
                    Title = "Chemical Hazard Communication",
                    Content = "Requirements for labeling, safety data sheets, and employee training on chemical hazards.",
                    DocumentType = "Procedure",
                    Source = "EHS Manual"
                },
                new SafetyDocument
                {
                    Title = "Chemical Spill Response",
                    Content = "Procedures for responding to chemical spills including containment, cleanup, and reporting requirements.",
                    DocumentType = "Emergency Procedure",
                    Source = "Emergency Response Plan"
                }
            };
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // _geminiClient doesn't implement IDisposable
                    _activeSessions?.Clear();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    public class ChatSession
    {
        public string SessionId { get; set; }
        public string UserId { get; set; }
        public string UserRole { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime LastActivity { get; set; }
        public List<string> QueryHistory { get; set; }
        public List<string> ResponseHistory { get; set; }

        public ChatSession()
        {
            QueryHistory = new List<string>();
            ResponseHistory = new List<string>();
        }

        public void AddQuery(string query)
        {
            QueryHistory.Add(query);
            
            // Keep only last 10 queries for memory management
            if (QueryHistory.Count > 10)
            {
                QueryHistory.RemoveAt(0);
            }
        }

        public void AddResponse(string response)
        {
            ResponseHistory.Add(response);
            
            // Keep only last 10 responses for memory management
            if (ResponseHistory.Count > 10)
            {
                ResponseHistory.RemoveAt(0);
            }
        }
    }
}