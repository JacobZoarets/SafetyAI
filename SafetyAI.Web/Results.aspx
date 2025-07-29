<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Results.aspx.cs" Inherits="SafetyAI.Web.Results" Async="true" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>SafetyAI - Analysis Results</title>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet" />
    <link href="Styles/safetyai.css" rel="stylesheet" />
</head>
<body>
    <form id="form1" runat="server">
        <nav class="navbar navbar-expand-lg navbar-dark bg-primary">
            <div class="container">
                <a class="navbar-brand" href="Default.aspx">
                    <i class="fas fa-shield-alt me-2"></i>SafetyAI
                </a>
                <div class="navbar-nav">
                    <a class="nav-link" href="Default.aspx">Dashboard</a>
                    <a class="nav-link" href="History.aspx">History</a>
                    <a class="nav-link" href="Chat.aspx">AI Assistant</a>
                </div>
            </div>
        </nav>

        <div class="container mt-4">
            <div class="card">
                <div class="card-header d-flex justify-content-between align-items-center">
                    <h4><i class="fas fa-file-alt me-2"></i>Safety Report Analysis</h4>
                    <asp:LinkButton ID="btnBackToHistory" runat="server" CssClass="btn btn-outline-secondary btn-sm" OnClick="btnBackToHistory_Click">
                        <i class="fas fa-arrow-left me-1"></i>Back to History
                    </asp:LinkButton>
                </div>
                <div class="card-body">
                    <asp:Panel ID="pnlError" runat="server" CssClass="alert alert-danger" Visible="false">
                        <i class="fas fa-exclamation-triangle me-2"></i>
                        <asp:Label ID="lblError" runat="server"></asp:Label>
                    </asp:Panel>

                    <asp:Panel ID="pnlResults" runat="server" Visible="false">
                        <div class="row mb-4">
                            <div class="col-md-6">
                                <h6>File Information</h6>
                                <p><strong>File Name:</strong> <asp:Label ID="lblFileName" runat="server"></asp:Label></p>
                                <p><strong>Uploaded By:</strong> <asp:Label ID="lblUploadedBy" runat="server"></asp:Label></p>
                                <p><strong>Upload Date:</strong> <asp:Label ID="lblUploadDate" runat="server"></asp:Label></p>
                            </div>
                            <div class="col-md-6">
                                <h6>Analysis Status</h6>
                                <p><strong>Status:</strong> <asp:Label ID="lblStatus" runat="server"></asp:Label></p>
                                <p><strong>Analysis Date:</strong> <asp:Label ID="lblAnalysisDate" runat="server"></asp:Label></p>
                                <p><strong>Severity:</strong> <asp:Label ID="lblSeverity" runat="server"></asp:Label></p>
                                <p><strong>Risk Score:</strong> <asp:Label ID="lblRiskScore" runat="server"></asp:Label>/10</p>
                            </div>
                        </div>

                        <div class="row">
                            <div class="col-12">
                                <h6>Analysis Summary</h6>
                                <div class="bg-light p-3 rounded mb-4">
                                    <asp:Label ID="lblAnalysisSummary" runat="server"></asp:Label>
                                </div>
                            </div>
                        </div>

                        <div class="row">
                            <div class="col-12">
                                <h6>Safety Recommendations</h6>
                                <asp:Repeater ID="rptRecommendations" runat="server">
                                    <ItemTemplate>
                                        <div class="alert alert-info mb-3">
                                            <h6 class="alert-heading"><%# Eval("RecommendationType") %></h6>
                                            <p class="mb-2"><%# Eval("Description") %></p>
                                            <small class="text-muted">
                                                <strong>Priority:</strong> <%# Eval("Priority") %>
                                            </small>
                                        </div>
                                    </ItemTemplate>
                                </asp:Repeater>
                                
                                <asp:Panel ID="pnlNoRecommendations" runat="server" Visible="false" CssClass="text-center py-4">
                                    <i class="fas fa-check-circle fa-3x text-success mb-3"></i>
                                    <h5>No Safety Issues Identified</h5>
                                    <p class="text-muted">The analysis did not identify any specific safety recommendations for this report.</p>
                                </asp:Panel>
                            </div>
                        </div>
                    </asp:Panel>

                    <asp:Panel ID="pnlNotFound" runat="server" CssClass="text-center py-5" Visible="false">
                        <i class="fas fa-search fa-3x text-muted mb-3"></i>
                        <h4>Report Not Found</h4>
                        <p class="text-muted">The requested safety report could not be found or you don't have permission to view it.</p>
                    </asp:Panel>
                </div>
            </div>
        </div>
    </form>

    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js"></script>
    <script src="https://kit.fontawesome.com/your-fontawesome-kit.js"></script>
</body>
</html>