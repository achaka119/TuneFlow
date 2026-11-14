import wave, math, struct, random
from pathlib import Path
random.seed(32)
sr=22050; duration=32
for tid,bpm,base in [(1,112,110),(2,72,130.81),(3,84,146.83),(4,128,123.47),(5,64,98),(6,90,164.81)]:
 data=bytearray();beat=60/bpm; scale=[1,1.189207,1.498307,1.781797]
 for i in range(sr*duration):
  t=i/sr; chord=int(t/(beat*4))%4; root=base*[1,0.7937,1.1892,0.8909][chord]
  pad=sum(math.sin(2*math.pi*root*r*t+0.12*math.sin(t*.5)) for r in [1,1.189207,1.498307])/3
  step=int(t/(beat/2)); elapsed=t%(beat/2); freq=root*scale[(step+tid)%4]*2
  bell=(math.sin(2*math.pi*freq*t)+.25*math.sin(2*math.pi*freq*2*t))*math.exp(-elapsed*7)
  phase=t%beat;kick=math.sin(2*math.pi*(48*phase+7*(1-math.exp(-phase*30))))*math.exp(-phase*16)
  hat=(random.random()*2-1)*math.exp(-(t%(beat/2))*85)
  amp=.26*pad+.17*bell+(.18 if tid in (1,4) else .08)*kick+.025*hat
  fade=min(1,t/1.5,(duration-t)/2);data.extend(struct.pack('<h',int(max(-1,min(1,amp*fade))*28000)))
 with wave.open(str((Path(__file__).resolve().parents[1]/'src/TuneFlow.Web/wwwroot/audio')/f'{tid}.wav'),'wb') as w:w.setnchannels(1);w.setsampwidth(2);w.setframerate(sr);w.writeframes(data)
