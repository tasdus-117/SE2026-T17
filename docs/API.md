# REST API Documentation

## Base URL
```
http://localhost:5000/api
https://localhost:5001/api (production)
```

## Authentication
Include bearer token in Authorization header:
```
Authorization: Bearer {session_token}
```

## Endpoints

### Players

#### Register Player
```
POST /api/players/register
Content-Type: application/json

Request:
{
  "username": "player_name",
  "email": "player@example.com"
}

Response (201):
{
  "playerId": 1,
  "username": "player_name"
}

Error (400):
{
  "error": "Player already exists"
}
```

#### Login
```
POST /api/players/login
Content-Type: application/json

Request:
{
  "username": "player_name"
}

Response (200):
{
  "playerId": 1,
  "token": "abc123def456..."
}

Error (401):
{
  "error": "Player not found"
}
```

#### Logout
```
POST /api/players/logout
Authorization: Bearer {token}

Response (200):
{
  "message": "Logged out successfully"
}
```

#### Get Player Profile
```
GET /api/players/{playerId}
Authorization: Bearer {token}

Response (200):
{
  "id": 1,
  "username": "player_name",
  "level": 5,
  "experience": 2500,
  "wins": 10,
  "losses": 3,
  "winRate": 0.769,
  "status": "Online",
  "createdAt": "2024-01-15T10:30:00Z"
}

Error (404):
{
  "error": "Player not found"
}
```

#### Get Leaderboard
```
GET /api/players/leaderboard?limit=10
Authorization: Bearer {token}

Response (200):
[
  {
    "rank": 1,
    "id": 1,
    "username": "TopPlayer",
    "level": 50,
    "wins": 150,
    "losses": 20,
    "winRate": 0.882
  },
  {
    "rank": 2,
    "id": 2,
    "username": "SecondPlayer",
    "level": 45,
    "wins": 140,
    "losses": 30,
    "winRate": 0.823
  }
]
```

#### Validate Session
```
POST /api/players/validate-session
Authorization: Bearer {token}

Response (200):
{
  "valid": true,
  "playerId": 1
}

Error (401):
{
  "valid": false
}
```

### Health Check

```
GET /api/health

Response (200):
{
  "status": "ok",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

## SignalR Endpoints

### Lobby Hub
```
WebSocket: ws://localhost:5000/hubs/lobby
```

### Arena Hub
```
WebSocket: ws://localhost:5000/hubs/arena
```

## Error Codes

| Code | Message | Meaning |
|------|---------|---------|
| 200 | OK | Success |
| 201 | Created | Resource created |
| 400 | Bad Request | Invalid input |
| 401 | Unauthorized | Invalid/missing token |
| 404 | Not Found | Resource not found |
| 409 | Conflict | Resource already exists |
| 500 | Internal Server Error | Server error |

## Rate Limiting

Currently not implemented. Recommended for production:
- 100 requests per minute per IP
- 50 requests per minute per player (authenticated)

## Pagination

For list endpoints (future):
```
GET /api/matches?page=1&pageSize=20&sort=date

Response (200):
{
  "data": [...],
  "page": 1,
  "pageSize": 20,
  "total": 100,
  "totalPages": 5
}
```

## Examples

### Complete Flow

#### 1. Register
```bash
curl -X POST http://localhost:5000/api/players/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "newplayer",
    "email": "player@example.com"
  }'

# Response
{
  "playerId": 42,
  "username": "newplayer"
}
```

#### 2. Login
```bash
curl -X POST http://localhost:5000/api/players/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "newplayer"
  }'

# Response
{
  "playerId": 42,
  "token": "kDf8jS9dKsL0pQwEr1tYuI2oP3aSd4fG"
}
```

#### 3. Connect to Lobby
```bash
# WebSocket connection
wscat -c ws://localhost:5000/hubs/lobby

# Send message
{
  "method": "JoinLobby",
  "data": {
    "playerId": 42,
    "playerName": "newplayer"
  }
}
```

#### 4. Get Profile
```bash
curl -X GET http://localhost:5000/api/players/42 \
  -H "Authorization: Bearer kDf8jS9dKsL0pQwEr1tYuI2oP3aSd4fG"

# Response
{
  "id": 42,
  "username": "newplayer",
  "level": 1,
  "experience": 0,
  "wins": 0,
  "losses": 0,
  "winRate": 0.0,
  "status": "InLobby",
  "createdAt": "2024-01-15T10:30:00Z"
}
```

## Future Endpoints

```
GET /api/matches/{matchId}
GET /api/matches?player={playerId}&limit=20
POST /api/players/{playerId}/report
GET /api/stats/leaderboard/by-level
GET /api/stats/weekly-wins
```
