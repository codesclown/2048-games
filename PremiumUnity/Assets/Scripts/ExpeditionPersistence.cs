using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
namespace Premium2048 {
[Serializable] public sealed class ExpeditionSave {
 public int version=1;public ExpeditionSession session;
 public CampaignProgress progress=new CampaignProgress();
 public DailyProgress daily=new DailyProgress();
 public bool Valid(){if(version!=1||progress==null||progress.missions==null||daily==null||session!=null&&!session.Valid())return false;var ids=new System.Collections.Generic.HashSet<string>();foreach(var record in progress.missions)if(record==null||string.IsNullOrEmpty(record.missionId)||!ids.Add(record.missionId)||record.definitionVersion<1||record.stars<0||record.stars>3||record.bestActions<0)return false;return true;}
}
// Independent namespace: E1 never migrates or rewrites the Classic v2 payload.
public sealed class ExpeditionPersistence {
 [Serializable] sealed class Slot {public long revision;public string payload,checksum;}
 readonly IPreferenceStore store;readonly string prefix;long revision;int slot=-1;string lastPayload;
 public bool ReadOnly{get;private set;}
 public ExpeditionPersistence(string prefix,IPreferenceStore store){this.prefix=prefix;this.store=store;}
 string Key(int index){return prefix+".expedition.save.v1."+index;}
 static string Hash(long revision,string payload){using(var hash=SHA256.Create())return Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(revision.ToString(System.Globalization.CultureInfo.InvariantCulture)+"\n"+payload)));}
 public ExpeditionSave Load(){var result=new ExpeditionSave();for(int i=0;i<2;i++){try{var record=JsonUtility.FromJson<Slot>(store.Read(Key(i)));if(record==null||record.revision<1||record.payload==null||record.checksum!=Hash(record.revision,record.payload))continue;var candidate=JsonUtility.FromJson<ExpeditionSave>(record.payload);if(candidate!=null&&candidate.version>1){ReadOnly=true;continue;}if(candidate==null||!candidate.Valid()||record.revision<=revision)continue;result=candidate;revision=record.revision;slot=i;lastPayload=record.payload;}catch(Exception){/* Retain the other committed slot. */}}return result;}
 public void Save(ExpeditionSave data){if(ReadOnly)return;if(data==null||!data.Valid())throw new ArgumentException("Invalid Expedition save.");var payload=JsonUtility.ToJson(data);if(payload==lastPayload)return;long next=checked(revision+1);int target=slot==0?1:0;store.Write(Key(target),JsonUtility.ToJson(new Slot{revision=next,payload=payload,checksum=Hash(next,payload)}));store.Flush();revision=next;slot=target;lastPayload=payload;}
}
}
