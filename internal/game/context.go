package game

import (
	"errors"
	"game3d/internal/auth"
)

var ErrUnauthorized = errors.New("unauthenticated request cannot enter game domain")

// RequestContext is the domain-facing parameter object. network.RequestContext
// aliases this type to preserve the transport package API without a dependency
// cycle between the transport and game packages.
type RequestContext struct { Security auth.SecurityContext; IdemKey string; Timestamp int64; TraceID string; Payload []byte }
type Command interface { Execute(*RequestContext) error }

// Handle is the stable authoritative command entry point.
func Handle(ctx *RequestContext, cmd Command) error { if ctx==nil || cmd==nil || !ctx.Security.IsAuthenticated { return ErrUnauthorized }; return cmd.Execute(ctx) }
