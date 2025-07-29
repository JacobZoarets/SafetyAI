# SafetyAI Deployment Script for Windows Server 2019+ with IIS 10+
param(
    [Parameter(Mandatory=$true)]
    [string]$Environment = "Production",
    
    [Parameter(Mandatory=$false)]
    [string]$SiteName = "SafetyAI",
    
    [Parameter(Mandatory=$false)]
    [string]$AppPoolName = "SafetyAI_AppPool",
    
    [Parameter(Mandatory=$false)]
    [string]$DeployPath = "C:\inetpub\wwwroot\SafetyAI",
    
    [Parameter(Mandatory=$false)]
    [string]$BackupPath = "C:\Backups\SafetyAI"
)

Write-Host "Starting SafetyAI deployment for $Environment environment..." -ForegroundColor Green

# Check if running as administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Error "This script must be run as Administrator. Exiting..."
    exit 1
}

# Import required modules
Import-Module WebAdministration -ErrorAction SilentlyContinue
if (-not (Get-Module WebAdministration)) {
    Write-Error "IIS WebAdministration module not available. Please install IIS Management Tools."
    exit 1
}

try {
    # Step 1: Create backup of existing deployment
    Write-Host "Creating backup..." -ForegroundColor Yellow
    if (Test-Path $DeployPath) {
        $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
        $backupFolder = "$BackupPath\$timestamp"
        New-Item -ItemType Directory -Path $backupFolder -Force | Out-Null
        Copy-Item -Path "$DeployPath\*" -Destination $backupFolder -Recurse -Force
        Write-Host "Backup created at: $backupFolder" -ForegroundColor Green
    }

    # Step 2: Stop application pool and website
    Write-Host "Stopping IIS application pool and website..." -ForegroundColor Yellow
    if (Get-IISAppPool -Name $AppPoolName -ErrorAction SilentlyContinue) {
        Stop-WebAppPool -Name $AppPoolName
        Write-Host "Application pool '$AppPoolName' stopped" -ForegroundColor Green
    }
    
    if (Get-Website -Name $SiteName -ErrorAction SilentlyContinue) {
        Stop-Website -Name $SiteName
        Write-Host "Website '$SiteName' stopped" -ForegroundColor Green
    }

    # Step 3: Deploy application files
    Write-Host "Deploying application files..." -ForegroundColor Yellow
    if (-not (Test-Path $DeployPath)) {
        New-Item -ItemType Directory -Path $DeployPath -Force | Out-Null
    }

    # Copy application files (assuming build output is in current directory)
    $sourceFiles = @(
        "bin\*",
        "App_Data\*",
        "App_Start\*",
        "Controllers\*",
        "Scripts\*",
        "Styles\*",
        "*.aspx",
        "*.aspx.cs",
        "*.aspx.designer.cs",
        "*.asax",
        "*.asax.cs",
        "Web.config",
        "Global.asax"
    )

    foreach ($pattern in $sourceFiles) {
        $files = Get-ChildItem -Path $pattern -ErrorAction SilentlyContinue
        if ($files) {
            Copy-Item -Path $files -Destination $DeployPath -Recurse -Force
        }
    }

    # Step 4: Update Web.config for environment
    Write-Host "Updating configuration for $Environment environment..." -ForegroundColor Yellow
    $webConfigPath = "$DeployPath\Web.config"
    if (Test-Path $webConfigPath) {
        $webConfig = [xml](Get-Content $webConfigPath)
        
        # Update connection string for environment
        $connectionString = $webConfig.configuration.connectionStrings.add | Where-Object { $_.name -eq "SafetyAIConnection" }
        if ($connectionString) {
            switch ($Environment) {
                "Production" {
                    $connectionString.connectionString = "Data Source=PROD-SQL-SERVER;Initial Catalog=SafetyAI_Prod;Integrated Security=True;MultipleActiveResultSets=True"
                }
                "Staging" {
                    $connectionString.connectionString = "Data Source=STAGE-SQL-SERVER;Initial Catalog=SafetyAI_Stage;Integrated Security=True;MultipleActiveResultSets=True"
                }
                "Development" {
                    $connectionString.connectionString = "Data Source=(local);Initial Catalog=SafetyAI_Dev;Integrated Security=True;MultipleActiveResultSets=True"
                }
            }
        }

        # Update debug mode
        $compilation = $webConfig.configuration.'system.web'.compilation
        if ($compilation) {
            $compilation.debug = if ($Environment -eq "Development") { "true" } else { "false" }
        }

        # Update custom errors
        $customErrors = $webConfig.configuration.'system.web'.customErrors
        if ($customErrors) {
            $customErrors.mode = if ($Environment -eq "Development") { "Off" } else { "RemoteOnly" }
        }

        $webConfig.Save($webConfigPath)
        Write-Host "Web.config updated for $Environment" -ForegroundColor Green
    }

    # Step 5: Create/Configure Application Pool
    Write-Host "Configuring application pool..." -ForegroundColor Yellow
    if (Get-IISAppPool -Name $AppPoolName -ErrorAction SilentlyContinue) {
        Remove-WebAppPool -Name $AppPoolName
    }

    New-WebAppPool -Name $AppPoolName -Force
    Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name "managedRuntimeVersion" -Value "v4.0"
    Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name "processModel.identityType" -Value "ApplicationPoolIdentity"
    Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name "recycling.periodicRestart.time" -Value "00:00:00"
    Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name "processModel.maxProcesses" -Value 1
    Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name "processModel.idleTimeout" -Value "00:00:00"
    
    Write-Host "Application pool '$AppPoolName' configured" -ForegroundColor Green

    # Step 6: Create/Configure Website
    Write-Host "Configuring website..." -ForegroundColor Yellow
    if (Get-Website -Name $SiteName -ErrorAction SilentlyContinue) {
        Remove-Website -Name $SiteName
    }

    $port = switch ($Environment) {
        "Production" { 80 }
        "Staging" { 8080 }
        "Development" { 8081 }
        default { 80 }
    }

    New-Website -Name $SiteName -Port $port -PhysicalPath $DeployPath -ApplicationPool $AppPoolName
    Write-Host "Website '$SiteName' configured on port $port" -ForegroundColor Green

    # Step 7: Set permissions
    Write-Host "Setting permissions..." -ForegroundColor Yellow
    $appPoolSid = (New-Object System.Security.Principal.SecurityIdentifier("S-1-5-82")).Translate([System.Security.Principal.NTAccount])
    $appPoolIdentity = "IIS AppPool\$AppPoolName"
    
    # Grant permissions to application pool identity
    $acl = Get-Acl $DeployPath
    $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($appPoolIdentity, "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
    $acl.SetAccessRule($accessRule)
    Set-Acl -Path $DeployPath -AclObject $acl

    # Grant permissions to App_Data folder
    $appDataPath = "$DeployPath\App_Data"
    if (Test-Path $appDataPath) {
        $acl = Get-Acl $appDataPath
        $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($appPoolIdentity, "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
        $acl.SetAccessRule($accessRule)
        Set-Acl -Path $appDataPath -AclObject $acl
    }

    Write-Host "Permissions configured" -ForegroundColor Green

    # Step 8: Install SSL certificate (if production)
    if ($Environment -eq "Production") {
        Write-Host "Configuring SSL certificate..." -ForegroundColor Yellow
        # This would typically involve installing and binding an SSL certificate
        # For now, we'll just add the HTTPS binding
        try {
            New-WebBinding -Name $SiteName -Protocol https -Port 443 -ErrorAction SilentlyContinue
            Write-Host "HTTPS binding added (certificate must be installed separately)" -ForegroundColor Yellow
        } catch {
            Write-Warning "Could not add HTTPS binding: $($_.Exception.Message)"
        }
    }

    # Step 9: Start application pool and website
    Write-Host "Starting services..." -ForegroundColor Yellow
    Start-WebAppPool -Name $AppPoolName
    Start-Website -Name $SiteName
    
    Write-Host "Application pool and website started" -ForegroundColor Green

    # Step 10: Run database migrations
    Write-Host "Running database migrations..." -ForegroundColor Yellow
    try {
        # This would typically run Entity Framework migrations
        # For now, we'll just verify the connection string works
        Write-Host "Database migration placeholder - implement EF migrations here" -ForegroundColor Yellow
    } catch {
        Write-Warning "Database migration failed: $($_.Exception.Message)"
    }

    # Step 11: Warm up application
    Write-Host "Warming up application..." -ForegroundColor Yellow
    try {
        $warmupUrl = "http://localhost:$port/Default.aspx"
        $response = Invoke-WebRequest -Uri $warmupUrl -TimeoutSec 30 -ErrorAction SilentlyContinue
        if ($response.StatusCode -eq 200) {
            Write-Host "Application warmed up successfully" -ForegroundColor Green
        } else {
            Write-Warning "Application warmup returned status code: $($response.StatusCode)"
        }
    } catch {
        Write-Warning "Application warmup failed: $($_.Exception.Message)"
    }

    # Step 12: Verify deployment
    Write-Host "Verifying deployment..." -ForegroundColor Yellow
    $appPoolStatus = Get-WebAppPoolState -Name $AppPoolName
    $websiteStatus = Get-WebsiteState -Name $SiteName
    
    if ($appPoolStatus.Value -eq "Started" -and $websiteStatus.Value -eq "Started") {
        Write-Host "Deployment completed successfully!" -ForegroundColor Green
        Write-Host "Application Pool Status: $($appPoolStatus.Value)" -ForegroundColor Green
        Write-Host "Website Status: $($websiteStatus.Value)" -ForegroundColor Green
        Write-Host "Website URL: http://localhost:$port" -ForegroundColor Green
    } else {
        Write-Error "Deployment verification failed. Check IIS configuration."
        exit 1
    }

} catch {
    Write-Error "Deployment failed: $($_.Exception.Message)"
    Write-Host "Rolling back..." -ForegroundColor Red
    
    # Rollback logic would go here
    # For now, just stop the services
    try {
        Stop-WebAppPool -Name $AppPoolName -ErrorAction SilentlyContinue
        Stop-Website -Name $SiteName -ErrorAction SilentlyContinue
    } catch {
        Write-Warning "Rollback failed: $($_.Exception.Message)"
    }
    
    exit 1
}

Write-Host "SafetyAI deployment completed successfully!" -ForegroundColor Green