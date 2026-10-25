package game

import (
	"strconv"
	"sync"
	"game3d/internal/idem"
)

type BotTarget struct { Health int }
type Arena struct { mu sync.Mutex; Target BotTarget; pickupAvailable bool; inventory map[string]string; auditRecords int }
func NewArena() *Arena { return &Arena{Target:BotTarget{Health:10},pickupAvailable:true,inventory:make(map[string]string)} }
// CollectWeapon is atomic: an arena pickup can be assigned to only one player.
func (a *Arena) CollectWeapon(playerID string) (bool,string) {
	a.mu.Lock();defer a.mu.Unlock();if playerID=="" {return false,""};if weapon,ok:=a.inventory[playerID];ok{return false,weapon};if !a.pickupAvailable{return false,""};a.pickupAvailable=false;a.inventory[playerID]="rifle";a.auditRecords++;return true,"rifle"
}
func (a *Arena) Inventory(playerID string) string {a.mu.Lock();defer a.mu.Unlock();return a.inventory[playerID]}
func (a *Arena) AuditRecords() int {a.mu.Lock();defer a.mu.Unlock();return a.auditRecords}
func (a *Arena) CollectWeaponIdempotent(guard *idem.IdempotencyGuard,key,playerID string)(bool,string,error){
	status,response,err:=guard.AcquireOrGet(key);if err!=nil{return false,"",err};if status==idem.Completed{return len(response)>0&&response[0]=='1',string(response[2:]),nil}
	awarded,weapon:=a.CollectWeapon(playerID);result:="0|"+weapon;if awarded{result="1|"+weapon};if err:=guard.Complete(key,[]byte(result));err!=nil{guard.Abandon(key);return false,"",err};return awarded,weapon,nil
}
func (a *Arena) PunchIdempotent(guard *idem.IdempotencyGuard,key string)(int,error){
	status,response,err:=guard.AcquireOrGet(key);if err!=nil{return 0,err};if status==idem.Completed{health,parseErr:=strconv.Atoi(string(response));return health,parseErr}
	a.mu.Lock();if a.Target.Health>0{a.Target.Health--;a.auditRecords++};health:=a.Target.Health;a.mu.Unlock();if err:=guard.Complete(key,[]byte(strconv.Itoa(health)));err!=nil{guard.Abandon(key);return 0,err};return health,nil
}
