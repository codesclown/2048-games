namespace Premium2048 {
// Rules expose state; GameController owns gestures, GameNavigation owns input gates.
public interface IGameSession {
 GameMode Mode {get;}
 int Size {get;}
 int[] Cells {get;}
 long Score {get;}
}
}
