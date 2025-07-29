<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Chat.aspx.cs" Inherits="SafetyAI.Web.Chat" Async="true" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>SafetyAI - AI Safety Assistant</title>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet" />
    <link href="https://kit.fontawesome.com/your-fontawesome-kit.js" crossorigin="anonymous" />
    <link href="Styles/safetyai.css" rel="stylesheet" />
    <style>
        .chat-container {
            height: 600px;
            border: 1px solid #dee2e6;
            border-radius: 8px;
            display: flex;
            flex-direction: column;
        }
        
        .chat-header {
            background-color: #f8f9fa;
            padding: 15px;
            border-bottom: 1px solid #dee2e6;
            border-radius: 8px 8px 0 0;
        }
        
        .chat-messages {
            flex: 1;
            overflow-y: auto;
            padding: 15px;
            background-color: #ffffff;
        }
        
        .chat-input-area {
            padding: 15px;
            border-top: 1px solid #dee2e6;
            background-color: #f8f9fa;
            border-radius: 0 0 8px 8px;
        }
        
        .message {
            margin-bottom: 15px;
            display: flex;
            align-items: flex-start;
        }
        
        .message.user {
            justify-content: flex-end;
        }
        
        .message.assistant {
            justify-content: flex-start;
        }
        
        .message-content {
            max-width: 70%;
            padding: 12px 16px;
            border-radius: 18px;
            word-wrap: break-word;
        }
        
        .message.user .message-content {
            background-color: #007bff;
            color: white;
            margin-left: 20px;
        }
        
        .message.assistant .message-content {
            background-color: #e9ecef;
            color: #333;
            margin-right: 20px;
        }
        
        .message-avatar {
            width: 32px;
            height: 32px;
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 14px;
            font-weight: bold;
        }
        
        .user-avatar {
            background-color: #007bff;
            color: white;
        }
        
        .assistant-avatar {
            background-color: #28a745;
            color: white;
        }
        
        .typing-indicator {
            display: none;
            align-items: center;
            margin-bottom: 15px;
        }
        
        .server-status {
            position: fixed;
            top: 70px;
            right: 20px;
            padding: 8px 16px;
            background-color: #17a2b8;
            color: white;
            border-radius: 20px;
            font-size: 14px;
            z-index: 1000;
            display: none;
            box-shadow: 0 2px 8px rgba(0,0,0,0.2);
        }
        
        .server-status.processing {
            background-color: #ffc107;
            color: #333;
        }
        
        .server-status.error {
            background-color: #dc3545;
            color: white;
        }
        
        .typing-dots {
            display: flex;
            align-items: center;
            margin-left: 10px;
        }
        
        .typing-dots span {
            height: 8px;
            width: 8px;
            background-color: #999;
            border-radius: 50%;
            display: inline-block;
            margin: 0 2px;
            animation: typing 1.4s infinite ease-in-out;
        }
        
        .typing-dots span:nth-child(1) { animation-delay: -0.32s; }
        .typing-dots span:nth-child(2) { animation-delay: -0.16s; }
        
        @keyframes typing {
            0%, 80%, 100% { transform: scale(0.8); opacity: 0.5; }
            40% { transform: scale(1); opacity: 1; }
        }
        
        .suggested-questions {
            margin-top: 15px;
        }
        
        .suggested-question {
            display: inline-block;
            margin: 5px;
            padding: 8px 12px;
            background-color: #f8f9fa;
            border: 1px solid #dee2e6;
            border-radius: 20px;
            cursor: pointer;
            font-size: 14px;
            transition: all 0.2s;
        }
        
        .suggested-question:hover {
            background-color: #e9ecef;
            border-color: #adb5bd;
        }
        
        .referenced-docs {
            margin-top: 10px;
            padding: 10px;
            background-color: #f8f9fa;
            border-radius: 8px;
            border-left: 4px solid #007bff;
        }
        
        .doc-link {
            display: block;
            color: #007bff;
            text-decoration: none;
            font-size: 14px;
            margin: 2px 0;
        }
        
        .doc-link:hover {
            text-decoration: underline;
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <nav class="navbar navbar-expand-lg navbar-dark bg-primary">
            <div class="container">
                <a class="navbar-brand" href="Default.aspx">
                    <i class="fas fa-shield-alt me-2"></i>DatWiseAI
                </a>
                <div class="navbar-nav me-auto">
                    <a class="nav-link" href="Default.aspx">Dashboard</a>
                    <a class="nav-link" href="History.aspx">History</a>
                    <a class="nav-link active" href="Chat.aspx">AI Assistant</a>
                </div>
            </div>
        </nav>

        <div class="container mt-4">
            <div class="row">
                <div class="col-lg-8">
                    <div class="chat-container">
                        <div class="chat-header">
                            <h5 class="mb-0">
                                <i class="fas fa-robot me-2"></i>AI Safety Assistant
                                <small class="text-muted ms-2">Ask me anything about workplace safety</small>
                            </h5>
                        </div>
                        
                        <div class="chat-messages" id="chatMessages">
                            <div class="message assistant">
                                <div class="message-avatar assistant-avatar">AI</div>
                                <div class="message-content">
                                    Hello! I'm your AI Safety Assistant. I can help you with:
                                    <ul class="mb-0 mt-2">
                                        <li>Safety procedures and best practices</li>
                                        <li>OSHA regulations and compliance</li>
                                        <li>Incident response guidance</li>
                                        <li>PPE recommendations</li>
                                        <li>Emergency procedures</li>
                                    </ul>
                                    What safety question can I help you with today?
                                </div>
                            </div>
                            
                            <div class="typing-indicator" id="typingIndicator">
                                <div class="message-avatar assistant-avatar">AI</div>
                                <div class="typing-dots">
                                    <span></span>
                                    <span></span>
                                    <span></span>
                                </div>
                            </div>
                        </div>
                        
                        <div class="chat-input-area">
                            <div class="input-group">
                                <asp:TextBox ID="txtChatInput" runat="server" CssClass="form-control" 
                                    placeholder="Type your safety question here..." 
                                    onkeypress="return handleEnterKey(event)"></asp:TextBox>
                                <asp:Button ID="btnSendMessage" runat="server" Text="Send" 
                                    CssClass="btn btn-primary" OnClick="btnSendMessage_Click" />
                            </div>
                        </div>
                    </div>
                </div>
                
                <div class="col-lg-4">
                    <div class="card">
                        <div class="card-header">
                            <h6><i class="fas fa-lightbulb me-2"></i>Suggested Questions</h6>
                        </div>
                        <div class="card-body">
                            <div class="suggested-questions">
                                <div class="suggested-question" onclick="askQuestion('What PPE is required for working at heights?')">
                                    What PPE is required for working at heights?
                                </div>
                                <div class="suggested-question" onclick="askQuestion('How do I report a safety incident?')">
                                    How do I report a safety incident?
                                </div>
                                <div class="suggested-question" onclick="askQuestion('What should I do in case of a chemical spill?')">
                                    What should I do in case of a chemical spill?
                                </div>
                                <div class="suggested-question" onclick="askQuestion('What are the lockout/tagout procedures?')">
                                    What are the lockout/tagout procedures?
                                </div>
                                <div class="suggested-question" onclick="askQuestion('How often should safety training be conducted?')">
                                    How often should safety training be conducted?
                                </div>
                            </div>
                        </div>
                    </div>
                    
                    <div class="card mt-3">
                        <div class="card-header">
                            <h6><i class="fas fa-exclamation-triangle me-2"></i>Emergency Contacts</h6>
                        </div>
                        <div class="card-body">
                            <p><strong>Emergency:</strong> 911</p>
                            <p><strong>Safety Manager:</strong> ext. 2345</p>
                            <p><strong>First Aid:</strong> ext. 2222</p>
                            <p><strong>Security:</strong> ext. 2911</p>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <asp:HiddenField ID="hdnSessionId" runat="server" />
        
        <div id="serverStatus" class="server-status">
            <i class="fas fa-cog fa-spin me-2"></i>Processing your request...
        </div>
    </form>

    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js"></script>
    <script>
        // Define all functions first to ensure they're available immediately
        function addUserMessage(message) {
            const chatMessages = document.getElementById('chatMessages');
            const messageDiv = document.createElement('div');
            messageDiv.className = 'message user';
            messageDiv.innerHTML = `
                <div class="message-content">${escapeHtml(message)}</div>
                <div class="message-avatar user-avatar">You</div>
            `;
            chatMessages.appendChild(messageDiv);
            scrollToBottom();
        }

        function addAssistantMessage(message, referencedDocs) {
            console.log('addAssistantMessage called with:', message, referencedDocs);
            const chatMessages = document.getElementById('chatMessages');
            console.log('chatMessages element:', chatMessages);
            
            if (!chatMessages) {
                console.error('chatMessages element not found!');
                return;
            }
            
            const messageDiv = document.createElement('div');
            messageDiv.className = 'message assistant';
            
            let docsHtml = '';
            if (referencedDocs && referencedDocs.length > 0) {
                docsHtml = '<div class="referenced-docs"><strong>Referenced Documents:</strong><br>';
                referencedDocs.forEach(doc => {
                    docsHtml += `<a href="#" class="doc-link">${doc.Title} (${doc.Source})</a>`;
                });
                docsHtml += '</div>';
            }
            
            messageDiv.innerHTML = `
                <div class="message-avatar assistant-avatar">AI</div>
                <div class="message-content">
                    ${message.replace(/\n/g, '<br>')}
                    ${docsHtml}
                </div>
            `;
            chatMessages.appendChild(messageDiv);
            console.log('Message div added to chat');
            scrollToBottom();
        }

        function showTypingIndicator() {
            document.getElementById('typingIndicator').style.display = 'flex';
            scrollToBottom();
        }

        function hideTypingIndicator() {
            document.getElementById('typingIndicator').style.display = 'none';
        }

        function showServerStatus(message, type) {
            const statusDiv = document.getElementById('serverStatus');
            statusDiv.className = 'server-status ' + (type || '');
            statusDiv.innerHTML = '<i class="fas fa-cog fa-spin me-2"></i>' + message;
            statusDiv.style.display = 'block';
        }

        function hideServerStatus() {
            document.getElementById('serverStatus').style.display = 'none';
        }

        function showServerError(message) {
            showServerStatus('<i class="fas fa-exclamation-triangle me-2"></i>' + message, 'error');
            setTimeout(hideServerStatus, 5000);
        }

        function scrollToBottom() {
            const chatMessages = document.getElementById('chatMessages');
            chatMessages.scrollTop = chatMessages.scrollHeight;
        }

        function escapeHtml(text) {
            const div = document.createElement('div');
            div.textContent = text;
            return div.innerHTML;
        }

        // Ensure functions are available globally immediately
        window.addUserMessage = addUserMessage;
        window.addAssistantMessage = addAssistantMessage;
        window.showTypingIndicator = showTypingIndicator;
        window.hideTypingIndicator = hideTypingIndicator;
        window.showServerStatus = showServerStatus;
        window.hideServerStatus = hideServerStatus;
        window.showServerError = showServerError;

        function generateSessionId() {
            return 'chat_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
        }

        // Initialize session ID
        if (!document.getElementById('<%= hdnSessionId.ClientID %>').value) {
            document.getElementById('<%= hdnSessionId.ClientID %>').value = generateSessionId();
        }

        function handleEnterKey(event) {
            if (event.keyCode === 13) {
                event.preventDefault();
                document.getElementById('<%= btnSendMessage.ClientID %>').click();
                return false;
            }
            return true;
        }

        function askQuestion(question) {
            document.getElementById('<%= txtChatInput.ClientID %>').value = question;
            document.getElementById('<%= btnSendMessage.ClientID %>').click();
        }


        // Auto-focus on input
        document.addEventListener('DOMContentLoaded', function() {
            document.getElementById('<%= txtChatInput.ClientID %>').focus();
        });
    </script>
</body>
</html>