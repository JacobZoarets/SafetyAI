<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="SafetyAI.Web.Default" Async="true" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>SafetyAI - Safety Incident Analysis</title>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet" />
    <link href="Styles/safetyai.css" rel="stylesheet" />
</head>
<body>
    <form id="form1" runat="server" enctype="multipart/form-data">
        <asp:ScriptManager ID="ScriptManager1" runat="server" EnablePageMethods="true" />
        <nav class="navbar navbar-expand-lg navbar-dark bg-primary">
            <div class="container">
                <a class="navbar-brand" href="Default.aspx">
                    <i class="fas fa-shield-alt me-2"></i>DatWiseAI
                </a>
                <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target="#navbarNav">
                    <span class="navbar-toggler-icon"></span>
                </button>
                <div class="collapse navbar-collapse" id="navbarNav">
                    <ul class="navbar-nav me-auto">
                        <li class="nav-item">
                            <a class="nav-link active" href="Default.aspx">Dashboard</a>
                        </li>
                        <li class="nav-item">
                            <a class="nav-link" href="History.aspx">History</a>
                        </li>
                        <li class="nav-item">
                            <a class="nav-link" href="Chat.aspx">AI Assistant</a>
                        </li>
                    </ul>
                </div>
            </div>
        </nav>

        <div class="container mt-4">
            <div class="row">
                <div class="col-md-8">
                    <div class="card">
                        <div class="card-header">
                            <h4><i class="fas fa-upload me-2"></i>Upload Safety Report</h4>
                        </div>
                        <div class="card-body">
                            <div class="upload-area" id="uploadArea">
                                <div class="upload-content">
                                    <i class="fas fa-cloud-upload-alt fa-3x text-muted mb-3"></i>
                                    <h5>Drag and drop your safety report here</h5>
                                    <p class="text-muted">or click to select files</p>
                                    <p class="small text-muted">Supported formats: PDF, JPEG, PNG, TIFF (Max 10MB)</p>
                                </div>
                                <asp:FileUpload ID="fileUpload" runat="server" CssClass="file-input" accept=".pdf,.jpg,.jpeg,.png,.tiff,.tif" />
                            </div>
                            
                            <div class="mt-3">
                                <asp:Button ID="btnUpload" runat="server" Text="Analyze Report" CssClass="btn btn-primary" OnClick="btnUpload_Click" CausesValidation="false" />
                                <asp:Button ID="btnClear" runat="server" Text="Clear" CssClass="btn btn-secondary ms-2" OnClick="btnClear_Click" UseSubmitBehavior="false" />
                            </div>
                            
                            <div id="progressArea" class="mt-3" style="display: none;">
                                <div class="progress">
                                    <div class="progress-bar progress-bar-striped progress-bar-animated" role="progressbar" style="width: 0%"></div>
                                </div>
                                <p class="mt-2 text-center">Processing your safety report...</p>
                            </div>
                            
                            <asp:Panel ID="pnlMessage" runat="server" Visible="false" CssClass="mt-3">
                                <asp:Label ID="lblMessage" runat="server" CssClass="alert" style="display:block;"></asp:Label>
                            </asp:Panel>
                            
                            <!-- Gemini API Response Display -->
                            <asp:Panel ID="pnlGeminiResponse" runat="server" Visible="false" CssClass="mt-3">
                                <div class="card">
                                    <div class="card-header d-flex justify-content-between align-items-center">
                                        <h6 class="mb-0">
                                            <i class="fas fa-code me-2"></i>Gemini API Response Data
                                        </h6>
                                        <button type="button" class="btn btn-sm btn-outline-secondary" onclick="toggleJsonDisplay()">
                                            <i class="fas fa-eye" id="jsonToggleIcon"></i>
                                            <span id="jsonToggleText">Show</span>
                                        </button>
                                    </div>
                                    <div class="card-body" id="jsonResponseBody" style="display: none;">
                                        <pre id="jsonContent" style="background-color: #f8f9fa; padding: 15px; border-radius: 5px; max-height: 400px; overflow-y: auto; font-size: 12px;">
                                            <asp:Literal ID="litGeminiResponse" runat="server"></asp:Literal>
                                        </pre>
                                        <div class="mt-2">
                                            <button type="button" class="btn btn-sm btn-outline-primary" onclick="copyToClipboard()">
                                                <i class="fas fa-copy me-1"></i>Copy JSON
                                            </button>
                                        </div>
                                    </div>
                                </div>
                            </asp:Panel>
                            
                            <div id="clientProgress" class="mt-3" style="display:none;">
                                <div class="alert alert-info">
                                    <i class="fas fa-spinner fa-spin me-2"></i>Processing your safety document with AI...
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
                
                <div class="col-md-4">
                    <div class="card">
                        <div class="card-header">
                            <h5><i class="fas fa-chart-line me-2"></i>Quick Stats</h5>
                        </div>
                        <div class="card-body">
                            <div class="stat-item">
                                <div class="stat-number text-primary">
                                    <asp:Label ID="lblTotalReports" runat="server" Text="0"></asp:Label>
                                </div>
                                <div class="stat-label">Total Reports</div>
                            </div>
                            <div class="stat-item">
                                <div class="stat-number text-warning">
                                    <asp:Label ID="lblPendingReports" runat="server" Text="0"></asp:Label>
                                </div>
                                <div class="stat-label">Pending Analysis</div>
                            </div>
                            <div class="stat-item">
                                <div class="stat-number text-danger">
                                    <asp:Label ID="lblCriticalIncidents" runat="server" Text="0"></asp:Label>
                                </div>
                                <div class="stat-label">Critical Incidents</div>
                            </div>
                        </div>
                    </div>
                    
                    <div class="card mt-3">
                        <div class="card-header">
                            <h5><i class="fas fa-clock me-2"></i>Recent Activity</h5>
                        </div>
                        <div class="card-body">
                            <asp:Repeater ID="rptRecentActivity" runat="server">
                                <ItemTemplate>
                                    <div class="activity-item">
                                        <div class="activity-time"><%# Eval("UploadedDate", "{0:HH:mm}") %></div>
                                        <div class="activity-description">
                                            Report: <%# Eval("FileName") %>
                                            <span class="badge bg-<%# GetStatusBadgeClass(Eval("Status")) %> ms-1">
                                                <%# Eval("Status") %>
                                            </span>
                                        </div>
                                    </div>
                                </ItemTemplate>
                            </asp:Repeater>
                        </div>
                    </div>
                </div>
            </div>
            
            <!-- History Section -->
            <div class="row mt-4" style="display:none">
                <div class="col-12">
                    <div class="card">
                        <div class="card-header d-flex justify-content-between align-items-center">
                            <h4><i class="fas fa-history me-2"></i>Recent Safety Reports</h4>
                            <div>
                                <asp:DropDownList ID="ddlStatusFilter" runat="server" CssClass="form-select form-select-sm d-inline-block" AutoPostBack="true" OnSelectedIndexChanged="ddlStatusFilter_SelectedIndexChanged">
                                    <asp:ListItem Value="" Text="All Status" />
                                    <asp:ListItem Value="Pending" Text="Pending" />
                                    <asp:ListItem Value="Processing" Text="Processing" />
                                    <asp:ListItem Value="Completed" Text="Completed" />
                                    <asp:ListItem Value="Failed" Text="Failed" />
                                    <asp:ListItem Value="RequiresReview" Text="Requires Review" />
                                </asp:DropDownList>
                                <asp:TextBox ID="txtSearch" runat="server" CssClass="form-control form-control-sm d-inline-block ms-2" placeholder="Search reports..." style="width: 200px;" />
                                <asp:Button ID="btnSearch" runat="server" Text="Search" CssClass="btn btn-outline-primary btn-sm ms-2" OnClick="btnSearch_Click" />
                            </div>
                        </div>
                        <div class="card-body">
                            <asp:GridView ID="gvReports" runat="server" CssClass="table table-striped table-hover" AutoGenerateColumns="false" 
                                          OnRowCommand="gvReports_RowCommand" DataKeyNames="Id" AllowPaging="false">
                                <Columns>
                                    <asp:BoundField DataField="FileName" HeaderText="File Name" />
                                    <asp:TemplateField HeaderText="Status">
                                        <ItemTemplate>
                                            <span class="badge bg-<%# GetStatusBadgeClass(Eval("Status")) %>">
                                                <%# Eval("Status") %>
                                            </span>
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                    <asp:TemplateField HeaderText="Severity">
                                        <ItemTemplate>
                                            <span class="badge bg-<%# GetSeverityBadgeClass(GetLatestSeverity(Eval("AnalysisResults"))) %>">
                                                <%# GetLatestSeverity(Eval("AnalysisResults")) %>
                                            </span>
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                    <asp:TemplateField HeaderText="Risk Score">
                                        <ItemTemplate>
                                            <div class="text-center">
                                                <span class="fw-bold"><%# GetLatestRiskScore(Eval("AnalysisResults")) ?? "N/A" %></span>
                                                <%# GetLatestRiskScore(Eval("AnalysisResults")) != null ? "/10" : "" %>
                                            </div>
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                    <asp:BoundField DataField="UploadedBy" HeaderText="Uploaded By" />
                                    <asp:TemplateField HeaderText="Upload Date">
                                        <ItemTemplate>
                                            <%# ((DateTime)Eval("UploadedDate")).ToString("yyyy-MM-dd HH:mm") %>
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                    <asp:TemplateField HeaderText="Actions">
                                        <ItemTemplate>
                                            <asp:LinkButton ID="btnView" runat="server" CommandName="ViewReport" CommandArgument='<%# Eval("Id") %>' 
                                                           CssClass="btn btn-outline-primary btn-sm me-1" ToolTip="View Details">
                                                <i class="fas fa-eye"></i>
                                            </asp:LinkButton>
                                            <asp:LinkButton ID="btnReanalyze" runat="server" CommandName="ReanalyzeReport" CommandArgument='<%# Eval("Id") %>' 
                                                           CssClass="btn btn-outline-secondary btn-sm" ToolTip="Re-analyze" 
                                                           Visible='<%# Eval("Status").ToString() == "Completed" %>'>
                                                <i class="fas fa-redo"></i>
                                            </asp:LinkButton>
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                </Columns>
                                <EmptyDataTemplate>
                                    <div class="text-center text-muted py-4">
                                        <i class="fas fa-inbox fa-3x mb-3"></i>
                                        <p>No safety reports found.</p>
                                    </div>
                                </EmptyDataTemplate>
                            </asp:GridView>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </form>

    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js"></script>
    <script src="https://kit.fontawesome.com/your-fontawesome-kit.js"></script>
    <script src="Scripts/safetyai.js"></script>
    <script>
        // Prevent double submissions
        function preventDoubleSubmit(button) {
            console.log('preventDoubleSubmit called');
            if (button.dataset.submitted === 'true') {
                console.log('Already submitted, preventing double submit');
                return false;
            }
            button.dataset.submitted = 'true';
            button.disabled = true;
            button.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Processing...';
            console.log('Allowing submit');
            return true;
        }
        
        // JSON Response Display Functions
        function toggleJsonDisplay() {
            const jsonBody = document.getElementById('jsonResponseBody');
            const toggleIcon = document.getElementById('jsonToggleIcon');
            const toggleText = document.getElementById('jsonToggleText');
            
            if (jsonBody.style.display === 'none') {
                jsonBody.style.display = 'block';
                toggleIcon.className = 'fas fa-eye-slash';
                toggleText.textContent = 'Hide';
            } else {
                jsonBody.style.display = 'none';
                toggleIcon.className = 'fas fa-eye';
                toggleText.textContent = 'Show';
            }
        }
        
        function copyToClipboard() {
            const jsonContent = document.getElementById('jsonContent');
            const textArea = document.createElement('textarea');
            textArea.value = jsonContent.textContent;
            document.body.appendChild(textArea);
            textArea.select();
            document.execCommand('copy');
            document.body.removeChild(textArea);
            
            // Show feedback
            const button = event.target.closest('button');
            const originalHtml = button.innerHTML;
            button.innerHTML = '<i class="fas fa-check me-1"></i>Copied!';
            button.className = 'btn btn-sm btn-success';
            
            setTimeout(() => {
                button.innerHTML = originalHtml;
                button.className = 'btn btn-sm btn-outline-primary';
            }, 2000);
        }
        
        // Force postback test
        window.addEventListener('load', function() {
            console.log('Page loaded');
            const uploadBtn = document.getElementById('<%= btnUpload.ClientID %>');
            if (uploadBtn) {
                console.log('Upload button found, ID:', uploadBtn.id);
                
                // Override click to force postback
                uploadBtn.addEventListener('click', function(e) {
                    console.log('BUTTON CLICKED - triggering postback');
                    e.preventDefault();
                    
                    // Show processing indicator
                    uploadBtn.disabled = true;
                    uploadBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Processing...';
                    
                    // Show client-side progress
                    document.getElementById('clientProgress').style.display = 'block';
                    
                    // Force ASP.NET postback
                    __doPostBack('<%= btnUpload.UniqueID %>', '');
                });
            }
        });
    </script>
</body>
</html>