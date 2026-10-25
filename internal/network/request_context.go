package network

import "game3d/internal/game"

// RequestContext is the parameter object passed across the authenticated edge
// into authoritative game commands. Before this refactor, adding a field such
// as ClientTimestamp or TraceID required synchronized edits to packet.go,
// player.go, server/main.go, and storage/repository.go. Centralizing request
// metadata here keeps domain command signatures stable as the wire schema grows.
type RequestContext = game.RequestContext
type Command = game.Command
