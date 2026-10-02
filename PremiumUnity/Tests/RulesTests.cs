using System;
using Premium2048;
class RulesTests {
 static int checks;
 static void Check(bool value,string name){checks++;if(!value)throw new Exception(name);}
 static Board Row(int size,params int[] values){var b=new Board(size);Array.Copy(values,b.cells,values.Length);return b;}
 static Session Unlocked(int size){var s=new Session(size,new Random(1));s.moves=2;for(int i=0;i<3;i++)s.stock[i].available=2;return s;}
 static void Main(){
  var rng=new Random(81);
  for(int size=4;size<=6;size++){
   var b=Row(size,2,2,2,2);Check(b.Slide(Direction.Left),"valid move");Check(b.cells[0]==4 && b.cells[1]==4 && b.score==8,"single merge per tile");
   b=Row(size,2,2,4);b.Slide(Direction.Left);Check(b.cells[0]==4 && b.cells[1]==4 && b.score==4,"no chain merge");
   b=Row(size,2,0,2,2);b.Slide(Direction.Right);Check(b.cells[size-1]==4 && b.cells[size-2]==2,"right precedence");
   b=Row(size,2,4);Check(!b.Slide(Direction.Left) && b.score==0,"invalid move has no changes");
   b=new Board(size);b.cells[0]=b.cells[size]=2;b.Slide(Direction.Up);Check(b.cells[0]==4 && b.cells[size]==0,"up");
   b=new Board(size);b.cells[0]=b.cells[size]=2;b.Slide(Direction.Down);Check(b.cells[(size-1)*size]==4,"down");
   b=new Board(size);for(int i=0;i<b.cells.Length;i++)b.cells[i]=(i%size+i/size)%2==0?2:4;Check(b.Over,"game over");b.cells[1]=2;Check(!b.Over,"adjacent match");
   b=Row(size,new Board(size).target/2,new Board(size).target/2);b.Slide(Direction.Left);Check(b.won && b.Blocked && b.score==b.target,"level target");Check(!b.Slide(Direction.Right),"win blocks");b.continued=true;Check(b.Slide(Direction.Right),"continue after win");
   var s=new Session(size,rng);Check(!s.CanUse(Power.Undo),"locked tools");
   s.board=Row(size,2,4);Check(!s.Move(Direction.Left,rng,0) && s.moves==0 && s.history.Count==0,"no-op does not unlock or save history");
   Check(s.Move(Direction.Right,rng,0),"first move");Check(s.moves==1 && !s.Unlocked,"one counted move");
   Check(s.Move(Direction.Down,rng,1),"second move");Check(s.Unlocked && s.stock[0].available==(size==6?3:2) && s.stock[1].available==size-3,"unlock stock config");
   var snapshot=s.history[s.history.Count-1];Check(s.Undo(),"undo");Check(Equal(s.board.cells,snapshot.cells) && s.board.score==snapshot.score,"undo restores exact board and score");Check(s.stock[0].used==1,"undo stock not restored");
   s=Unlocked(size);s.board=Row(size,2,4,8);int count=s.stock[1].available;Check(!s.Swap(0,2) && s.stock[1].available==count,"nonadjacent swap rejected without charge");Check(s.Swap(0,1) && s.board.cells[0]==4 && s.board.cells[1]==2,"valid swap");
   s=Unlocked(size);s.board=Row(size,2,4,4);Check(s.Delete(1) && s.board.cells[1]==0 && s.board.cells[2]==4,"delete selected duplicate only");Check(s.Undo() && s.board.cells[1]==4,"undo delete");
   s=Unlocked(size);s.board=Row(size,2);Check(!s.Delete(0),"cannot delete only tile");
   s=Unlocked(size);s.board=Row(size,128,256,512);for(int i=0;i<3;i++){s.stock[i].used=1;s.stock[i].available=0;}
   s.Earn(0);for(int i=0;i<3;i++)Check(s.stock[i].available==1,"earn threshold");s.Earn(0.5);Check(s.stock[0].available==1,"earn cooldown");s.Earn(1);Check(s.stock[0].available==2,"earn after cooldown");
   for(int time=2;time<20;time++)s.Earn(time);for(int i=0;i<3;i++)Check(s.stock[i].available+s.stock[i].used==size,"lifetime cap");
   s=Unlocked(size);for(int j=0;j<7;j++){var historical=Row(size,2,4);historical.score=j*4;s.history.Add(historical);}s.board=new Board(size);for(int i=0;i<s.board.cells.Length;i++)s.board.cells[i]=(i%size+i/size)%2==0?2:4;s.board.score=500;
   Check(s.CanReward && s.Reward(),"reward available on loss");Check(s.board.score==500 && s.history.Count==2 && !s.board.Over,"rewind five actions preserves score");Check(!s.Reward() && s.rewardUsed,"one reward per game");
   s=Unlocked(size);s.history.Add(Row(size,2,4));s.board=new Board(size);for(int i=0;i<s.board.cells.Length;i++)s.board.cells[i]=(i%size+i/size)%2==0?2:4;Check(s.Reward() && s.history.Count==0,"short history reward");
   for(int game=0;game<80;game++){
    s=new Session(size,rng);
    for(int turn=0;turn<1500 && !s.board.Over;turn++){
     if(s.board.won)s.board.continued=true;long sum=0;foreach(int value in s.board.cells)sum+=value;
     if(s.Move((Direction)rng.Next(4),rng,turn)){
      long animationSum=0;var destinations=new int[size*size];foreach(var motion in s.board.motions){Check(motion.from>=0 && motion.from<size*size && motion.to>=0 && motion.to<size*size,"motion bounds");animationSum+=motion.value;destinations[motion.to]+=motion.value;}
      Check(sum==animationSum,"move conserves tile values");for(int i=0;i<destinations.Length;i++)Check(destinations[i]+(s.board.spawned==i?s.board.cells[i]:0)==s.board.cells[i],"motion destinations plus spawn equal board");
      Check(s.history.Count<=15 && s.moves>0,"bounded history");
     }
     Check(s.Valid(),"session integrity");
    }
   }
  }
  var corrupt=new Board();corrupt.cells=null;Check(!corrupt.Valid(),"null board");corrupt=new Board();corrupt.cells[0]=3;Check(!corrupt.Valid(),"invalid tile");
  Console.WriteLine("Passed "+checks+" assertions across 4x4, 5x5 and 6x6.");
 }
 static bool Equal(int[] a,int[] b){if(a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}
}
