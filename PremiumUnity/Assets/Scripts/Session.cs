using System;
using System.Collections.Generic;
namespace Premium2048 {
public enum Power { Undo, Swap, Delete }
[Serializable] public class Stock {
 public int available, used;
 public double lastEarn=-1;
}
[Serializable] public class Session : IGameSession {
 public GameMode Mode {get{return GameMode.Classic;}}
 public int Size {get{return board.size;}}
 public int[] Cells {get{return board.cells;}}
 public long Score {get{return board.score;}}
 public Board board;
 public List<Board> history=new List<Board>();
 public Stock[] stock={new Stock(),new Stock(),new Stock()};
 public int moves;
 public bool rewardUsed, winRecorded;
 public bool Unlocked { get { return moves>=2; } }
 public int Limit { get { return board.size; } }
 public Session() {}
 public Session(int size,Random random) { board=new Board(size);board.Spawn(random);board.Spawn(random); }
 void Remember(Board previous) { history.Add(previous); if(history.Count>15) history.RemoveAt(0); }
 public bool Move(Direction direction,Random random,double time) {
  Board previous=board.Copy(); if(!board.Slide(direction)) return false;
  Remember(previous);board.Spawn(random);moves++;
  if(moves==2) {
   stock[0].available=board.size==6?3:2;
   stock[1].available=stock[2].available=board.size-3;
  }
  Earn(time);return true;
 }
 public void Earn(double time) {
  if(!Unlocked || board.Blocked) return;
  int[] required={128,256,512};
  for(int i=0;i<3;i++) {
   Stock s=stock[i];
   if(s.used>0 && s.available+s.used<Limit && time-s.lastEarn>=1 && Array.IndexOf(board.cells,required[i])>=0) { s.available++;s.lastEarn=time; }
  }
 }
 public bool CanUse(Power power) {
  int i=(int)power;return i>=0 && i<3 && Unlocked && stock[i].available>0 && stock[i].used<Limit && !board.Blocked && (power!=Power.Undo || history.Count>0);
 }
 void Spend(Power power) { stock[(int)power].available--;stock[(int)power].used++; }
 public bool Undo() {
  if(!CanUse(Power.Undo)) return false;
  board=history[history.Count-1].Copy();history.RemoveAt(history.Count-1);Spend(Power.Undo);return true;
 }
 bool Occupied(int index) { return index>=0 && index<board.cells.Length && board.cells[index]!=0; }
 public bool Swap(int first,int second) {
  if(!CanUse(Power.Swap) || !Occupied(first) || !Occupied(second) || board.cells[first]==board.cells[second]) return false;
  if(Math.Abs(first/board.size-second/board.size)+Math.Abs(first%board.size-second%board.size)!=1) return false;
  Remember(board.Copy()); int temp=board.cells[first];board.cells[first]=board.cells[second];board.cells[second]=temp;Spend(Power.Swap);return true;
 }
 public bool Delete(int index) {
  if(!CanUse(Power.Delete) || !Occupied(index)) return false;
  int occupied=0;foreach(int v in board.cells) if(v!=0) occupied++;
  if(occupied<=1) return false;
  Remember(board.Copy());board.cells[index]=0;
  // Won is a historical achievement even if its tile is deleted.
  Spend(Power.Delete);return true;
 }
 public bool CanReward { get { return board.Over && !rewardUsed && history.Count>0; } }
 public bool Reward() {
  if(!CanReward) return false;
  int targetIndex=Math.Max(0,history.Count-5);long score=board.score;
  board=history[targetIndex].Copy();board.score=score;board.continued=true;
  history.RemoveRange(targetIndex,history.Count-targetIndex);rewardUsed=true;
  if(!board.HasMoves()){
   int cleared=0;
   for(int i=0;i<board.cells.Length && cleared<2;i++)if(board.cells[i]>0){board.cells[i]=0;cleared++;}
  }
  return true;
 }
 public bool Valid() {
  if(board==null || !board.Valid() || moves<0 || stock==null || stock.Length!=3 || history==null || history.Count>15) return false;
  foreach(Stock s in stock) if(s==null || s.available<0 || s.used<0 || s.available+s.used>Limit || double.IsNaN(s.lastEarn) || double.IsInfinity(s.lastEarn)) return false;
  foreach(Board b in history) if(b==null || b.size!=board.size || !b.Valid()) return false;
  return true;
 }
}
[Serializable] public class Stats {
 public long[] best=new long[3];
 public int played, won;
 public bool Valid() { return best!=null && best.Length==3 && Array.TrueForAll(best,v=>v>=0) && played>=0 && won>=0 && won<=played; }
}
}
