window.tune = (() => {
 let audio, ref, last=0, objectUrl, playVersion=0;
 const report=(method,...args)=>ref?.invokeMethodAsync(method,...args).catch(()=>{});
 const db=()=>new Promise((resolve,reject)=>{const r=indexedDB.open('tuneflow.audio.v1',1);r.onupgradeneeded=()=>r.result.createObjectStore('songs');r.onsuccess=()=>resolve(r.result);r.onerror=()=>reject(new Error('Browser audio storage is unavailable.'));});
 async function store(mode,key,value){const d=await db();try{return await new Promise((resolve,reject)=>{const tx=d.transaction('songs',mode),s=tx.objectStore('songs');let r=value===undefined?s.get(key):value===null?s.delete(key):s.put(value,key);tx.oncomplete=()=>resolve(r.result);tx.onerror=()=>reject(new Error('Could not save audio. Browser storage may be full.'));tx.onabort=()=>reject(new Error('Audio storage was interrupted.'));});}finally{d.close();}}
 async function inspect(file){return await new Promise((resolve,reject)=>{const a=new Audio(),u=URL.createObjectURL(file);const end=(error)=>{clearTimeout(timer);a.onloadedmetadata=null;a.onerror=null;a.removeAttribute('src');a.load();URL.revokeObjectURL(u);if(error)reject(new Error(error));};const timer=setTimeout(()=>end('Audio validation timed out. Try another file.'),15000);a.onloadedmetadata=()=>{const n=a.duration;if(!Number.isFinite(n)||n<=0||n>86400){end('Choose an audio file shorter than 24 hours.');return;}end();resolve(n);};a.onerror=()=>end('This file is not playable audio in your browser. Try MP3 or WAV.');a.preload='metadata';a.src=u;});}
 return {
 init(r){ref=r;audio=new Audio();audio.preload='metadata';audio.addEventListener('timeupdate',()=>{if(Date.now()-last>750){last=Date.now();report('Progress',audio.currentTime)}});audio.addEventListener('ended',()=>report('Ended'));audio.addEventListener('play',()=>report('Playback',true));audio.addEventListener('pause',()=>report('Playback',false));audio.addEventListener('error',()=>report('AudioError'));},
 async importTrack(inputId,key){const file=document.getElementById(inputId)?.files?.[0];if(!file)throw new Error('Choose an audio file first.');if(!/\.(mp3|wav|m4a|ogg)$/i.test(file.name))throw new Error('Supported files: MP3, WAV, M4A and OGG.');if(file.size===0||file.size>20*1024*1024)throw new Error('Choose a nonempty audio file no larger than 20 MB.');const duration=await inspect(file);await store('readwrite',key,file);return duration;},
 async removeImport(key){await store('readwrite',key,null);},
 async play(url,v){const version=++playVersion;audio.pause();if(objectUrl){URL.revokeObjectURL(objectUrl);objectUrl=null;}let src=url;if(url.startsWith('local:')){const blob=await store('readonly',url.slice(6));if(!blob)throw new Error('This audio file is no longer in browser storage. Import it again.');if(version!==playVersion)return;src=objectUrl=URL.createObjectURL(blob);}audio.src=src;audio.volume=v;await audio.play();},
 async toggle(){if(audio.paused)await audio.play();else audio.pause()},
 seek(n){if(audio.duration)audio.currentTime=Math.max(0,Math.min(n,audio.duration))},
 volume(n){audio.volume=Math.max(0,Math.min(1,n))},
 save(s){localStorage.setItem('tuneflow.library.v1',s)}, load(){return localStorage.getItem('tuneflow.library.v1')},
 dispose(){playVersion++;if(audio){audio.pause();audio.removeAttribute('src');audio.load()}if(objectUrl){URL.revokeObjectURL(objectUrl);objectUrl=null;}ref=null}
 };
})();
