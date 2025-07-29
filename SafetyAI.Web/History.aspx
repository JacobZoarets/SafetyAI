<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="History.aspx.cs" Inherits="SafetyAI.Web.History" Async="true" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>SafetyAI - Report History</title>
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
                    <i class="fas fa-shield-alt me-2"></i>DatWiseAI
                </a>
                <div class="navbar-nav me-auto">
                    <a class="nav-link" href="Default.aspx">Dashboard</a>
                    <a class="nav-link active" href="History.aspx">History</a>
                    <a class="nav-link" href="Chat.aspx">AI Assistant</a>
                </div>
            </div>
        </nav>

        <div class="container mt-4">
            <div class="row">
                <div class="col-md-9">
                    <div class="card">
                        <div class="card-header d-flex justify-content-between align-items-center">
                            <h4><i class="fas fa-history me-2"></i>Safety Report History</h4>
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
                                    <div class="text-center py-4">
                                        <i class="fas fa-inbox fa-3x text-muted mb-3"></i>
                                        <h5>No Reports Found</h5>
                                        <p class="text-muted">No safety reports match your current filters.</p>
                                    </div>
                                </EmptyDataTemplate>

                            </asp:GridView>
                        </div>
                    </div>
                </div>

                <div class="col-md-3">
                    <!-- Quick Stats -->
                    <div class="card">
                        <div class="card-header">
                            <h5><i class="fas fa-chart-bar me-2"></i>Quick Stats</h5>
                        </div>
                        <div class="card-body">
                            <div class="row text-center">
                                <div class="col-6">
                                    <div class="border-end">
                                        <h4 class="text-primary"><asp:Label ID="lblTotalReports" runat="server" Text="0"></asp:Label></h4>
                                        <small class="text-muted">Total Reports</small>
                                    </div>
                                </div>
                                <div class="col-6">
                                    <h4 class="text-warning"><asp:Label ID="lblPendingReports" runat="server" Text="0"></asp:Label></h4>
                                    <small class="text-muted">Pending</small>
                                </div>
                            </div>
                            <hr />
                            <div class="row text-center">
                                <div class="col-6">
                                    <div class="border-end">
                                        <h4 class="text-danger"><asp:Label ID="lblCriticalIncidents" runat="server" Text="0"></asp:Label></h4>
                                        <small class="text-muted">Critical</small>
                                    </div>
                                </div>
                                <div class="col-6">
                                    <h4 class="text-success"><asp:Label ID="lblCompletedToday" runat="server" Text="0"></asp:Label></h4>
                                    <small class="text-muted">Today</small>
                                </div>
                            </div>
                        </div>
                    </div>

                    <!-- Recent Activity -->
                    <div class="card mt-3">
                        <div class="card-header">
                            <h5><i class="fas fa-clock me-2"></i>Recent Activity</h5>
                        </div>
                        <div class="card-body">
                            <asp:Repeater ID="rptRecentActivity" runat="server">
                                <ItemTemplate>
                                    <div class="d-flex align-items-start mb-3">
                                        <div class="flex-shrink-0">
                                            <span class="badge bg-<%# GetActivityBadgeClass(Eval("Status")) %> rounded-pill">
                                                <i class="fas fa-<%# GetActivityIcon(Eval("Status")) %>"></i>
                                            </span>
                                        </div>
                                        <div class="flex-grow-1 ms-3">
                                            <div class="fw-bold"><%# Eval("FileName") %></div>
                                            <small class="text-muted">
                                                <%# Eval("Status") %> • <%# ((DateTime)Eval("UploadedDate")).ToString("MMM dd, HH:mm") %>
                                            </small>
                                        </div>
                                    </div>
                                </ItemTemplate>
                            </asp:Repeater>
                        </div>
                    </div>

                    <!-- Export Options -->
                    <div class="card mt-3">
                        <div class="card-header">
                            <h5><i class="fas fa-download me-2"></i>Export Data</h5>
                        </div>
                        <div class="card-body">
                            <div class="d-grid gap-2">
                                <asp:Button ID="btnExportCSV" runat="server" Text="Export to CSV" CssClass="btn btn-outline-primary btn-sm" OnClick="btnExportCSV_Click" />
                                <asp:Button ID="btnExportExcel" runat="server" Text="Export to Excel" CssClass="btn btn-outline-success btn-sm" OnClick="btnExportExcel_Click" />
                                <asp:Button ID="btnExportPDF" runat="server" Text="Export to PDF" CssClass="btn btn-outline-danger btn-sm" OnClick="btnExportPDF_Click" />
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </form>

    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js"></script>
    <script src="https://kit.fontawesome.com/your-fontawesome-kit.js"></script>
</body>
</html>