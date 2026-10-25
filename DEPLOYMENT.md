# Deployment Guide

## Local Development

### Prerequisites
- .NET 8.0 SDK
- Godot 4.2+
- PostgreSQL 14+
- Docker (optional, for containerized setup)

### Setup with Docker Compose (Recommended)

```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f server

# Stop services
docker-compose down
```

### Manual Setup

#### 1. Start PostgreSQL
```bash
# Using Docker
docker run -d \
  -e POSTGRES_DB=arena_game \
  -e POSTGRES_PASSWORD=postgres \
  -p 5432:5432 \
  postgres:14-alpine

# Or use local PostgreSQL
psql -U postgres -c "CREATE DATABASE arena_game;"
```

#### 2. Start Server
```bash
cd server
dotnet restore
dotnet build
dotnet run
```

Server runs on:
- HTTP: http://localhost:5000
- HTTPS: https://localhost:5001
- Swagger UI: http://localhost:5000/swagger

#### 3. Open Client
```bash
# Using Godot Editor
godot --path ./client/

# Or export and run
godot --export-release Windows client/exports/arena.exe
```

## Running Tests

### Unit Tests
```bash
cd server
dotnet test
```

### Load Tests
```bash
cd tests
dotnet run LoadTest.cs
```

Expected output:
- 100 connection spike: ~50ms avg
- 200 player lobby throughput: 50 matches in <5s
- Match start latency: p95 < 3.5s
- Arena gameplay: 1000+ msg/sec
- Reconnection: < 500ms avg

### Cheat Detection Tests
```bash
cd tests
dotnet run CheatTests.cs
```

## Production Deployment

### Option 1: Docker (Recommended)

#### Build Image
```bash
docker build -t arena-server:1.0 -f server/Dockerfile ./server
```

#### Push to Registry
```bash
docker tag arena-server:1.0 your-registry/arena-server:1.0
docker push your-registry/arena-server:1.0
```

#### Deploy on Kubernetes
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: arena-server
spec:
  replicas: 3
  selector:
    matchLabels:
      app: arena-server
  template:
    metadata:
      labels:
        app: arena-server
    spec:
      containers:
      - name: arena-server
        image: your-registry/arena-server:1.0
        ports:
        - containerPort: 5000
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: db-connection
              key: connection-string
        resources:
          requests:
            cpu: "500m"
            memory: "512Mi"
          limits:
            cpu: "1000m"
            memory: "1Gi"
        livenessProbe:
          httpGet:
            path: /health
            port: 5000
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health
            port: 5000
          initialDelaySeconds: 5
          periodSeconds: 5
---
apiVersion: v1
kind: Service
metadata:
  name: arena-server-service
spec:
  type: LoadBalancer
  selector:
    app: arena-server
  ports:
  - protocol: TCP
    port: 80
    targetPort: 5000
```

#### Deploy PostgreSQL
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: db-connection
type: Opaque
stringData:
  connection-string: "Host=postgres-service;Port=5432;Database=arena_game;Username=postgres;Password=YOUR_PASSWORD"
---
apiVersion: bitnami.com/v1alpha1
kind: PostgreSQL
metadata:
  name: arena-postgres
spec:
  database: arena_game
  auth:
    password: YOUR_PASSWORD
  persistence:
    size: 100Gi
```

### Option 2: AWS EC2

#### 1. Launch Instance
```bash
# Ubuntu 22.04 LTS
aws ec2 run-instances \
  --image-id ami-0c55b159cbfafe1f0 \
  --instance-type t3.medium \
  --key-name your-key-pair
```

#### 2. Install Dependencies
```bash
sudo apt update
sudo apt install -y dotnet-sdk-8.0 postgresql postgresql-contrib docker.io

# Add user to docker group
sudo usermod -aG docker ubuntu
```

#### 3. Deploy Server
```bash
git clone https://github.com/tasdus-117/SE2026-T17
cd SE2026-T17

# Start with docker-compose
docker-compose up -d

# Or manual
cd server
dotnet publish -c Release
nohup dotnet /path/to/ArenaServer.dll > server.log 2>&1 &
```

#### 4. Setup Reverse Proxy (Nginx)
```nginx
upstream arena_server {
    server localhost:5000;
}

server {
    listen 80;
    server_name your-domain.com;

    location / {
        proxy_pass http://arena_server;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # WebSocket support
    location /hubs/ {
        proxy_pass http://arena_server;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_buffering off;
    }
}
```

#### 5. SSL Certificate (Let's Encrypt)
```bash
sudo apt install certbot python3-certbot-nginx
sudo certbot certonly --nginx -d your-domain.com
```

### Option 3: Azure App Service

#### 1. Create App Service
```bash
az webapp create \
  --resource-group myResourceGroup \
  --plan myAppServicePlan \
  --name arena-server-app \
  --runtime "DOTNETCORE|8.0"
```

#### 2. Configure Database Connection
```bash
az webapp config appsettings set \
  --resource-group myResourceGroup \
  --name arena-server-app \
  --settings \
    ConnectionStrings__DefaultConnection="Host=postgres.database.azure.com;Database=arena_game;Username=admin@postgres;Password=YOUR_PASSWORD" \
    ASPNETCORE_ENVIRONMENT=Production
```

#### 3. Deploy Code
```bash
cd server
dotnet publish -c Release -o ./publish
az webapp deployment source config-zip \
  --resource-group myResourceGroup \
  --name arena-server-app \
  --src-path publish.zip
```

## Configuration

### Environment Variables

```bash
# Server
export ASPNETCORE_ENVIRONMENT=Production
export ASPNETCORE_URLS=https://0.0.0.0:5001

# Database
export ConnectionStrings__DefaultConnection="Host=postgres;Port=5432;Database=arena_game;Username=postgres;Password=postgres"

# Game Settings
export GameSettings__MaxPlayersPerArena=4
export GameSettings__MatchDurationSeconds=600
export GameSettings__TickRate=60

# Networking
export Networking__SimulateLatency=false
export Networking__SimulatedLatencyMs=50
```

### appsettings.Production.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft": "Error"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=POSTGRES_HOST;Port=5432;Database=arena_game;Username=POSTGRES_USER;Password=POSTGRES_PASSWORD"
  },
  "GameSettings": {
    "MaxPlayersPerArena": 4,
    "MatchDurationSeconds": 600
  },
  "Endpoints": {
    "Http": "http://0.0.0.0:5000",
    "Https": "https://0.0.0.0:5001"
  }
}
```

## Monitoring

### Logging
```bash
# View server logs
docker-compose logs -f server

# View application logs
tail -f /var/log/arena-server/application.log
```

### Health Check
```bash
curl http://localhost:5000/health
```

### Metrics (Prometheus)
```bash
# Configure Prometheus to scrape metrics
global:
  scrape_interval: 15s

scrape_configs:
  - job_name: 'arena-server'
    static_configs:
      - targets: ['localhost:5000']
    metrics_path: '/metrics'
```

## Backup & Recovery

### Database Backup
```bash
# Backup
pg_dump -U postgres arena_game > arena_game_backup.sql

# Restore
psql -U postgres arena_game < arena_game_backup.sql
```

### Automated Backups (Cron)
```bash
# Daily backup at 2 AM
0 2 * * * pg_dump -U postgres arena_game > /backups/arena_game_$(date +%Y%m%d).sql
```

## Troubleshooting

### Server Won't Start
```bash
# Check logs
docker-compose logs server

# Verify database connection
psql -h localhost -U postgres -d arena_game -c "SELECT version();"

# Check port availability
lsof -i :5000
```

### Players Can't Connect
```bash
# Test WebSocket
wscat -c ws://localhost:5000/hubs/lobby

# Check firewall
sudo ufw status
sudo ufw allow 5000
```

### High Database Latency
```bash
# Check connections
psql -c "SELECT count(*) FROM pg_stat_activity;"

# Optimize queries
EXPLAIN ANALYZE SELECT * FROM players WHERE username = 'test';

# Check indexes
SELECT * FROM pg_stat_user_indexes;
```

## Performance Tuning

### PostgreSQL
```sql
-- Connection pooling (add to postgresql.conf)
max_connections = 200
shared_buffers = 256MB
work_mem = 16MB

-- Index optimization
CREATE INDEX idx_players_username ON players(username);
CREATE INDEX idx_sessions_token ON sessions(token);
CREATE INDEX idx_matches_arena_id ON matches(arena_id);
```

### Application
- Increase server replicas if CPU > 80%
- Add caching layer (Redis) for player stats
- Use connection pooling
- Monitor memory usage

## Rollback Procedure

```bash
# Tag current version
git tag -a v1.0.0 -m "Production release"
git push origin v1.0.0

# Revert if needed
git checkout v0.9.0
docker-compose down
docker-compose up -d

# Database rollback (if migrations)
dotnet ef database update 0
```

## Support & Maintenance

- **On-call:** Team member available 24/7
- **SLA:** 99.5% uptime target
- **Incident response:** < 15 minutes
- **Maintenance window:** Sunday 2 AM UTC (1 hour)
