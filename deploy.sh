#!/bin/bash

# Production Deployment Script for Exchange Service API
# This script handles the complete deployment process including health checks

set -e  # Exit on any error

# Configuration
APP_NAME="exchangeservice-api"
IMAGE_NAME="exchangeservice"
CONTAINER_NAME="exchangeservice-api"
HEALTH_CHECK_URL="http://localhost:8080/health"
MAX_HEALTH_CHECK_ATTEMPTS=30
HEALTH_CHECK_INTERVAL=5

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Logging functions
log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to check if a command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Function to wait for health check
wait_for_health() {
    local url=$1
    local max_attempts=$2
    local interval=$3
    local attempt=1

    log_info "Waiting for application health check at $url..."
    
    while [ $attempt -le $max_attempts ]; do
        if curl -f -s "$url" > /dev/null 2>&1; then
            log_info "Health check passed on attempt $attempt"
            return 0
        fi
        
        log_warn "Health check failed (attempt $attempt/$max_attempts), retrying in ${interval}s..."
        sleep $interval
        attempt=$((attempt + 1))
    done
    
    log_error "Health check failed after $max_attempts attempts"
    return 1
}

# Function to perform deployment
deploy() {
    log_info "Starting deployment of $APP_NAME..."
    
    # Check prerequisites
    if ! command_exists docker; then
        log_error "Docker is not installed or not in PATH"
        exit 1
    fi
    
    if ! command_exists docker-compose; then
        log_error "Docker Compose is not installed or not in PATH"
        exit 1
    fi
    
    # Stop existing containers
    log_info "Stopping existing containers..."
    docker-compose down || true
    
    # Pull latest images (if using registry)
    log_info "Pulling latest images..."
    docker-compose pull || log_warn "Failed to pull images, will build locally"
    
    # Build and start services
    log_info "Building and starting services..."
    docker-compose up -d --build
    
    # Wait for application to be healthy
    if wait_for_health "$HEALTH_CHECK_URL" $MAX_HEALTH_CHECK_ATTEMPTS $HEALTH_CHECK_INTERVAL; then
        log_info "Deployment successful! Application is healthy."
        
        # Show deployment status
        echo
        log_info "Deployment Summary:"
        echo "  - API URL: http://localhost:8080"
        echo "  - API Documentation: http://localhost:8080/api-docs"
        echo "  - Health Check: http://localhost:8080/health"
        echo "  - Health UI: http://localhost:8080/health-ui"
        echo "  - Metrics: http://localhost:8080/metrics"
        echo "  - Redis Commander: http://localhost:8082"
        echo
        
        # Show running containers
        log_info "Running containers:"
        docker-compose ps
        
    else
        log_error "Deployment failed! Application is not healthy."
        
        # Show logs for debugging
        log_info "Container logs:"
        docker-compose logs --tail=50
        
        exit 1
    fi
}

# Function to rollback deployment
rollback() {
    log_warn "Rolling back deployment..."
    docker-compose down
    # In a real scenario, you would restore the previous version here
    log_info "Rollback completed"
}

# Function to show logs
show_logs() {
    docker-compose logs -f
}

# Function to show status
show_status() {
    log_info "Service Status:"
    docker-compose ps
    echo
    
    log_info "Health Check Status:"
    if curl -f -s "$HEALTH_CHECK_URL" > /dev/null 2>&1; then
        echo -e "${GREEN}✓ Healthy${NC}"
    else
        echo -e "${RED}✗ Unhealthy${NC}"
    fi
}

# Function to stop services
stop_services() {
    log_info "Stopping all services..."
    docker-compose down
    log_info "Services stopped"
}

# Function to restart services
restart_services() {
    log_info "Restarting services..."
    docker-compose restart
    
    if wait_for_health "$HEALTH_CHECK_URL" $MAX_HEALTH_CHECK_ATTEMPTS $HEALTH_CHECK_INTERVAL; then
        log_info "Services restarted successfully"
    else
        log_error "Services failed to restart properly"
        exit 1
    fi
}

# Function to update configuration
update_config() {
    log_info "Updating configuration and restarting services..."
    docker-compose down
    docker-compose up -d --build
    
    if wait_for_health "$HEALTH_CHECK_URL" $MAX_HEALTH_CHECK_ATTEMPTS $HEALTH_CHECK_INTERVAL; then
        log_info "Configuration updated successfully"
    else
        log_error "Configuration update failed"
        exit 1
    fi
}

# Function to backup data
backup_data() {
    local backup_dir="./backups/$(date +%Y%m%d_%H%M%S)"
    mkdir -p "$backup_dir"
    
    log_info "Creating backup in $backup_dir..."
    
    # Backup Redis data if container is running
    if docker-compose ps redis | grep -q "Up"; then
        docker-compose exec redis redis-cli BGSAVE
        sleep 5  # Wait for background save to complete
        docker cp $(docker-compose ps -q redis):/data/dump.rdb "$backup_dir/"
        log_info "Redis data backed up"
    fi
    
    # Backup configuration files
    cp -r ./config "$backup_dir/" 2>/dev/null || true
    cp docker-compose.yml "$backup_dir/" 2>/dev/null || true
    cp .env "$backup_dir/" 2>/dev/null || true
    
    log_info "Backup completed in $backup_dir"
}

# Function to run performance test
performance_test() {
    log_info "Running basic performance test..."
    
    if ! command_exists curl; then
        log_error "curl is required for performance testing"
        exit 1
    fi
    
    local test_url="http://localhost:8080/v1/Exchange/ExchangeService"
    local test_data='{"amount": 100, "inputCurrency": "USD", "outputCurrency": "EUR"}'
    local success_count=0
    local total_requests=10
    
    log_info "Sending $total_requests test requests..."
    
    for i in $(seq 1 $total_requests); do
        if curl -s -o /dev/null -w "%{http_code}" -X POST \
           -H "Content-Type: application/json" \
           -d "$test_data" \
           "$test_url" | grep -q "200"; then
            success_count=$((success_count + 1))
        fi
        echo -n "."
    done
    echo
    
    log_info "Performance test completed: $success_count/$total_requests requests successful"
}

# Main script logic
case "${1:-deploy}" in
    "deploy")
        deploy
        ;;
    "rollback")
        rollback
        ;;
    "logs")
        show_logs
        ;;
    "status")
        show_status
        ;;
    "stop")
        stop_services
        ;;
    "restart")
        restart_services
        ;;
    "update-config")
        update_config
        ;;
    "backup")
        backup_data
        ;;
    "test")
        performance_test
        ;;
    "help"|"-h"|"--help")
        echo "Usage: $0 [command]"
        echo
        echo "Commands:"
        echo "  deploy        Deploy the application (default)"
        echo "  rollback      Rollback the deployment"
        echo "  logs          Show container logs"
        echo "  status        Show service status"
        echo "  stop          Stop all services"
        echo "  restart       Restart all services"
        echo "  update-config Update configuration and restart"
        echo "  backup        Backup data and configuration"
        echo "  test          Run basic performance test"
        echo "  help          Show this help message"
        ;;
    *)
        log_error "Unknown command: $1"
        echo "Use '$0 help' for usage information"
        exit 1
        ;;
esac
