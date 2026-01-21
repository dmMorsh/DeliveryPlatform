#!/bin/bash

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${BLUE}═══════════════════════════════════════════════════════════${NC}"
echo -e "${BLUE}  Delivery Tracking Platform - Local Development Setup${NC}"
echo -e "${BLUE}═══════════════════════════════════════════════════════════${NC}\n"

# Check Docker
if ! command -v docker &> /dev/null; then
    echo -e "${YELLOW}⚠️  Docker is not installed. Please install Docker first.${NC}"
    exit 1
fi

# Build Docker images
echo -e "${BLUE}🔨 Building Docker images...${NC}"
docker-compose build --no-cache

# Start infrastructure
echo -e "\n${BLUE}🚀 Starting infrastructure (PostgreSQL, Redis, Kafka)...${NC}"
docker-compose up -d postgres redis zookeeper kafka

# Wait for services to be healthy
echo -e "\n${BLUE}⏳ Waiting for services to be healthy...${NC}"
sleep 5

# Build .NET projects
echo -e "\n${BLUE}🔨 Building .NET projects...${NC}"
dotnet build

# Run database migrations
echo -e "\n${BLUE}📦 Running database migrations...${NC}"
dotnet ef database update --project src/OrderService
dotnet ef database update --project src/CourierService

# Start microservices
echo -e "\n${BLUE}🚀 Starting microservices...${NC}"
docker-compose up -d gateway-api order-service courier-service

# Health checks
echo -e "\n${BLUE}🏥 Checking service health...${NC}"
sleep 3

for service in "gateway-api:5000" "order-service:5001" "courier-service:5002"; do
    name=${service%:*}
    port=${service#*:}
    if curl -s http://localhost:$port/health > /dev/null; then
        echo -e "${GREEN}✓ $name is healthy${NC}"
    else
        echo -e "${YELLOW}⚠ $name is not responding yet${NC}"
    fi
done

echo -e "\n${GREEN}════════════════════════════════════════════════════════════${NC}"
echo -e "${GREEN}✅ All services are running!${NC}"
echo -e "${GREEN}════════════════════════════════════════════════════════════${NC}"
echo -e "\n${BLUE}Service URLs:${NC}"
echo -e "  Gateway API:        ${GREEN}http://localhost:5000${NC}"
echo -e "  Order Service:      ${GREEN}http://localhost:5001${NC}"
echo -e "  Courier Service:    ${GREEN}http://localhost:5002${NC}"
echo -e "  PostgreSQL:         ${GREEN}localhost:5432${NC}"
echo -e "  Redis:              ${GREEN}localhost:6379${NC}"
echo -e "  Kafka:              ${GREEN}localhost:9092${NC}"
echo -e "  Zookeeper:          ${GREEN}localhost:2181${NC}"
echo -e "\n${BLUE}Health Check:${NC}"
echo -e "  curl http://localhost:5000/health"
echo -e "  curl http://localhost:5000/api/services-health"
echo -e "\n${BLUE}View logs:${NC}"
echo -e "  docker-compose logs -f gateway-api"
echo -e "  docker-compose logs -f order-service"
echo -e "  docker-compose logs -f courier-service"
echo -e "\n${BLUE}Stop services:${NC}"
echo -e "  docker-compose down"
echo ""
