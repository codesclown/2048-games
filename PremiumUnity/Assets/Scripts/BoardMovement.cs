using System;
using System.Collections.Generic;
namespace Premium2048 {
public sealed class BoardTurn {
 public bool changed;
 public long scoreGain;
 public readonly List<TileMotion> motions=new List<TileMotion>();
 public readonly List<int> merges=new List<int>();
}
// One merge algorithm for both modes. Terrain divides each line into independent segments.
public static class BoardMovement {
 public static BoardTurn Slide(int[] cells,int size,Direction direction,int[] durability) {
  if(size<4 || size>6 || cells==null || cells.Length!=size*size || (int)direction<0 || (int)direction>3 || durability!=null && durability.Length!=cells.Length)throw new ArgumentException("Invalid movement grid.");
  var turn=new BoardTurn();
  for(int line=0;line<size;line++) {
   int start=0;
   while(start<size) {
    if(Locked(durability,Index(size,direction,line,start))){start++;continue;}
    int end=start;while(end<size && !Locked(durability,Index(size,direction,line,end)))end++;
    var values=new List<int>();var sources=new List<int>();
    for(int i=start;i<end;i++){int index=Index(size,direction,line,i);if(cells[index]!=0){values.Add(cells[index]);sources.Add(index);}}
    var output=new List<int>();
    for(int i=0;i<values.Count;i++) {
     int value=values[i],destination=Index(size,direction,line,start+output.Count);
     turn.motions.Add(new TileMotion(sources[i],destination,value));
     if(i+1<values.Count && values[i+1]==value && value<=int.MaxValue/2){turn.motions.Add(new TileMotion(sources[++i],destination,value));value*=2;turn.merges.Add(destination);turn.scoreGain+=value;}
     output.Add(value);
    }
    for(int i=start;i<end;i++){int index=Index(size,direction,line,i),value=i-start<output.Count?output[i-start]:0;turn.changed|=cells[index]!=value;cells[index]=value;}
    start=end;
   }
  }
  return turn;
 }
 static bool Locked(int[] terrain,int index){return terrain!=null && terrain[index]>0;}
 static int Index(int n,Direction d,int line,int offset){switch(d){case Direction.Left:return line*n+offset;case Direction.Right:return line*n+n-1-offset;case Direction.Up:return offset*n+line;default:return (n-1-offset)*n+line;}}
}
}
