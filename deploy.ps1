# Production Deployment Script for Exchange Service API (PowerShell)
# This script handles the complete deployment process including health checks

param(
    [Parameter(Position=0)]
    [ValidateSet("deploy", "rollback", "logs", "status", "stop", "restart", "update-config", "backup", "test", "help")]
    [string]$Command = "deploy"
)

# Configuration
$APP_NAME = "exchangeservice-api"
$IMAGE_NAME = "exchangeservice"
$CONTAINER_NAME = "exchangeservice-api"
$HEALTH_CHECK_URL = "http://localhost:8080/health"
$MAX_HEALTH_CHECK_ATTEMPTS = 30
$HEALTH_CHECK_INTERVAL = 5

# Logging functions
function Write-Info {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Green
}

function Write-Warn {
    param([string]$Message)
    Write-Host "[WARN] $Message" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

# Function to check if a command exists
function Test-Command {
    param([string]$CommandName)
    return $null -ne (Get-Command $CommandName -ErrorAction SilentlyContinue)
}

# Function to wait for health check
function Wait-ForHealth {
    param(
        [string]$Url,
        [int]$MaxAttempts,
        [int]$Interval
    )
    
    Write-Info "Waiting for application health check at $Url..."
    
    for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
        try {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5
            if ($response.StatusCode -eq 200) {
                Write-Info "Health check passed on attempt $attempt"
                return $true
            }
        }
        catch {
            # Health check failed, continue
        }
        
        Write-Warn "Health check failed (attempt $attempt/$MaxAttempts), retrying in ${Interval}s..."
        Start-Sleep -Seconds $Interval
    }
    
    Write-Error "Health check failed after $MaxAttempts attempts"
    return $false
}

# Function to perform deployment
function Deploy {
    Write-Info "Starting deployment of $APP_NAME..."
    
    # Check prerequisites
    if (-not (Test-Command "docker")) {
        Write-Error "Docker is not installed or not in PATH"
        exit 1
    }
    
    if (-not (Test-Command "docker-compose")) {
        Write-Error "Docker Compose is not installed or not in PATH"
        exit 1
    }
    
    # Stop existing containers
    Write-Info "Stopping existing containers..."
    try { docker-compose down } catch { }
    
    # Pull latest images (if using registry)
    Write-Info "Pulling latest images..."
    try { 
        docker-compose pull 
    } 
    catch { 
        Write-Warn "Failed to pull images, will build locally" 
    }
    
    # Build and start services
    Write-Info "Building and starting services..."
    docker-compose up -d --build
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to start services"
        exit 1
    }
    
    # Wait for application to be healthy
    if (Wait-ForHealth $HEALTH_CHECK_URL $MAX_HEALTH_CHECK_ATTEMPTS $HEALTH_CHECK_INTERVAL) {
        Write-Info "Deployment successful! Application is healthy."
        
        # Show deployment status
        Write-Host ""
        Write-Info "Deployment Summary:"
        Write-Host "  - API URL: http://localhost:8080"
        Write-Host "  - API Documentation: http://localhost:8080/api-docs"
        Write-Host "  - Health Check: http://localhost:8080/health"
        Write-Host "  - Health UI: http://localhost:8080/health-ui"
        Write-Host "  - Metrics: http://localhost:8080/metrics"
        Write-Host "  - Redis Commander: http://localhost:8082"
        Write-Host ""
        
        # Show running containers
        Write-Info "Running containers:"
        docker-compose ps
    }
    else {
        Write-Error "Deployment failed! Application is not healthy."
        
        # Show logs for debugging
        Write-Info "Container logs:"
        docker-compose logs --tail=50
        
        exit 1
    }
}

# Function to rollback deployment
function Rollback {
    Write-Warn "Rolling back deployment..."
    docker-compose down
    # In a real scenario, you would restore the previous version here
    Write-Info "Rollback completed"
}

# Function to show logs
function Show-Logs {
    docker-compose logs -f
}

# Function to show status
function Show-Status {
    Write-Info "Service Status:"
    docker-compose ps
    Write-Host ""
    
    Write-Info "Health Check Status:"
    try {
        $response = Invoke-WebRequest -Uri $HEALTH_CHECK_URL -UseBasicParsing -TimeoutSec 5
        if ($response.StatusCode -eq 200) {
            Write-Host "✓ Healthy" -ForegroundColor Green
        }
        else {
            Write-Host "✗ Unhealthy" -ForegroundColor Red
        }
    }
    catch {
        Write-Host "✗ Unhealthy" -ForegroundColor Red
    }
}

# Function to stop services
function Stop-Services {
    Write-Info "Stopping all services..."
    docker-compose down
    Write-Info "Services stopped"
}

# Function to restart services
function Restart-Services {
    Write-Info "Restarting services..."
    docker-compose restart
    
    if (Wait-ForHealth $HEALTH_CHECK_URL $MAX_HEALTH_CHECK_ATTEMPTS $HEALTH_CHECK_INTERVAL) {
        Write-Info "Services restarted successfully"
    }
    else {
        Write-Error "Services failed to restart properly"
        exit 1
    }
}

# Function to update configuration
function Update-Config {
    Write-Info "Updating configuration and restarting services..."
    docker-compose down
    docker-compose up -d --build
    
    if (Wait-ForHealth $HEALTH_CHECK_URL $MAX_HEALTH_CHECK_ATTEMPTS $HEALTH_CHECK_INTERVAL) {
        Write-Info "Configuration updated successfully"
    }
    else {
        Write-Error "Configuration update failed"
        exit 1
    }
}

# Function to backup data
function Backup-Data {
    $backupDir = "./backups/$(Get-Date -Format 'yyyyMMdd_HHmmss')"
    New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
    
    Write-Info "Creating backup in $backupDir..."
    
    # Backup Redis data if container is running
    $redisContainer = docker-compose ps -q redis
    if ($redisContainer) {
        try {
            docker-compose exec redis redis-cli BGSAVE
            Start-Sleep -Seconds 5  # Wait for background save to complete
            docker cp "${redisContainer}:/data/dump.rdb" "$backupDir/"
            Write-Info "Redis data backed up"
        }
        catch {
            Write-Warn "Failed to backup Redis data"
        }
    }
    
    # Backup configuration files
    try {
        if (Test-Path "./config") { Copy-Item -Recurse "./config" "$backupDir/" }
        if (Test-Path "docker-compose.yml") { Copy-Item "docker-compose.yml" "$backupDir/" }
        if (Test-Path ".env") { Copy-Item ".env" "$backupDir/" }
    }
    catch {
        Write-Warn "Some configuration files could not be backed up"
    }
    
    Write-Info "Backup completed in $backupDir"
}

# Function to run performance test
function Performance-Test {
    Write-Info "Running basic performance test..."
    
    $testUrl = "http://localhost:8080/v1/Exchange/ExchangeService"
    $testData = @{
        amount = 100
        inputCurrency = "USD"
        outputCurrency = "EUR"
    } | ConvertTo-Json
    
    $successCount = 0
    $totalRequests = 10
    
    Write-Info "Sending $totalRequests test requests..."
    
    for ($i = 1; $i -le $totalRequests; $i++) {
        try {
            $response = Invoke-WebRequest -Uri $testUrl -Method POST -Body $testData -ContentType "application/json" -UseBasicParsing -TimeoutSec 10
            if ($response.StatusCode -eq 200) {
                $successCount++
            }
        }
        catch {
            # Request failed
        }
        Write-Host "." -NoNewline
    }
    Write-Host ""
    
    Write-Info "Performance test completed: $successCount/$totalRequests requests successful"
}

# Function to show help
function Show-Help {
    Write-Host "Usage: .\deploy.ps1 [command]"
    Write-Host ""
    Write-Host "Commands:"
    Write-Host "  deploy        Deploy the application (default)"
    Write-Host "  rollback      Rollback the deployment"
    Write-Host "  logs          Show container logs"
    Write-Host "  status        Show service status"
    Write-Host "  stop          Stop all services"
    Write-Host "  restart       Restart all services"
    Write-Host "  update-config Update configuration and restart"
    Write-Host "  backup        Backup data and configuration"
    Write-Host "  test          Run basic performance test"
    Write-Host "  help          Show this help message"
}

# Main script logic
switch ($Command) {
    "deploy" { Deploy }
    "rollback" { Rollback }
    "logs" { Show-Logs }
    "status" { Show-Status }
    "stop" { Stop-Services }
    "restart" { Restart-Services }
    "update-config" { Update-Config }
    "backup" { Backup-Data }
    "test" { Performance-Test }
    "help" { Show-Help }
    default {
        Write-Error "Unknown command: $Command"
        Write-Host "Use '.\deploy.ps1 help' for usage information"
        exit 1
    }
}
