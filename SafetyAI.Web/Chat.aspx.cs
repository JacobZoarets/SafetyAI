using System;
using System.Web.UI;
using SafetyAI.Services.Interfaces;
using SafetyAI.Web.App_Start;
using SafetyAI.Services.Infrastructure;
using SafetyAI.Models.DTOs;
using Newtonsoft.Json;

namespace SafetyAI.Web
{
    public partial class Chat : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            
            if (!IsPostBack)
            {
                // Generate session ID if not exists
                if (string.IsNullOrEmpty(hdnSessionId.Value))
                {
                    hdnSessionId.Value = $"chat_{DateTime.Now.Ticks}_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
                }
            }
        }

        protected async void btnSendMessage_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtChatInput.Text))
                return;

            try
            {
                var userMessage = txtChatInput.Text.Trim();
                var sessionId = hdnSessionId.Value;

                // Create user context
                var userContext = new UserContext
                {
                    UserId = Session["UserId"]?.ToString() ?? "Anonymous",
                    UserRole = Session["UserRole"]?.ToString() ?? "Employee",
                    Location = Session["Location"]?.ToString(),
                    Permissions = new System.Collections.Generic.List<string>()
                };

                // Add user message to chat first
                ClientScript.RegisterStartupScript(this.GetType(), "addUserMessage", 
                    $@"setTimeout(function() {{
                        if(typeof window.addUserMessage === 'function') {{ 
                            window.addUserMessage('{EscapeJavaScript(userMessage)}'); 
                        }} else {{ 
                            console.error('addUserMessage function not found'); 
                        }}
                    }}, 100);", true);

                // Show typing indicator
                ClientScript.RegisterStartupScript(this.GetType(), "showTyping", 
                    "setTimeout(function() { if(typeof window.showTypingIndicator === 'function') { window.showTypingIndicator(); } }, 200);", true);

                // Process the chat query
                var chatService = DependencyConfig.GetService<IChatService>();
                if (chatService == null)
                {
                    throw new InvalidOperationException("Chat service is not available. Please check the system configuration.");
                }
                
                var response = await chatService.ProcessQueryAsync(userMessage, sessionId, userContext);

                    // Hide typing indicator and add assistant response
                    var referencedDocsJson = "[]";
                    if (response.ReferencedDocuments != null && response.ReferencedDocuments.Count > 0)
                    {
                        var docs = new System.Collections.Generic.List<object>();
                        foreach (var doc in response.ReferencedDocuments)
                        {
                            docs.Add(new { Title = doc.Title, Source = doc.Source });
                        }
                        referencedDocsJson = Newtonsoft.Json.JsonConvert.SerializeObject(docs);
                    }

                    ClientScript.RegisterStartupScript(this.GetType(), "addAssistantMessage", 
                        $@"setTimeout(function() {{
                            console.log('Adding assistant message...');
                            console.log('Response text: ', '{EscapeJavaScript(response.Response)}');
                            
                            // Force hide indicators
                            var serverStatus = document.getElementById('serverStatus');
                            if (serverStatus) serverStatus.style.display = 'none';
                            
                            var typingIndicator = document.getElementById('typingIndicator');
                            if (typingIndicator) typingIndicator.style.display = 'none';
                            
                            // Add the response message
                            if(typeof window.addAssistantMessage === 'function') {{ 
                                window.addAssistantMessage('{EscapeJavaScript(response.Response)}', {referencedDocsJson}); 
                                console.log('Assistant message added successfully');
                            }} else {{
                                console.error('addAssistantMessage function not found');
                            }}
                        }}, 500);", true);

                    // Show warning if human review is required
                    //if (response.RequiresHumanReview)
                    //{
                    //    ClientScript.RegisterStartupScript(this.GetType(), "showWarning", 
                    //        @"setTimeout(function() {
                    //            if(typeof window.addAssistantMessage === 'function') {
                    //                window.addAssistantMessage('⚠️ This question may require consultation with a safety professional. Please contact your safety manager if you need immediate assistance.', []);
                    //            } else {
                    //                console.error('addAssistantMessage function not found for warning');
                    //            }
                    //        }, 1000);", true);
                    //}
                // Clear input
                txtChatInput.Text = "";
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "ChatPage");
                
                ClientScript.RegisterStartupScript(this.GetType(), "showError", 
                    @"setTimeout(function() {
                        // Force hide indicators
                        var serverStatus = document.getElementById('serverStatus');
                        if (serverStatus) serverStatus.style.display = 'none';
                        
                        var typingIndicator = document.getElementById('typingIndicator');
                        if (typingIndicator) typingIndicator.style.display = 'none';
                        
                        if(typeof window.addAssistantMessage === 'function') {
                            window.addAssistantMessage('I apologize, but I\'m experiencing technical difficulties. Please try again or contact a safety professional if you have an urgent safety concern.', []);
                        } else {
                            console.error('JavaScript functions not available for error handling');
                        }
                    }, 500);", true);
            }
        }

        private string EscapeJavaScript(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            return text.Replace("\\", "\\\\")
                      .Replace("'", "\\'")
                      .Replace("\"", "\\\"")
                      .Replace("\r", "\\r")
                      .Replace("\n", "\\n")
                      .Replace("\t", "\\t");
        }

        protected override void OnUnload(EventArgs e)
        {
            base.OnUnload(e);
        }
    }
}