import fs from 'fs';
import { Session } from './rules.mjs';
const id=+process.argv[2], sec=+process.argv[3];
const l=JSON.parse(fs.readFileSync(`Assets/VoxelBloom/Data/Resources/VoxelBloom/Conveyor/level_${String(id).padStart(3,'0')}.json`));
const s=new Session(l); let w=0;
while(s.tick<sec*60&&!s.isWon()){while(w<l.witness.length&&l.witness[w][0]===s.tick)s.place(l.witness[w++][1]);s.step();}
const {w:W,h:H,r:R}=l.board,PU=l.slots*100,a=W-2*R,b=H-2*R,q=Math.PI*R/2,LG=2*a+2*b+4*q;
function at(sp){let g=(((sp%PU)+PU)%PU)*LG/PU;if(g<a)return[R+g,0];g-=a;if(g<q){const f=g/R;return[W-R+R*Math.sin(f),R-R*Math.cos(f)]}g-=q;if(g<b)return[W,R+g];g-=b;if(g<q){const f=g/R;return[W-R+R*Math.cos(f),H-R+R*Math.sin(f)]}g-=q;if(g<a)return[W-R-g,H];g-=a;if(g<q){const f=g/R;return[R-R*Math.sin(f),H-R+R*Math.cos(f)]}g-=q;if(g<b)return[0,H-R-g];g-=b;const f=Math.min(g,q)/R;return[R-R*Math.cos(f),R-R*Math.sin(f)]}
const comp=(st,i)=>{const f=1.6+i*1.7;return st.side==='top'?[st.at,f]:st.side==='right'?[W-f,st.at]:st.side==='bottom'?[st.at,H-f]:[f,st.at]};
console.log(JSON.stringify({id,W,H,R,colors:l.colors,visible:l.visible,tier:l.tier,tierName:l.tierName,completed:s.completed,total:s.total,hopper:s.hopperLeft(),
 cubes:Array.from(s.slot).map((c,k)=>c<0?null:[...at(s.slotPos(k)),c]).filter(Boolean),feeder:at(l.feeder),
 stations:l.stations.map((d,i)=>({side:d.side,cap:d.capacity,lock:s.stations[i].lock,comps:[...Array(d.capacity).keys()].map(k=>comp(d,k)),tray:s.stations[i].tray?{colors:s.stations[i].tray.colors,filled:s.stations[i].tray.filled}:null})),
 queue:s.queue.slice(0,l.visible).map(t=>t.colors),hidden:Math.max(0,s.queue.length-l.visible)}));
