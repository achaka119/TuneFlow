"""Run against an already running TuneFlow server. Python standard library only."""
import urllib.request, json, sys
base=sys.argv[1] if len(sys.argv)>1 else 'http://127.0.0.1:5080'
def get(path,headers=None):
 return urllib.request.urlopen(urllib.request.Request(base+path,headers=headers or {}),timeout=10)
assert json.load(get('/health'))['status']=='ok'
assert len(json.load(get('/api/tracks')))==6
assert json.load(get('/api/tracks?q=MIDNIGHT'))[0]['id']==1
assert len(json.load(get('/api/tracks?genre=Ambient')))==2
assert len(json.load(get('/api/tracks?q=missing')))==0
assert all(t['mood']=='Focus' for t in json.load(get('/api/mix?mood=Focus'))[:2])
for i in range(1,7):
 with get(f'/audio/{i}.wav',{'Range':'bytes=0-99'}) as r:
  assert r.status==206
  assert len(r.read())==100
  assert r.headers['Content-Type']=='audio/wav'
  assert r.headers['Accept-Ranges']=='bytes'
  assert r.headers['Cache-Control']=='public,max-age=86400'
print('PASS: 6 catalog/health checks and 6 audio range/cache checks')
