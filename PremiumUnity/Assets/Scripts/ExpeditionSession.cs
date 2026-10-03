using System;
using System.Collections.Generic;
namespace Premium2048 {
public enum GameMode { Classic, Expedition }
public enum MissionGoalKind { ReachTile, HoldTiles, UnlockCells, All }
public enum MissionTurnEventKind { Slide, Merge, Spawn, LockDamaged, LockOpened, GoalReached, Completed, Failed }
[Serializable] public sealed class MissionGoalDefinition {
 public MissionGoalKind kind;public int value=128,count=1;public int[] cells=new int[0];public MissionGoalDefinition[] children=new MissionGoalDefinition[0];
 public string Describe(){
  switch(kind){
   case MissionGoalKind.ReachTile: return "Reach "+value;
   case MissionGoalKind.HoldTiles: return "Hold "+count+" × "+value+" tiles";
   case MissionGoalKind.UnlockCells: return "Unlock "+(cells!=null?cells.Length:0)+" locks";
   case MissionGoalKind.All:
    var list=new List<string>();
    if(children!=null)foreach(var c in children)if(c!=null)list.Add(c.Describe());
    return string.Join(" & ",list.ToArray());
   default: return "";
  }
 }
}
[Serializable] public sealed class MissionSpawnStep {
 public int value=2;public int[] priority=new int[0];
}
[Serializable] public sealed class MissionDefinition {
 public string id;public string title="";public string description="";public int chapter=1;public int version=1,size=4,actionLimit=30;
 public int[] initialTiles=new int[16],terrain=new int[16];
 public int[] toolAllowance=new int[3];
 public MissionGoalDefinition goal=new MissionGoalDefinition();
 public MissionSpawnStep[] spawnScript=new MissionSpawnStep[0];
 public MissionDefinition Copy(){return new MissionDefinition{id=id,title=title,description=description,chapter=chapter,version=version,size=size,actionLimit=actionLimit,initialTiles=(int[])initialTiles.Clone(),terrain=(int[])terrain.Clone(),toolAllowance=(int[])toolAllowance.Clone(),goal=CopyGoal(goal),spawnScript=CopyScript(spawnScript)};}
 static MissionGoalDefinition CopyGoal(MissionGoalDefinition g){var copy=new MissionGoalDefinition{kind=g.kind,value=g.value,count=g.count,cells=(int[])g.cells.Clone(),children=new MissionGoalDefinition[g.children.Length]};for(int i=0;i<copy.children.Length;i++)copy.children[i]=CopyGoal(g.children[i]);return copy;}
 static MissionSpawnStep[] CopyScript(MissionSpawnStep[] script){var copy=new MissionSpawnStep[script.Length];for(int i=0;i<copy.Length;i++)copy[i]=new MissionSpawnStep{value=script[i].value,priority=(int[])script[i].priority.Clone()};return copy;}
 public bool Valid(){
  if(string.IsNullOrEmpty(id)||version<1||size<4||size>6||actionLimit<1||chapter<1||chapter>3||initialTiles==null||terrain==null||initialTiles.Length!=size*size||terrain.Length!=initialTiles.Length||toolAllowance==null||toolAllowance.Length!=3||spawnScript==null||!GoalValid(goal,size*size,0))return false;
  bool occupied=false;for(int i=0;i<initialTiles.Length;i++){if(!Number(initialTiles[i])||terrain[i]<0||terrain[i]>2||terrain[i]>0&&initialTiles[i]!=0)return false;occupied|=initialTiles[i]>0;}
  foreach(int n in toolAllowance)if(n<0)return false;
  foreach(var step in spawnScript){if(step==null||step.value<2||!Number(step.value)||step.priority==null)return false;foreach(int i in step.priority)if(i<0||i>=initialTiles.Length)return false;}
  return occupied;
 }
 internal static bool Number(int n){return n==0 || n>=2&&(n&(n-1))==0;}
 static bool GoalValid(MissionGoalDefinition g,int length,int depth){if(g==null||depth>16||g.cells==null||g.children==null)return false;switch(g.kind){case MissionGoalKind.ReachTile:case MissionGoalKind.HoldTiles:return g.value>=2&&Number(g.value)&&g.count>0&&g.count<=length;case MissionGoalKind.UnlockCells:if(g.cells.Length==0)return false;var seen=new HashSet<int>();foreach(int i in g.cells)if(i<0||i>=length||!seen.Add(i))return false;return true;case MissionGoalKind.All:if(g.children.Length==0)return false;foreach(var child in g.children)if(!GoalValid(child,length,depth+1))return false;return true;default:return false;}}
}
[Serializable] public sealed class MissionTurnEvent {public MissionTurnEventKind kind;public int cell,value;public MissionTurnEvent(MissionTurnEventKind type,int index,int number){kind=type;cell=index;value=number;}}
[Serializable] public sealed class ExpeditionActionSnapshot {
 public int[] tiles,terrain,tools;public int spawnCursor,actions;public long score;public string[] reachedGoals=new string[0];
}
[Serializable] public sealed class MissionProgress {public string missionId;public int definitionVersion,stars,bestActions;public bool completed;}
[Serializable] public sealed class CampaignProgress {
 public List<MissionProgress> missions=new List<MissionProgress>();
 public MissionProgress Get(string id){foreach(var m in missions)if(m.missionId==id)return m;return null;}
 public void Record(string id,int version,int stars,int actions){
  var p=Get(id);
  if(p==null){p=new MissionProgress{missionId=id,definitionVersion=version,stars=stars,bestActions=actions,completed=true};missions.Add(p);}
  else{p.completed=true;p.definitionVersion=Math.Max(p.definitionVersion,version);p.stars=Math.Max(p.stars,stars);p.bestActions=p.bestActions==0?actions:Math.Min(p.bestActions,actions);}
 }
}
// E3 owns mission engine, goal evaluation, action budget, tools and safe persistence.
[Serializable] public sealed class ExpeditionSession : IGameSession {
 public GameMode Mode {get{return GameMode.Expedition;}}
 public int Size {get{return mission.size;}}
 public int[] Cells {get{return tiles;}}
 public long Score {get{return score;}}
 public MissionDefinition mission;public int[] tiles,terrain,tools;public int spawnCursor,actions;public long score;
 public bool completed,failed;public string[] reachedGoals=new string[0];
 public List<ExpeditionActionSnapshot> history=new List<ExpeditionActionSnapshot>();
 [NonSerialized] public BoardTurn lastTurn;
 [NonSerialized] public List<MissionTurnEvent> events=new List<MissionTurnEvent>();
 public ExpeditionSession(){}
 public ExpeditionSession(MissionDefinition definition){if(definition==null||!definition.Valid())throw new ArgumentException("Invalid mission.");mission=definition.Copy();tiles=(int[])mission.initialTiles.Clone();terrain=(int[])mission.terrain.Clone();tools=(int[])mission.toolAllowance.Clone();}
 public ExpeditionActionSnapshot Snapshot(){return new ExpeditionActionSnapshot{tiles=(int[])tiles.Clone(),terrain=(int[])terrain.Clone(),tools=(int[])tools.Clone(),spawnCursor=spawnCursor,actions=actions,score=score,reachedGoals=(string[])reachedGoals.Clone()};}
 public bool Move(Direction direction){
  if(completed||failed||actions>=mission.actionLimit||spawnCursor==int.MaxValue||(int)direction<0||(int)direction>3)return false;
  var before=Snapshot();lastTurn=BoardMovement.Slide(tiles,mission.size,direction,terrain);events=new List<MissionTurnEvent>();
  if(!lastTurn.changed)return false;
  history.Add(before);if(history.Count>15)history.RemoveAt(0);actions++;score+=lastTurn.scoreGain;
  foreach(var motion in lastTurn.motions)events.Add(new MissionTurnEvent(MissionTurnEventKind.Slide,motion.to,motion.value));
  foreach(int cell in lastTurn.merges)events.Add(new MissionTurnEvent(MissionTurnEventKind.Merge,cell,tiles[cell]));
  
  int n=mission.size;var damaged=new List<int>();var newlyOpened=new HashSet<int>();
  foreach(int mergeCell in lastTurn.merges){
   int r=mergeCell/n,c=mergeCell%n;
   if(r>0)CheckLock(mergeCell-n,damaged);
   if(r<n-1)CheckLock(mergeCell+n,damaged);
   if(c>0)CheckLock(mergeCell-1,damaged);
   if(c<n-1)CheckLock(mergeCell+1,damaged);
  }
  foreach(int lockCell in damaged){
   terrain[lockCell]--;
   if(terrain[lockCell]==0){
    newlyOpened.Add(lockCell);
    events.Add(new MissionTurnEvent(MissionTurnEventKind.LockOpened,lockCell,0));
   }else{
    events.Add(new MissionTurnEvent(MissionTurnEventKind.LockDamaged,lockCell,terrain[lockCell]));
   }
  }
  Spawn(newlyOpened);
  Evaluate();
  return true;
 }
 void CheckLock(int index,List<int> list){if(terrain!=null && terrain[index]>0 && !list.Contains(index))list.Add(index);}
 void Spawn(HashSet<int> newlyOpened){
  var script=mission.spawnScript;MissionSpawnStep step=spawnCursor<script.Length?script[spawnCursor]:null;
  int cell=-1;
  if(step!=null)foreach(int candidate in step.priority)if(tiles[candidate]==0&&terrain[candidate]==0&&!newlyOpened.Contains(candidate)){cell=candidate;break;}
  if(cell<0)for(int i=0;i<tiles.Length;i++)if(tiles[i]==0&&terrain[i]==0&&!newlyOpened.Contains(i)){cell=i;break;}
  if(cell<0)return;int value=step==null?2:step.value;tiles[cell]=value;spawnCursor++;events.Add(new MissionTurnEvent(MissionTurnEventKind.Spawn,cell,value));
 }
 public bool CanUseTool(Power power){
  int idx=(int)power;
  return !completed && !failed && tools!=null && idx>=0 && idx<tools.Length && tools[idx]>0;
 }
 public bool Undo(){
  if(!CanUseTool(Power.Undo) || history.Count==0)return false;
  var snapshot=history[history.Count-1];
  history.RemoveAt(history.Count-1);
  tiles=(int[])snapshot.tiles.Clone();
  terrain=(int[])snapshot.terrain.Clone();
  spawnCursor=snapshot.spawnCursor;
  score=snapshot.score;
  reachedGoals=(string[])snapshot.reachedGoals.Clone();
  tools[(int)Power.Undo]--;
  actions++;
  events=new List<MissionTurnEvent>();
  Evaluate();
  return true;
 }
 public bool Swap(int first,int second){
  if(!CanUseTool(Power.Swap))return false;
  if(first<0||first>=tiles.Length||second<0||second>=tiles.Length||first==second)return false;
  if(terrain[first]>0||terrain[second]>0||tiles[first]==0||tiles[second]==0||tiles[first]==tiles[second])return false;
  int r1=first/mission.size,c1=first%mission.size;
  int r2=second/mission.size,c2=second%mission.size;
  if(Math.Abs(r1-r2)+Math.Abs(c1-c2)!=1)return false;
  var before=Snapshot();
  history.Add(before);if(history.Count>15)history.RemoveAt(0);
  int temp=tiles[first];tiles[first]=tiles[second];tiles[second]=temp;
  tools[(int)Power.Swap]--;
  actions++;
  events=new List<MissionTurnEvent>();
  Evaluate();
  return true;
 }
 public bool Delete(int index){
  if(!CanUseTool(Power.Delete))return false;
  if(index<0||index>=tiles.Length||terrain[index]>0||tiles[index]==0)return false;
  var before=Snapshot();
  history.Add(before);if(history.Count>15)history.RemoveAt(0);
  tiles[index]=0;
  tools[(int)Power.Delete]--;
  actions++;
  events=new List<MissionTurnEvent>();
  Evaluate();
  return true;
 }
 public void Evaluate(){
  if(EvaluateGoal(mission.goal)){
   completed=true;
   events.Add(new MissionTurnEvent(MissionTurnEventKind.GoalReached,0,0));
   events.Add(new MissionTurnEvent(MissionTurnEventKind.Completed,0,0));
  }else if(actions>=mission.actionLimit || !CanMove()){
   failed=true;
   events.Add(new MissionTurnEvent(MissionTurnEventKind.Failed,0,0));
  }
 }
 public bool EvaluateGoal(MissionGoalDefinition g){
  if(g==null)return false;
  switch(g.kind){
   case MissionGoalKind.ReachTile:
    for(int i=0;i<tiles.Length;i++)if(tiles[i]>=g.value)return true;
    return false;
   case MissionGoalKind.HoldTiles:
    int count=0;for(int i=0;i<tiles.Length;i++)if(tiles[i]==g.value)count++;
    return count>=g.count;
   case MissionGoalKind.UnlockCells:
    foreach(int cell in g.cells)if(terrain[cell]>0)return false;
    return true;
   case MissionGoalKind.All:
    foreach(var child in g.children)if(!EvaluateGoal(child))return false;
    return true;
   default:return false;
  }
 }
 public bool CanMove(){
  int n=mission.size;
  for(int i=0;i<tiles.Length;i++)if(tiles[i]==0&&terrain[i]==0)return true;
  for(int r=0;r<n;r++)for(int c=0;c<n-1;c++){
   int i1=r*n+c,i2=r*n+c+1;
   if(terrain[i1]==0&&terrain[i2]==0&&tiles[i1]>0&&tiles[i1]==tiles[i2])return true;
  }
  for(int c=0;c<n;c++)for(int r=0;r<n-1;r++){
   int i1=r*n+c,i2=(r+1)*n+c;
   if(terrain[i1]==0&&terrain[i2]==0&&tiles[i1]>0&&tiles[i1]==tiles[i2])return true;
  }
  return false;
 }
 public int CalculateStars(){
  if(!completed)return 0;
  if(actions<=Math.Max(1,(int)(mission.actionLimit*0.6f)))return 3;
  if(actions<=Math.Max(1,(int)(mission.actionLimit*0.85f)))return 2;
  return 1;
 }
 public bool Valid(){
  if(mission==null||!mission.Valid()||!GridValid(tiles,terrain)||tools==null||tools.Length!=3||spawnCursor<0||actions<0||spawnCursor>actions||score<0||reachedGoals==null||history==null||history.Count>15||completed&&failed)return false;
  for(int i=0;i<3;i++)if(tools[i]<0||tools[i]>mission.toolAllowance[i])return false;
  foreach(var snapshot in history)if(snapshot==null||!GridValid(snapshot.tiles,snapshot.terrain)||snapshot.spawnCursor<0||snapshot.spawnCursor>spawnCursor||snapshot.score<0||snapshot.score>score||snapshot.reachedGoals==null)return false;
  return true;
 }
 bool GridValid(int[] values,int[] locks){if(values==null||locks==null||values.Length!=mission.size*mission.size||locks.Length!=values.Length)return false;bool occupied=false;for(int i=0;i<values.Length;i++){if(!MissionDefinition.Number(values[i])||locks[i]<0||locks[i]>mission.terrain[i]||locks[i]>0&&values[i]!=0)return false;occupied|=values[i]>0;}return occupied;}
}
}
