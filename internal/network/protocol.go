package network

import (
	"encoding/binary"
	"errors"
	"math"
	"game3d/internal/game"
)

const (
	packetJoin byte = 1
	packetInput byte = 2
	packetChat byte = 3
	packetStatus byte = 4
	packetPickup byte = 5
	packetPunch byte = 6
	packetWelcome byte = 0x81
	packetSnapshot byte = 0x82
	packetChatReply byte = 0x83
	inputSize = 25
	maxPacket = 1200
)
type decoded struct { kind byte; input game.Input; chat string; token string; idemKey string }
func decode(b []byte) (decoded,error) {
	if len(b)==0 || len(b)>maxPacket { return decoded{},errors.New("invalid packet size") }
	switch b[0] {
	case packetJoin:
		if len(b)<2 || len(b)>258 { return decoded{},errors.New("join requires an authentication token") }; return decoded{kind:packetJoin,token:string(b[1:])},nil
	case packetStatus:
		if len(b)!=1 { return decoded{},errors.New("invalid control packet") }; return decoded{kind:b[0]},nil
	case packetPickup, packetPunch:
		if len(b)<2 || len(b)>65 { return decoded{},errors.New("invalid action idempotency key") }; return decoded{kind:b[0],idemKey:string(b[1:])},nil
	case packetInput:
		if len(b)!=inputSize { return decoded{},errors.New("invalid input length") }
		in:=game.Input{Sequence:binary.BigEndian.Uint32(b[1:5]),ClientNanos:int64(binary.BigEndian.Uint64(b[5:13])),DX:math.Float32frombits(binary.BigEndian.Uint32(b[13:17])),DY:math.Float32frombits(binary.BigEndian.Uint32(b[17:21])),DZ:math.Float32frombits(binary.BigEndian.Uint32(b[21:25]))}
		return decoded{kind:packetInput,input:in},nil
	case packetChat:
		if len(b)<2 || len(b)>258 { return decoded{},errors.New("invalid chat length") }
		return decoded{kind:packetChat,chat:string(b[1:])},nil
	default:return decoded{},errors.New("unknown packet type")
	}
}
func encodeWelcome(id uint32) []byte { b:=make([]byte,5); b[0]=packetWelcome; binary.BigEndian.PutUint32(b[1:],id); return b }
func encodeInputAck(sequence uint32) []byte { b:=make([]byte,5); b[0]=0x85; binary.BigEndian.PutUint32(b[1:],sequence); return b }
func encodeStatus(tick uint64, heap uint64, sessions uint32, idempotentReplays uint64, mutations uint32) []byte { b:=make([]byte,33); b[0]=0x84; binary.BigEndian.PutUint64(b[1:9],tick); binary.BigEndian.PutUint64(b[9:17],heap); binary.BigEndian.PutUint32(b[17:21],sessions); binary.BigEndian.PutUint64(b[21:29],idempotentReplays);binary.BigEndian.PutUint32(b[29:33],mutations); return b }
func encodeSnapshot(s game.Snapshot) []byte {
	n:=len(s.Entities); if n>73 { n=73 }
	b:=make([]byte,1+8+4+4+8+2+n*16); b[0]=packetSnapshot
	binary.BigEndian.PutUint64(b[1:9],s.Tick); binary.BigEndian.PutUint32(b[9:13],s.SelfID); binary.BigEndian.PutUint32(b[13:17],s.Ack); binary.BigEndian.PutUint64(b[17:25],uint64(s.EchoNanos)); binary.BigEndian.PutUint16(b[25:27],uint16(n))
	o:=27; for _,e:=range s.Entities[:n] { binary.BigEndian.PutUint32(b[o:o+4],e.ID); binary.BigEndian.PutUint32(b[o+4:o+8],math.Float32bits(e.X)); binary.BigEndian.PutUint32(b[o+8:o+12],math.Float32bits(e.Y)); binary.BigEndian.PutUint32(b[o+12:o+16],math.Float32bits(e.Z)); o+=16 }; return b
}
func encodeChatReply(text string) []byte { if len(text)>240 { text=text[:240] }; return append([]byte{packetChatReply},[]byte(text)...)}
func encodeActionReply(kind byte,health int,weapon string)[]byte{if len(weapon)>32{weapon=weapon[:32]};b:=make([]byte,6+len(weapon));b[0]=0x86;b[1]=kind;binary.BigEndian.PutUint32(b[2:6],uint32(health));copy(b[6:],weapon);return b}
