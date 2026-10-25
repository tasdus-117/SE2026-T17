package auth

import "time"

// SecurityContext is the verified identity attached to an inbound request.
// Callers must treat Roles as read-only; middleware makes a defensive copy.
type SecurityContext struct {
	UserID string
	SessionID string
	Roles []string
	Expiry time.Time
	IsAuthenticated bool
}

func (s SecurityContext) HasRole(role string) bool {
	for _, r := range s.Roles { if r == role { return true } }
	return false
}
