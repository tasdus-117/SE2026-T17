package game

import "errors"

type Player struct { ID string; X, Y, Z float64; Health int; Weapon string }
func (p *Player) Move(dx,dy,dz float64, dt, maxSpeed, bound float64) error {
	if p == nil || !finite(dx)||!finite(dy)||!finite(dz)||dt < 0 || maxSpeed <= 0 || bound <= 0 { return errors.New("invalid movement") }
	n := dx*dx+dy*dy+dz*dz; if n > 1.001*1.001 { return errors.New("movement exceeds speed limit") }
	p.X=mathMax(-bound,mathMin(bound,p.X+dx*maxSpeed*dt));p.Y=mathMax(-bound,mathMin(bound,p.Y+dy*maxSpeed*dt));p.Z=mathMax(-bound,mathMin(bound,p.Z+dz*maxSpeed*dt));return nil
}
func mathMin(a,b float64)float64{if a<b{return a};return b};func mathMax(a,b float64)float64{if a>b{return a};return b}
