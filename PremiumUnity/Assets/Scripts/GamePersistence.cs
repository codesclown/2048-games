using System;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using UnityEngine;
namespace Premium2048 {
public interface IPreferenceStore {
 string Read(string key,string fallback="");
 int ReadInt(string key,int fallback);
 void Write(string key,string value);
 void Delete(string key);
 void Flush();
}
public sealed class PlayerPreferenceStore : IPreferenceStore {
 public string Read(string key,string fallback=""){return PlayerPrefs.GetString(key,fallback);}
 public int ReadInt(string key,int fallback){return PlayerPrefs.GetInt(key,fallback);}
 public void Write(string key,string value){PlayerPrefs.SetString(key,value);}
 public void Delete(string key){PlayerPrefs.DeleteKey(key);}
 public void Flush(){PlayerPrefs.Save();}
}
// Each save slot is a separate native file. Atomic replacement protects the older
// slot even if the process exits during the current transaction. PlayerPrefs is
// read only for migration and flushed only when legacy keys are explicitly reset.
public sealed class NativeSaveStore : IPreferenceStore {
 readonly string directory;readonly PlayerPreferenceStore legacy=new PlayerPreferenceStore();bool legacyDirty;
 public NativeSaveStore(string directory=null){this.directory=directory??Path.Combine(Application.persistentDataPath,"Saves");}
 string PathFor(string key){using(var hash=SHA256.Create())return Path.Combine(directory,BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(key))).Replace("-","")+".json");}
 public string Read(string key,string fallback="") {
  if(!key.Contains(".save.v2."))return legacy.Read(key,fallback);
  var path=PathFor(key);return File.Exists(path)?File.ReadAllText(path):legacy.Read(key,fallback);
 }
 public int ReadInt(string key,int fallback){return legacy.ReadInt(key,fallback);}
 public void Write(string key,string value) {
  if(!key.Contains(".save.v2.")){legacy.Write(key,value);legacyDirty=true;return;}
  Directory.CreateDirectory(directory);string path=PathFor(key),temporary=path+".tmp";
  byte[] bytes=Encoding.UTF8.GetBytes(value);
  using(var stream=new FileStream(temporary,FileMode.Create,FileAccess.Write,FileShare.None)){stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
  if(File.Exists(path))File.Delete(path);
  File.Move(temporary,path);
 }
 public void Delete(string key) {
  if(key.Contains(".save.v2.")){var path=PathFor(key);if(File.Exists(path))File.Delete(path);if(File.Exists(path+".tmp"))File.Delete(path+".tmp");}
  legacy.Delete(key);legacyDirty=true;
 }
 public void Flush(){if(!legacyDirty)return;legacy.Flush();legacyDirty=false;}
}
[Serializable] public sealed class GameSave {
 public int version=2;
 public Session session;
 public Stats stats=new Stats();
 public bool sound=true;
 public bool haptics=true;
 public bool largeText,reducedMotion,highContrast;
 public string language="en";
 public bool Valid(){return version==2 && (language==null || language=="en" || language=="es") && stats!=null && stats.Valid() && (session==null || session.Valid());}
}
[Serializable] sealed class SaveSlot {
 public long revision;
 public string payload,checksum;
}
// Two alternating, checksum-verified records keep the previous committed save intact.
// Session, statistics and settings are one transaction, never separate writes.
public sealed class GamePersistence {
 readonly IPreferenceStore preferences;readonly string prefix;
 long revision;int currentSlot=-1;string lastPayload;
 public bool ReadOnly {get;private set;}
 public int Commits {get;private set;}
 public bool NeedsCommit {get{return !ReadOnly && lastPayload==null;}}
 public GamePersistence(string prefix,IPreferenceStore preferences=null){this.prefix=prefix;this.preferences=preferences??new NativeSaveStore();}
 string Key(int slot){return prefix+".save.v2."+slot;}
 public GameSave Load() {
  SaveSlot chosen=null;GameSave result=null;
  for(int i=0;i<2;i++) {
   try {
    var slot=JsonUtility.FromJson<SaveSlot>(preferences.Read(Key(i)));
    if(slot==null || slot.revision<=0 || slot.payload==null || slot.checksum!=Checksum(slot.revision,slot.payload))continue;
    var data=JsonUtility.FromJson<GameSave>(slot.payload);
    if(data!=null && data.version>2){ReadOnly=true;continue;}
    if(data==null || !data.Valid() || (chosen!=null && slot.revision<=chosen.revision))continue;
    chosen=slot;result=data;currentSlot=i;
   }catch(Exception){/* A damaged record must not hide the other committed slot. */}
  }
  if(chosen!=null){revision=chosen.revision;lastPayload=chosen.payload;return result;}
  // A future-version record is preserved rather than overwritten by an older app.
  if(ReadOnly)return new GameSave();
  var legacyStats=Parse<Stats>(preferences.Read(prefix+".stats.v1"));
  var legacySession=Parse<Session>(preferences.Read(prefix+".session.v1"));
  result=new GameSave{stats=legacyStats!=null && legacyStats.Valid()?legacyStats:new Stats(),session=legacySession!=null && legacySession.Valid()?legacySession:null,
   sound=preferences.ReadInt(prefix+".sound",1)==1};
  // Legacy preferences remain until an explicit reset; failed migration loses nothing.
  try{Save(result);}catch(Exception e){Debug.LogWarning("Save migration could not commit; legacy data remains available: "+e.Message);}
  return result;
 }
 static T Parse<T>(string value) where T:class {try{return JsonUtility.FromJson<T>(value);}catch(Exception){return null;}}
 static string Checksum(long revision,string payload) {
  using(var hash=SHA256.Create())return Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(revision.ToString(System.Globalization.CultureInfo.InvariantCulture)+"\n"+payload)));
 }
 public bool Save(GameSave data) {
  if(ReadOnly)return false;
  if(data==null || !data.Valid())throw new ArgumentException("Cannot persist invalid game state.");
  string payload=JsonUtility.ToJson(data);
  if(payload==lastPayload)return false;
  long next=checked(revision+1);int slot=currentSlot==0?1:0;
  var record=new SaveSlot{revision=next,payload=payload,checksum=Checksum(next,payload)};
  preferences.Write(Key(slot),JsonUtility.ToJson(record));preferences.Flush();
  revision=next;currentSlot=slot;lastPayload=payload;Commits++;return true;
 }
 public void ClearLegacy(){preferences.Delete(prefix+".session.v1");preferences.Delete(prefix+".stats.v1");preferences.Delete(prefix+".sound");}
 public void Reset() {
  preferences.Delete(Key(0));preferences.Delete(Key(1));ClearLegacy();preferences.Flush();
  revision=0;currentSlot=-1;lastPayload=null;ReadOnly=false;
 }
}
}
