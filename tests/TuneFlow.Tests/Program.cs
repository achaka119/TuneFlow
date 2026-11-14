using TuneFlow.Core;
using System.Text.Json;
int passed=0,failed=0;
void Test(string name,Action f){try{f();Console.WriteLine($"PASS {name}");passed++;}catch(Exception e){Console.WriteLine($"FAIL {name}: {e.Message}");failed++;}}
void Check(bool ok){if(!ok)throw new Exception("Assertion failed");}
void Reject(Action f){try{f();}catch(ArgumentException){return;}throw new Exception("Expected validation error");}
Test("Search ignores case and trims query",()=>Check(Catalog.Search("  MIDNIGHT  ").Single().Id==1));
Test("Search combines genre and query",()=>Check(!Catalog.Search("Midnight","Ambient").Any()));
Test("Empty search returns all tracks",()=>Check(Catalog.Search("").Count()==6));
Test("Unknown query returns no tracks",()=>Check(!Catalog.Search("unknown").Any()));
Test("Mix prioritizes selected mood",()=>Check(Catalog.Mix("Focus",Array.Empty<int>()).Take(2).All(t=>t.Mood=="Focus")));
Test("Mix uses favorite preference within mood",()=>Check(Catalog.Mix("Focus",new[]{5})[0].Id==5));
Test("Mix deterministic and unique",()=>Check(Catalog.Mix("Relax",new[]{3}).Select(t=>t.Id).SequenceEqual(Catalog.Mix("Relax",new[]{3}).Select(t=>t.Id)) && Catalog.Mix("Relax",new[]{3}).Select(t=>t.Id).Distinct().Count()==6));
Test("Favorite toggle is reversible",()=>{var s=new LibraryState();s.ToggleFavorite(1);Check(s.Favorites.Contains(1));s.ToggleFavorite(1);Check(!s.Favorites.Any());});
Test("Reject invalid track favorite",()=>Reject(()=>new LibraryState().ToggleFavorite(999)));
Test("Reject blank, long and duplicate playlist names",()=>{var s=new LibraryState();Reject(()=>s.Create("  "));Reject(()=>s.Create(new string('a',41)));s.Create("Focus");Reject(()=>s.Create(" focus "));});
Test("Playlist create, add, deduplicate, remove, delete",()=>{var s=new LibraryState();var p=s.Create("Mix");s.Add(p.Id,1);s.Add(p.Id,1);Check(s.Playlists[0].TrackIds.Count==1);s.Remove(p.Id,1);Check(s.Playlists[0].TrackIds.Count==0);s.Delete(p.Id);Check(s.Playlists.Count==0);});
Test("Rename validates duplicate",()=>{var s=new LibraryState();var p=s.Create("One");s.Create("Two");Reject(()=>s.Rename(p.Id,"Two"));s.Rename(p.Id,"New");Check(s.Playlists[0].Name=="New");});
Test("Adding invalid track rejects without mutation",()=>{var s=new LibraryState();var p=s.Create("Mix");Reject(()=>s.Add(p.Id,999));Check(s.Playlists[0].TrackIds.Count==0);});
Test("History is recent first and unique",()=>{var s=new LibraryState();s.Record(1);s.Record(2);s.Record(1);s.Record(999);Check(s.History.SequenceEqual(new[]{1,2}));});
Test("JSON snapshot roundtrip",()=>{var s=new LibraryState();s.ToggleFavorite(2);var p=s.Create("Study");s.Add(p.Id,5);s.Record(3);var t=new LibraryState();t.Restore(JsonSerializer.Deserialize<LibrarySnapshot>(JsonSerializer.Serialize(s.Snapshot()))!);Check(t.Favorites.Contains(2)&&t.Playlists[0].TrackIds.Contains(5)&&t.History[0]==3);});
Test("Restore sanitizes unknown and duplicate tracks",()=>{var s=new LibraryState();s.Restore(new(1,new[]{1,999,1},new[]{new Playlist(Guid.NewGuid(),"Good",new(){2,2,999})},new[]{3,3,999}));Check(s.Favorites.Count==1&&s.Playlists[0].TrackIds.SequenceEqual(new[]{2})&&s.History.SequenceEqual(new[]{3}));});
Test("Unsupported restore version preserves data",()=>{var s=new LibraryState();s.ToggleFavorite(1);Reject(()=>s.Restore(new(2,[],[],[])));Check(s.Favorites.Contains(1));});
Test("Snapshot and public playlist data are isolated",()=>{var s=new LibraryState();var p=s.Create("Mix");s.Add(p.Id,1);s.Snapshot().Playlists[0].TrackIds.Clear();s.Playlists[0].TrackIds.Clear();Check(s.Playlists[0].TrackIds.Count==1);});
Test("Playlist count capped",()=>{var s=new LibraryState();for(var i=0;i<50;i++)s.Create($"List {i}");Reject(()=>s.Create("Overflow"));});
Track Upload(int id=100000) => new(id,"Test song","Test artist","Uploads","Personal",0,"#b6a0ff",180,Guid.NewGuid().ToString("N"));
Test("Uploaded tracks participate in search, favorites and playlists",()=>{var s=new LibraryState();var t=Upload();s.RegisterUpload(t);Check(Catalog.Search("test artist","All",s.Tracks).Single().Id==t.Id);s.ToggleFavorite(t.Id);var p=s.Create("Imported");s.Add(p.Id,t.Id);s.Record(t.Id);Check(s.Favorites.Contains(t.Id)&&s.Playlists[0].TrackIds.Contains(t.Id)&&s.History[0]==t.Id);});
Test("Uploaded snapshot restores references before favorite validation",()=>{var s=new LibraryState();var t=Upload();s.RegisterUpload(t);s.ToggleFavorite(t.Id);var p=s.Create("Local");s.Add(p.Id,t.Id);var restored=new LibraryState();restored.Restore(JsonSerializer.Deserialize<LibrarySnapshot>(JsonSerializer.Serialize(s.Snapshot()))!);Check(restored.Tracks.Count==7&&restored.Favorites.Contains(t.Id)&&restored.Playlists[0].TrackIds.Contains(t.Id));});
Test("Invalid upload text, ID, duration and source are rejected",()=>{var s=new LibraryState();var t=Upload();Reject(()=>s.RegisterUpload(t with {Title=" "}));Reject(()=>s.RegisterUpload(t with {Artist=new string('x',101)}));Reject(()=>s.RegisterUpload(t with {Id=1}));Reject(()=>s.RegisterUpload(t with {Duration=0}));Reject(()=>s.RegisterUpload(t with {LocalAudioKey="../../bad"}));Check(s.Tracks.Count==6);});
Test("Upload restore filters invalid records and duplicate IDs",()=>{var s=new LibraryState();var t=Upload();s.Restore(new(1,[],[],[],new[]{t,t,t with {Id=100001,LocalAudioKey="https://example.com"}}));Check(s.Tracks.Count==7&&s.NextUploadId==100001);});
Test("Legacy snapshot remains compatible",()=>{var s=new LibraryState();s.Restore(new(1,new[]{1},[],[]));Check(s.Tracks.Count==6&&s.Favorites.Contains(1));});
Test("Mix includes uploaded songs without changing original catalog",()=>{var s=new LibraryState();s.RegisterUpload(Upload());Check(Catalog.Mix("Focus",[],s.Tracks).Count==7&&Catalog.Tracks.Count==6);});
Console.WriteLine($"\n{passed} passed, {failed} failed");return failed==0?0:1;
