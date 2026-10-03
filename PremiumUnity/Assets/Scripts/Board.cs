using System;
using System.Collections.Generic;
namespace Premium2048 {
public enum Direction { Up, Right, Down, Left }
public struct TileMotion {
 public int from, to, value;
 public TileMotion(int source,int destination,int number) { from=source;to=destination;value=number; }
}
[Serializable] public class Board {
 [NonSerialized] public List<TileMotion> motions=new List<TileMotion>();
 [NonSerialized] public List<int> merges=new List<int>();
 [NonSerialized] public int spawned=-1;
 public int size=4;
 public int target=2048;
 public int[] cells = new int[16];
 public Board() {}
 public Board(int dimension) { if(dimension<4 || dimension>6) throw new ArgumentOutOfRangeException("dimension"); size=dimension;target=dimension==4?2048:dimension==5?4096:8192;cells=new int[size*size]; }
 public Board Copy() { var b=new Board(size);b.cells=(int[])cells.Clone();b.score=score;b.won=won;b.continued=continued;return b; }
 public long score;
 public bool won, continued;
 public bool Over { get { return !HasMoves(); } }
 public bool Blocked { get { return Over || (won && !continued); } }
 public bool Slide(Direction direction) {
  motions=new List<TileMotion>(); merges=new List<int>(); spawned=-1;
  if (Blocked) return false;
  var turn=BoardMovement.Slide(cells,size,direction,null);
  motions=turn.motions;merges=turn.merges;score+=turn.scoreGain;
  foreach(int index in merges)if(cells[index]>=target)won=true;
  return turn.changed;
 }
 public bool Spawn(Random random) {
  var empty=new List<int>(); for(int i=0;i<cells.Length;i++) if(cells[i]==0) empty.Add(i);
  spawned=-1; if(empty.Count==0) return false;
  spawned=empty[random.Next(empty.Count)]; cells[spawned]=random.NextDouble()<0.9?2:4; return true;
 }
 public bool HasMoves() {
  for(int i=0;i<cells.Length;i++) if(cells[i]==0 || (i%size<size-1 && cells[i]==cells[i+1] && cells[i]<=int.MaxValue/2) || (i<cells.Length-size && cells[i]==cells[i+size] && cells[i]<=int.MaxValue/2)) return true;
  return false;
 }
 public bool Valid() {
  if(cells==null || size<4 || size>6 || cells.Length!=size*size || target!=(size==4?2048:size==5?4096:8192) || score<0) return false;
  bool occupied=false, hasTarget=false;
  foreach(int v in cells) { if(v<0 || v==1 || (v!=0 && (v & (v-1))!=0)) return false; occupied |= v!=0; hasTarget |= v>=target; }
  return occupied && (!hasTarget || won) && (!continued || won);
 }
}
}
