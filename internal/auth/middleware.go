package auth

import (
	"context"
	"crypto/hmac"
	"crypto/sha256"
	"encoding/base64"
	"errors"
	"strconv"
	"strings"
	"time"
)

var ErrUnauthenticated = errors.New("missing, invalid, or expired authentication token")

type Packet struct { Token string; UserID string; SessionID string; Payload []byte }
type PacketHandler func(context.Context, Packet, SecurityContext) error
type TokenVerifier interface { Verify(context.Context, string) (SecurityContext, error) }

// AuthMiddleware is the edge boundary: the downstream authoritative handler is
// called only after a verifier has established an unexpired identity.
func AuthMiddleware(verifier TokenVerifier, next PacketHandler) PacketHandler {
	return func(ctx context.Context, packet Packet, _ SecurityContext) error {
		if verifier == nil || next == nil { return ErrUnauthenticated }
		security, err := verifier.Verify(ctx, packet.Token)
		if err != nil || !security.IsAuthenticated || security.UserID == "" || security.SessionID == "" || !security.Expiry.After(time.Now()) { return ErrUnauthenticated }
		security.Roles = append([]string(nil), security.Roles...)
		return next(ctx, packet, security)
	}
}

// StaticTokenVerifier accepts only explicitly configured opaque tokens. For
// signed wire tokens use HMACTokenVerifier below.
type StaticTokenVerifier struct { Tokens map[string]SecurityContext }
func (v StaticTokenVerifier) Verify(_ context.Context, token string) (SecurityContext, error) {
	if v.Tokens != nil { if s, ok := v.Tokens[token]; ok && s.Expiry.After(time.Now()) { s.IsAuthenticated = true; return s, nil } }
	return SecurityContext{}, ErrUnauthenticated
}

// HMACTokenVerifier validates compact base64url(payload).base64url(HMAC-SHA256)
// bearer tokens. Payload fields are user:session:expiry:roles.
type HMACTokenVerifier struct{ Secret []byte }
func SignToken(secret []byte,userID,sessionID string,expiry time.Time,roles ...string)string{
	payload:=userID+":"+sessionID+":"+strconv.FormatInt(expiry.Unix(),10)+":"+strings.Join(roles,",")
	encoded:=base64.RawURLEncoding.EncodeToString([]byte(payload));mac:=hmac.New(sha256.New,secret);_,_=mac.Write([]byte(encoded));return encoded+"."+base64.RawURLEncoding.EncodeToString(mac.Sum(nil))
}
func(v HMACTokenVerifier)Verify(_ context.Context,token string)(SecurityContext,error){
	parts:=strings.Split(token,".");if len(parts)!=2||len(v.Secret)<16{return SecurityContext{},ErrUnauthenticated}
	provided,err:=base64.RawURLEncoding.DecodeString(parts[1]);if err!=nil{return SecurityContext{},ErrUnauthenticated};mac:=hmac.New(sha256.New,v.Secret);_,_=mac.Write([]byte(parts[0]));if !hmac.Equal(provided,mac.Sum(nil)){return SecurityContext{},ErrUnauthenticated}
	payload,err:=base64.RawURLEncoding.DecodeString(parts[0]);if err!=nil{return SecurityContext{},ErrUnauthenticated};fields:=strings.Split(string(payload),":");if len(fields)!=4||fields[0]==""||fields[1]==""{return SecurityContext{},ErrUnauthenticated};expiry,err:=strconv.ParseInt(fields[2],10,64);if err!=nil{return SecurityContext{},ErrUnauthenticated};t:=time.Unix(expiry,0);if !t.After(time.Now()){return SecurityContext{},ErrUnauthenticated};roles:=[]string{};if fields[3]!=""{roles=strings.Split(fields[3],",")};return SecurityContext{UserID:fields[0],SessionID:fields[1],Roles:roles,Expiry:t,IsAuthenticated:true},nil
}
