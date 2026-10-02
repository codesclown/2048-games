using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace Premium2048 {
[DisallowMultipleComponent]
public class PremiumGame : MonoBehaviour {
 public string persistenceNamespace="Premium2048";
 public RewardProvider rewardProvider;
 static PremiumGame instance;
 GameManager manager;
 Session session {get{return manager.Session;}}
 Stats stats {get{return manager.Statistics;}}
 bool sound {get{return manager.Sound;}}
 GameNavigation navigation {get{return manager.Navigation;}}
 bool rewardPending {get{return navigation.State==GameState.Rewarding;}}
 int selection {get{return navigation.SelectedPower.HasValue?(int)navigation.SelectedPower.Value:-1;}}
 GameController controller;
 Font font;Sprite rounded,brandMark,contrastTile;CanvasScaler scaler;
 RectTransform safeRoot, root, grid, modalPanel;
 GameObject home, game, loading, modal, cancelSelection;
 Text score,best,level,hint,addition,soundLabel,loadText;Image loadFill;
 Image[] tiles;Text[] numbers;Button[] powers=new Button[3];Text[] powerLabels=new Text[3];Text[] powerStatuses=new Text[3];Text resumeDetails;GameObject selectionFrame;
 readonly Dictionary<int,Sprite> tileSprites=new Dictionary<int,Sprite>();
 readonly List<RectTransform> ghostPool=new List<RectTransform>();
 readonly List<RectTransform> activeGhosts=new List<RectTransform>();
 bool ready,animating,appPaused,focusLost;
 int selected=-1;
 Coroutine moveAnimation,scoreAnimation,boardPulseAnimation;AudioSource audioSource;AudioClip moveTone,mergeTone,clickTone,winTone;
 int lastWidth,lastHeight;Rect lastSafe;
 Color Gold=>PremiumTheme.Accent;Color Ink=>PremiumTheme.Ink;Color White=>PremiumTheme.Text;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Boot() { if(FindFirstObjectByType<PremiumGame>()==null) new GameObject("2048 Premium").AddComponent<PremiumGame>(); }
 void Awake() {
  if(instance!=null && instance!=this){Destroy(this);return;}instance=this;
  manager=GetComponent<GameManager>();if(manager==null)manager=gameObject.AddComponent<GameManager>();
  controller=GetComponent<GameController>();if(controller==null)controller=gameObject.AddComponent<GameController>();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
  if(Array.IndexOf(Environment.GetCommandLineArgs(),"-premiumSmoke")>=0){persistenceNamespace="Premium2048.QA";new GamePersistence(persistenceNamespace).Reset();}
#endif
  manager.Initialize(persistenceNamespace);
  manager.Changed+=OnGameChanged;navigation.Changed+=OnNavigationChanged;
  Application.targetFrameRate=120;QualitySettings.vSyncCount=0;Application.runInBackground=false;Screen.sleepTimeout=SleepTimeout.SystemSetting;font=Resources.Load<Font>("Fonts/NotoSans-Regular");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
  // Keep the desktop QA harness advancing when another window has focus.
  if(Array.IndexOf(Environment.GetCommandLineArgs(),"-premiumSmoke")>=0)Application.runInBackground=true;
#endif
  rounded=RoundedSprite();brandMark=Resources.Load<Sprite>("Brand/Mark");
  if(brandMark==null){var mt=Resources.Load<Texture2D>("Brand/Mark");if(mt!=null)brandMark=Sprite.Create(mt,new Rect(0,0,mt.width,mt.height),new Vector2(0.5f,0.5f));}
  contrastTile=GradientRounded(Hex("F2EFE5"),Hex("F2EFE5"));BuildShell();
  if(rewardProvider==null) rewardProvider=GetComponent<RewardProvider>();
  BuildHome();BuildGame();
  audioSource=gameObject.AddComponent<AudioSource>();audioSource.playOnAwake=false;
  moveTone=Tone("Slide",220,0.07f);mergeTone=Tone("Merge",440,0.13f);clickTone=Tone("Click",660,0.06f);winTone=Tone("Win",880,0.35f);
  ready=true;ShowHome();FitScreen();
 }
 void Progress(float value,string text) {if(loadFill!=null)loadFill.fillAmount=value;if(loadText!=null)loadText.text=text+"\n"+Mathf.RoundToInt(value*100)+"%";}
 void Update() {
  FitScreen();if(!ready)return;
  if(Input.GetKeyDown(KeyCode.Escape)) {
   if(rewardPending || navigation.Suspended)return;
   if(navigation.Dialog!=GameDialog.None)CloseModal();
   else if(selection>=0){CancelSelection();Refresh();}
   else if(navigation.Screen==GameScreen.Game)ShowMenu();return;
  }
  if(!Playing)return;
  controller.Poll(grid,selection>=0,Move,SelectCell,ConfirmRestart);
 }
 bool Playing {get{return ready && navigation.AcceptsBoardInput && session!=null && !session.board.Blocked;}}
 void Move(Direction direction){if(ready)manager.Move(direction,Time.realtimeSinceStartupAsDouble);}
 void OnGameChanged(GameChange change) {
  if(!ready)return;
  if(change==GameChange.Settings){ApplyPresentation();return;}
  if(change==GameChange.Move && manager.ReducedMotion){StopScore();Refresh(false);PremiumHaptics.Trigger(manager.LastScoreGain>0?HapticImpact.Medium:HapticImpact.Light);manager.ResolveAnimation();return;}
  if(change==GameChange.Move){Play(manager.LastScoreGain>0?mergeTone:moveTone);moveAnimation=StartCoroutine(AnimateMove(manager.LastScoreGain));return;}
  if(change==GameChange.NewGame || change==GameChange.Undo || change==GameChange.Continue){StopScore();BuildBoard();}
  Refresh();
 }
 void OnNavigationChanged() {
  controller.Cancel();if(!ready)return;
  home.SetActive(navigation.Screen==GameScreen.Home);game.SetActive(navigation.Screen==GameScreen.Game);loading.SetActive(navigation.Screen==GameScreen.Loading);
  if(navigation.State==GameState.Animating)return;
  if(animating)FinishAnimation();
  if(selection<0)ClearSelectionVisuals();
  if(navigation.Screen==GameScreen.Home)RefreshHome();
  modal.SetActive(navigation.Dialog!=GameDialog.None);
  if(navigation.Dialog!=GameDialog.None)RenderDialog();
  if(navigation.Screen==GameScreen.Game && session!=null){if(tiles==null || tiles.Length!=session.board.cells.Length)BuildBoard();Refresh();}
 }
 IEnumerator AnimateMove(long gain) {
  animating=true;var motions=session.board.motions;var moving=new List<RectTransform>();
  for(int i=0;i<tiles.Length;i++)tiles[i].gameObject.SetActive(false);
  foreach(var motion in motions){var tile=AcquireGhost(grid,motion.from,motion.value);moving.Add(tile);}
  float elapsed=0;
  while(elapsed<0.09f) {
   elapsed+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(elapsed/0.09f);float ease=t*t*(3f-2f*t);
   for(int i=0;i<moving.Count;i++)moving[i].anchoredPosition=Vector2.Lerp(CellPosition(motions[i].from),CellPosition(motions[i].to),ease);
   yield return null;
  }
  ClearGhosts();Refresh(false);ScoreGain(gain);
  PremiumHaptics.Trigger(gain>0?HapticImpact.Medium:HapticImpact.Light);
  elapsed=0;
  while(elapsed<0.12f) {
   elapsed+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(elapsed/0.12f);
   float pop=1f+0.16f*Mathf.Sin(t*Mathf.PI);
   foreach(int index in session.board.merges)tiles[index].transform.localScale=Vector3.one*pop;
   if(session.board.spawned>=0)tiles[session.board.spawned].transform.localScale=Vector3.one*(t*t*(3f-2f*t));
   yield return null;
  }
  animating=false;moveAnimation=null;manager.ResolveAnimation();Refresh();
 }
 RectTransform AcquireGhost(Transform parent,int index,int value) {
  RectTransform tile=null;
  while(ghostPool.Count>0 && tile==null){
   var cand=ghostPool[ghostPool.Count-1];ghostPool.RemoveAt(ghostPool.Count-1);
   if(cand!=null && cand.gameObject!=null)tile=cand;
  }
  if(tile!=null){
   tile.gameObject.SetActive(true);tile.transform.SetParent(parent,false);
  } else {
   tile=CreateTile(parent,index,value);
  }
  tile.anchoredPosition=CellPosition(index);
  var img=tile.GetComponent<Image>();if(img!=null){img.sprite=TileSprite(value);img.color=Color.white;tile.localScale=Vector3.one;}
  var txt=tile.GetComponentInChildren<Text>();if(txt!=null){SetText(txt,"{0}",value);txt.color=TileText(value);}
  activeGhosts.Add(tile);return tile;
 }
 void ClearGhosts(){foreach(var ghost in activeGhosts)if(ghost!=null && ghost.gameObject!=null){ghost.gameObject.SetActive(false);ghostPool.Add(ghost);}activeGhosts.Clear();}
 void FinishAnimation() {
  if(moveAnimation!=null)StopCoroutine(moveAnimation);moveAnimation=null;animating=false;controller.Cancel();ClearGhosts();
  if(boardPulseAnimation!=null)StopCoroutine(boardPulseAnimation);boardPulseAnimation=null;
  if(tiles!=null)foreach(var tile in tiles){tile.gameObject.SetActive(true);tile.transform.localScale=Vector3.one;}
  if(grid!=null)grid.localScale=Vector3.one;
 }
 void NewGame(int dimension){if(rewardPending || navigation.Suspended)return;FinishAnimation();manager.StartGame(dimension);}
 void ShowGame(){manager.Resume();}
 void ShowHome(){manager.Home();}
 void OnApplicationPause(bool paused){appPaused=paused;if(paused && audioSource!=null)audioSource.Stop();if(manager!=null){controller.Cancel();manager.SetBackground(appPaused || focusLost);}}
 void OnApplicationFocus(bool focused){focusLost=!focused;if(!focused && audioSource!=null)audioSource.Stop();if(manager!=null){controller.Cancel();manager.SetBackground(appPaused || focusLost);}}
 void OnApplicationQuit(){if(manager!=null)manager.Save();}
 void Refresh(bool results=true) {
  if(session==null || tiles==null)return;Board b=session.board;SetText(score,"{0}",b.score);SetText(best,"{0}",stats.best[b.size-4]);
  SetText(level,"Level {0}  /  {1} × {1}  /  Target {2}",b.size-3,b.size,b.target);
  for(int i=0;i<tiles.Length;i++){int v=b.cells[i];tiles[i].gameObject.SetActive(v!=0);tiles[i].sprite=TileSprite(v);tiles[i].color=Color.white;tiles[i].transform.localScale=Vector3.one;SetText(numbers[i],"{0}",v);numbers[i].color=TileText(v);}
  for(int i=0;i<3;i++) {
   bool active=selection==i,locked=!session.Unlocked;
   SetText(powerLabels[i],"{0}  {1}",T(((Power)i).ToString()),session.stock[i].available);
   SetText(powerStatuses[i],active?"SELECTED":locked?"LOCKED":session.CanUse((Power)i)?"READY":"EMPTY");
   powers[i].interactable=navigation.State==GameState.Playing && !navigation.Suspended && session.CanUse((Power)i);
   powers[i].GetComponent<PremiumControl>().State(active,locked);
  }
  cancelSelection.SetActive(selection>=0);
  if(selection<0)SetText(hint,session.Unlocked?"Hold 128 / 256 / 512 to earn tools":"Tools unlock after {0} more moves",Math.Max(0,2-session.moves));
  else SetText(hint,selection==(int)Power.Swap?"Tap two different adjacent tiles":"Tap the tile to remove · Cancel to exit");
  SetText(soundLabel,"");soundLabel.transform.parent.GetComponentInChildren<PremiumIcon>().Kind=sound?IconKind.Sound:IconKind.Mute;
  if(results && navigation.State!=GameState.Animating)manager.ShowResult();
 }
 void Result(){manager.ShowResult();}
 void ConfirmRestart(){if(session!=null && !rewardPending)navigation.Open(GameDialog.Restart);}
 void ShowMenu(){if(session!=null && !rewardPending)navigation.Open(GameDialog.Pause);}
 void StartLevel(int dimension){if(!navigation.Suspended && !rewardPending){FinishAnimation();manager.StartGame(dimension);}}
 void ShowLevels(){navigation.Open(GameDialog.Levels);}
 void ShowInstructions(){navigation.Open(GameDialog.Instructions);}
 void ShowScores(){navigation.Open(GameDialog.Scores);}
 void ShowSettings(){navigation.Open(GameDialog.Settings);}
 void RenderDialog() {
  switch(navigation.Dialog) {
   case GameDialog.Pause:
    Modal("Paused","Take a breath. Your board is saved.");
    DialogButton("Resume",150,ShowGame,true);DialogButton("Change level",78,ShowLevels);DialogButton("How to Play",6,ShowInstructions);DialogButton("High Scores",-66,ShowScores);DialogButton("Settings",-138,ShowSettings);DialogButton("Home",-210,ShowHome);break;
   case GameDialog.Levels:
    Modal("Choose your pace",session!=null?"Starting a board replaces your saved game.":"Three boards. One satisfying challenge.");
    LevelCard(4,132,"Classic","Short and focused");LevelCard(5,-16,"Advanced","Room to build");LevelCard(6,-164,"Expert","Think a few moves ahead");break;
   case GameDialog.Instructions:
    Modal("How to Play","Simple moves. Thoughtful choices.");BuildInstructions();break;
   case GameDialog.Scores:
    Modal("HIGH SCORES","Your personal hall of fame & career records");
    string[] modeNames=new[]{"CLASSIC 4×4","ADVANCED 5×5","EXPERT 6×6"};
    int[] targets=new[]{2048,4096,8192};
    for(int i=0;i<3;i++){
     var card=Panel("Record "+i,modalPanel,new Vector2(0,138-i*102),new Vector2(456,92),Hex("111E31"));
     var border=Rect("Record Border",card.transform,Vector2.zero,new Vector2(452,88)).gameObject.AddComponent<PremiumSelectionFrame>();border.color=Hex("243C5E");border.raycastTarget=false;
     var emblem=Panel("Trophy Emblem",card.transform,new Vector2(-172,0),new Vector2(58,58),Hex("091220"));
     AddIcon(emblem.transform,Vector2.zero,30,IconKind.Trophy,Gold);
     var modeTitle=Label("Mode",card.transform,modeNames[i],new Vector2(-30,14),new Vector2(190,30),20);Emphasis(modeTitle);modeTitle.alignment=TextAnchor.MiddleLeft;
     var targetLabel=Label("Grid",card.transform,"Target {0}  ·  Level {1}",new Vector2(-30,-16),new Vector2(190,24),14,targets[i],i+1);targetLabel.color=PremiumTheme.Muted;targetLabel.alignment=TextAnchor.MiddleLeft;
     var scoreBox=Panel("Score Box",card.transform,new Vector2(148,0),new Vector2(128,64),Hex("091220"));
     var scoreCap=Label("Score Cap",scoreBox.transform,"RECORD",new Vector2(0,16),new Vector2(120,20),12);scoreCap.color=Gold;Emphasis(scoreCap);
     var scoreVal=Label("Best score",scoreBox.transform,"{0}",new Vector2(0,-10),new Vector2(120,36),26,stats.best[i]);Emphasis(scoreVal);scoreVal.color=White;
    }
    var statsCard=Panel("Summary Card",modalPanel,new Vector2(0,-164),new Vector2(456,62),Hex("0D1728"));
    var statsBorder=Rect("Stats Border",statsCard.transform,Vector2.zero,new Vector2(452,58)).gameObject.AddComponent<PremiumSelectionFrame>();statsBorder.color=Hex("1E3048");statsBorder.raycastTarget=false;
    int totalP=stats.played, totalW=stats.won, winR=totalP==0?0:Mathf.RoundToInt(100f*totalW/totalP);
    var pBox=Rect("Played Col",statsCard.transform,new Vector2(-140,0),new Vector2(130,50));
    Label("Played Cap",pBox,"PLAYED",new Vector2(0,12),new Vector2(130,18),12).color=PremiumTheme.Muted;
    var pVal=Label("Played Val",pBox,"{0}",new Vector2(0,-10),new Vector2(130,28),22,totalP);Emphasis(pVal);pVal.color=Gold;
    var wBox=Rect("Won Col",statsCard.transform,new Vector2(0,0),new Vector2(130,50));
    Label("Won Cap",wBox,"VICTORIES",new Vector2(0,12),new Vector2(130,18),12).color=PremiumTheme.Muted;
    var wVal=Label("Won Val",wBox,"{0}",new Vector2(0,-10),new Vector2(130,28),22,totalW);Emphasis(wVal);wVal.color=Gold;
    var rBox=Rect("Rate Col",statsCard.transform,new Vector2(140,0),new Vector2(130,50));
    Label("Rate Cap",rBox,"WIN RATE",new Vector2(0,12),new Vector2(130,18),12).color=PremiumTheme.Muted;
    var rVal=Label("Rate Val",rBox,"{0}%",new Vector2(0,-10),new Vector2(130,28),22,winR);Emphasis(rVal);rVal.color=Gold;
    DialogButton("Clear scores",-244,()=>navigation.Open(GameDialog.ClearScores),false,true);break;
   case GameDialog.Settings:
    Modal("Settings","Make the game comfortable for you");
    SettingRow("Sound",166,sound,ToggleSound);
    SettingRow("Haptic feedback",94,manager.Haptics,ToggleHaptics);
    SettingRow("Reduced motion",22,manager.ReducedMotion,manager.ToggleReducedMotion);
    SettingRow("Large text",-50,manager.LargeText,manager.ToggleLargeText);
    SettingRow("High contrast",-122,manager.HighContrast,manager.ToggleHighContrast);
    var languageButton=Button("Language",modalPanel,new Vector2(0,-194),new Vector2(456,64),manager.ToggleLanguage);
    SetText(languageButton.GetComponentInChildren<Text>(),"Language · {0}",manager.Language=="es"?"Español":"English");break;
   case GameDialog.ClearScores:
    Modal("Clear saved scores?","This clears scores, statistics and the saved game.");DialogButton("Clear",-10,manager.ResetProgress,false,true);DialogButton("Cancel",-94,CloseModal);break;
   case GameDialog.Restart:
    Modal("Restart level?","Start a fresh board and reset power-ups. Your best score stays saved.");DialogButton("Restart",-10,()=>NewGame(session.board.size),false,true);DialogButton("Cancel",-94,CloseModal);break;
   case GameDialog.Win:
    Play(winTone);PremiumHaptics.Trigger(HapticImpact.Success);
    Modal("VICTORY!","Target reached! Keep going to set a new record.");
    ResultCard("TARGET REACHED",session.board.target);
    DialogButton("Keep going",-35,()=>manager.KeepGoing(),true);
    DialogButton("Play again",-108,()=>NewGame(session.board.size));
    DialogButton("Home",-180,ShowHome);break;
   case GameDialog.Loss:
    Modal("GAME OVER","No moves remain. Choose your next move.");
    ResultCard("FINAL SCORE",session.board.score);
    if(session.CanReward){
     DialogButton(rewardProvider==null?"Continue: rewind 5 moves":"Watch video to continue",-35,AskReward,true);
     DialogButton("Play again",-108,()=>NewGame(session.board.size));
     DialogButton("Home",-180,ShowHome);
    } else {
     DialogButton("Play again",-50,()=>NewGame(session.board.size),true);
     DialogButton("Home",-128,ShowHome);
    }break;
   case GameDialog.Continue:
    Modal("Continue playing?","Rewind up to 5 previous actions and keep your current score. Available once per game.");
    DialogButton("Continue",-10,GrantOfflineReward,true);
    DialogButton("No thanks",-94,CloseModal);break;
   case GameDialog.VideoUnavailable:
    Modal("Video unavailable","The ad provider is not ready. Your board remains saved.");DialogButton("Back",0,CloseModal);break;
   case GameDialog.RewardVideo:
    Modal("Reward video","Waiting for the ad provider. Reward is granted only after completion.");break;
  }
 }
 void ToggleSound(){manager.ToggleSound();if(sound)Play(clickTone);}
 void ToggleHaptics(){manager.ToggleHaptics();if(manager.Haptics)PremiumHaptics.Trigger(HapticImpact.Medium);}
 void UsePower(Power power){
  if(!Playing)return;
  if(power==Power.Undo){
   if(manager.Undo()){
    Play(clickTone);PremiumHaptics.Trigger(HapticImpact.Light);
    if(!manager.ReducedMotion){if(boardPulseAnimation!=null)StopCoroutine(boardPulseAnimation);boardPulseAnimation=StartCoroutine(PulseBoard());}
   }
  }else manager.BeginSelection(power);
 }
 IEnumerator PulseBoard() {
  if(grid==null)yield break;
  float elapsed=0;
  while(elapsed<0.14f){elapsed+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(elapsed/0.14f);grid.localScale=Vector3.one*(1f-0.035f*Mathf.Sin(t*Mathf.PI));yield return null;}
  grid.localScale=Vector3.one;boardPulseAnimation=null;
 }
 void ClearSelectionVisuals(){selected=-1;if(cancelSelection!=null)cancelSelection.SetActive(false);if(selectionFrame!=null){selectionFrame.SetActive(false);Destroy(selectionFrame);selectionFrame=null;}}
 void CancelSelection(){manager.CancelSelection();ClearSelectionVisuals();}
 void SelectCell(Vector2 screen) {
  if(!Playing || selection<0)return;
  Vector2 local;if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(grid,screen,null,out local))return;
  int index=-1;for(int i=0;i<tiles.Length;i++){Vector2 d=local-CellPosition(i);if(Mathf.Abs(d.x)<=CellSize/2 && Mathf.Abs(d.y)<=CellSize/2){index=i;break;}}
  if(index<0 || session.board.cells[index]==0)return;
  if(selection==(int)Power.Delete){if(manager.Delete(index)){Play(clickTone);PremiumHaptics.Trigger(HapticImpact.Medium);}return;}
  if(selected<0){selected=index;MarkSelection(index);SetText(hint,"Tile 1 selected · Tap an adjacent tile");return;}
  if(index==selected){CancelSelection();Refresh();return;}
  if(manager.Swap(selected,index)){Play(clickTone);PremiumHaptics.Trigger(HapticImpact.Medium);}else SetText(hint,"Choose a different tile next to tile 1");
 }
 void AskReward() {
  if(!session.CanReward || rewardPending)return;
  if(rewardProvider==null){navigation.Open(GameDialog.Continue);return;}
  if(!rewardProvider.IsReady){navigation.Open(GameDialog.VideoUnavailable);return;}
  int request=manager.BeginReward();if(request<0)return;StartCoroutine(RewardTimeout(request));
  try{rewardProvider.Show(success=>manager.CompleteReward(request,success));}
  catch(Exception e){Debug.LogWarning("Reward provider failed: "+e.Message);manager.CompleteReward(request,false);}
 }
 IEnumerator RewardTimeout(int request){yield return new WaitForSecondsRealtime(60);manager.CompleteReward(request,false);}
 void GrantOfflineReward(){if(manager.ContinueOffline()){Play(clickTone);PremiumHaptics.Trigger(HapticImpact.Medium);}}
 void StopScore() {if(scoreAnimation!=null)StopCoroutine(scoreAnimation);scoreAnimation=null;SetText(addition,"");}
 void ScoreGain(long gain) {if(gain<=0)return;StopScore();scoreAnimation=StartCoroutine(FloatScore(gain));}
 IEnumerator FloatScore(long gain) {
  SetText(addition,"+{0}",gain);float elapsed=0;
  while(elapsed<0.65f) {elapsed+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(elapsed/0.65f);addition.rectTransform.anchoredPosition=new Vector2(0,25+40*t);addition.color=new Color(1,0.84f,0,1-t);yield return null;}
  SetText(addition,"");scoreAnimation=null;
 }
 void Play(AudioClip clip) {if(sound && clip!=null && audioSource!=null)audioSource.PlayOneShot(clip,0.22f);}
 static AudioClip Tone(string name,float frequency,float duration) {
  int samples=(int)(44100*duration);var data=new float[samples];
  for(int i=0;i<samples;i++){float t=i/44100f;float envelope=Mathf.Sin(Mathf.PI*i/samples);data[i]=Mathf.Sin(2*Mathf.PI*frequency*t)*envelope*0.5f;}
  var clip=AudioClip.Create(name,samples,1,44100,false);clip.SetData(data,0);return clip;
 }
 void FitScreen() {
  if(safeRoot==null || (lastWidth==Screen.width && lastHeight==Screen.height && lastSafe==Screen.safeArea))return;
  lastWidth=Screen.width;lastHeight=Screen.height;lastSafe=Screen.safeArea;if(lastWidth==0 || lastHeight==0)return;
  var anchors=PremiumLayout.CalculateSafeAnchors(lastWidth,lastHeight,lastSafe);
  safeRoot.anchorMin=new Vector2(anchors.xMin,anchors.yMin);safeRoot.anchorMax=new Vector2(anchors.xMax,anchors.yMax);safeRoot.offsetMin=safeRoot.offsetMax=Vector2.zero;
  scaler.scaleFactor=PremiumLayout.CalculateScale(lastWidth,lastHeight,lastSafe,new Vector2(600,960));
 }
 static Color Hex(string value) {ColorUtility.TryParseHtmlString("#"+value,out var c);return c;}
 static Color TileColor(int value) {return value==0?Hex("253149"):Color.white;}
 Color TileText(int value){return PremiumTheme.TileText(value,manager.HighContrast);}
 Sprite TileSprite(int value) {
  if(manager.HighContrast && value>0)return contrastTile;
  if(tileSprites.TryGetValue(value,out var sprite))return sprite;
  PremiumTheme.TileColors(value,out var first,out var second);sprite=GradientRounded(first,second);tileSprites[value]=sprite;return sprite;
 }
 Sprite GradientRounded(Color first,Color second) {
  var texture=new Texture2D(64,64,TextureFormat.RGBA32,false);texture.wrapMode=TextureWrapMode.Clamp;texture.filterMode=FilterMode.Bilinear;
  for(int y=0;y<64;y++)for(int x=0;x<64;x++) {
   float dx=Mathf.Max(7-x,x-56),dy=Mathf.Max(7-y,y-56);float distance=Mathf.Sqrt(Mathf.Max(0,dx)*Mathf.Max(0,dx)+Mathf.Max(0,dy)*Mathf.Max(0,dy));
   var color=Color.Lerp(first,second,(x+63-y)/126f);color.a=Mathf.Clamp01(7.5f-distance);texture.SetPixel(x,y,color);
  }
  texture.Apply();return Sprite.Create(texture,new Rect(0,0,64,64),new Vector2(0.5f,0.5f));
 }
 int BoardDimension {get{return session!=null && session.board!=null?session.board.size:4;}}
 float CellSize {get{return PremiumLayout.CalculateCellSize(BoardDimension,496f);}}
 Vector2 CellPosition(int i) {return PremiumLayout.CalculateCellPosition(i,BoardDimension,CellSize);}
 RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size) {
  var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var rt=go.GetComponent<RectTransform>();rt.anchorMin=rt.anchorMax=new Vector2(0.5f,0.5f);rt.anchoredPosition=position;rt.sizeDelta=size;return rt;
 }
 Image Panel(string name,Transform parent,Vector2 position,Vector2 size,Color color,bool raycast=false) {
  var rt=Rect(name,parent,position,size);var img=rt.gameObject.AddComponent<Image>();img.sprite=rounded;img.type=Image.Type.Sliced;img.color=color;img.raycastTarget=raycast;return img;
 }
 Text Label(string name,Transform parent,string text,Vector2 position,Vector2 size,int fontSize,params object[] arguments) {
  var rt=Rect(name,parent,position,size);var label=rt.gameObject.AddComponent<Text>();label.font=font;label.text=text;label.fontSize=fontSize;label.alignment=TextAnchor.MiddleCenter;label.color=White;label.raycastTarget=false;label.horizontalOverflow=HorizontalWrapMode.Wrap;label.verticalOverflow=VerticalWrapMode.Truncate;rt.gameObject.AddComponent<PremiumText>().Initialize(manager,text,fontSize,arguments);return label;
 }
 Button Button(string name,Transform parent,Vector2 position,Vector2 size,Action action,bool primary=false,bool destructive=false) {
  var img=Panel(name,parent,position,size,primary?Gold:Hex("26334B"),true);var button=img.gameObject.AddComponent<Button>();button.targetGraphic=img;button.navigation=new Navigation{mode=Navigation.Mode.None};
  var colors=button.colors;colors.highlightedColor=Color.white;colors.pressedColor=new Color(.82f,.82f,.82f);colors.disabledColor=Color.white;button.colors=colors;
  var label=Label("Label",img.transform,name,Vector2.zero,size-Vector2.one*12,22);label.color=primary?Ink:White;Emphasis(label);
  img.gameObject.AddComponent<PremiumControl>().Initialize(manager,destructive?ControlTone.Destructive:primary?ControlTone.Primary:ControlTone.Secondary,name);
  img.gameObject.AddComponent<PremiumButtonMotion>();
  button.onClick.AddListener(()=>{Play(clickTone);action();});return button;
 }
 Button IconButton(string name,Transform parent,Vector2 position,Action action,IconKind kind) {
  var button=Button(name,parent,position,new Vector2(58,58),action);
  var txt=button.GetComponentInChildren<Text>();
  if(txt!=null)txt.gameObject.SetActive(false);
  AddIcon(button.transform,Vector2.zero,26,kind,White);
  return button;
 }
 void AddIcon(Transform parent,Vector2 position,float size,IconKind kind,Color color) {
  var rt=Rect("Icon",parent,position,Vector2.one*size);var icon=rt.gameObject.AddComponent<PremiumIcon>();icon.Kind=kind;icon.color=color;icon.raycastTarget=false;
 }
 static Sprite RoundedSprite() {
  var texture=new Texture2D(32,32,TextureFormat.RGBA32,false);texture.wrapMode=TextureWrapMode.Clamp;texture.filterMode=FilterMode.Bilinear;
  for(int y=0;y<32;y++)for(int x=0;x<32;x++) {float dx=Mathf.Max(7-x,x-24),dy=Mathf.Max(7-y,y-24);float distance=Mathf.Sqrt(Mathf.Max(0,dx)*Mathf.Max(0,dx)+Mathf.Max(0,dy)*Mathf.Max(0,dy));texture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(7.5f-distance)));}
  texture.Apply();return Sprite.Create(texture,new Rect(0,0,32,32),new Vector2(0.5f,0.5f),100,0,SpriteMeshType.FullRect,new Vector4(8,8,8,8));
 }
 void BuildShell() {
  if(FindFirstObjectByType<EventSystem>()==null)new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
  var canvas=new GameObject("Premium Canvas",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvas.transform.SetParent(transform,false);canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
  scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;
  var bg=Panel("Background",canvas.transform,Vector2.zero,Vector2.zero,Color.white);bg.rectTransform.anchorMin=Vector2.zero;bg.rectTransform.anchorMax=Vector2.one;bg.rectTransform.offsetMin=bg.rectTransform.offsetMax=Vector2.zero;bg.sprite=GradientSprite();bg.type=Image.Type.Simple;
  safeRoot=Rect("Safe Area",canvas.transform,Vector2.zero,Vector2.zero);root=Rect("Content",safeRoot,Vector2.zero,new Vector2(600,960));
  home=Rect("Home",root,Vector2.zero,new Vector2(600,960)).gameObject;game=Rect("Game",root,Vector2.zero,new Vector2(600,960)).gameObject;loading=Rect("Loading",root,Vector2.zero,new Vector2(600,960)).gameObject;
  Brand(loading.transform,new Vector2(0,90),1.1f);
  Label("Subtitle",loading.transform,"A moment of focus",new Vector2(0,-12),new Vector2(500,48),22).color=PremiumTheme.Muted;
  Panel("Track",loading.transform,new Vector2(0,-100),new Vector2(380,12),Hex("26334B"));loadFill=Panel("Progress",loading.transform,new Vector2(0,-100),new Vector2(380,12),Gold);loadFill.type=Image.Type.Filled;loadFill.fillMethod=Image.FillMethod.Horizontal;
  loadText=Label("Status",loading.transform,"",new Vector2(0,-160),new Vector2(500,70),19);
  modal=Panel("Modal Overlay",safeRoot,Vector2.zero,Vector2.zero,new Color(0.04f,0.06f,0.12f,0.96f),true).gameObject;
  var overlay=modal.GetComponent<RectTransform>();overlay.anchorMin=Vector2.zero;overlay.anchorMax=Vector2.one;overlay.offsetMin=overlay.offsetMax=Vector2.zero;
  modalPanel=Panel("Dialog",modal.transform,Vector2.zero,new Vector2(520,740),Hex("142236"),true).rectTransform;
  var modalBorder=Rect("Modal Border",modalPanel,Vector2.zero,new Vector2(516,736)).gameObject.AddComponent<PremiumSelectionFrame>();modalBorder.color=Hex("263C5B");modalBorder.raycastTarget=false;
  home.SetActive(false);game.SetActive(false);loading.SetActive(false);modal.SetActive(false);FitScreen();
 }
 Sprite GradientSprite() {
  var texture=new Texture2D(64,64,TextureFormat.RGBA32,false);for(int y=0;y<64;y++)for(int x=0;x<64;x++)texture.SetPixel(x,y,Color.Lerp(Hex("070E1A"),Hex("18283F"),(x+y)/126f));texture.Apply();return Sprite.Create(texture,new Rect(0,0,64,64),new Vector2(0.5f,0.5f));
 }
 void BuildHome() {
  var mute=IconButton("Sound: On",home.transform,new Vector2(-234,420),ToggleSound,sound?IconKind.Sound:IconKind.Mute);soundLabel=mute.GetComponentInChildren<Text>(true);
  IconButton("Settings",home.transform,new Vector2(234,420),ShowSettings,IconKind.Settings);
  Brand(home.transform,new Vector2(0,350),1.05f);
  Label("Edition",home.transform,"THE NUMBER COLLECTION",new Vector2(0,280),new Vector2(500,32),15).color=Gold;
  Label("Description",home.transform,"A little focus. A better move.",new Vector2(0,230),new Vector2(500,48),22).color=PremiumTheme.Muted;
  var heroCard=Panel("Preview Board",home.transform,new Vector2(0,46),new Vector2(496,260),Hex("111D30"));
  var heroBorder=Rect("Hero Border",heroCard.transform,Vector2.zero,new Vector2(492,256)).gameObject.AddComponent<PremiumSelectionFrame>();heroBorder.color=Hex("253B57");heroBorder.raycastTarget=false;
  var previewBox=Panel("Hero Preview",heroCard.transform,new Vector2(-140,0),new Vector2(176,176),Hex("091220"));
  int[] defaultValues={2,4,8,16,32,64,128,256,512,1024,2048,0,0,0,0,0};
  int[] previewValues=(session!=null && session.board!=null && session.board.size==4)?session.board.cells:defaultValues;
  for(int i=0;i<16;i++){
   int v=(i<previewValues.Length)?previewValues[i]:0;
   var cell=Panel("Preview "+i,previewBox.transform,new Vector2(-60+(i%4)*40,60-(i/4)*40),new Vector2(36,36),v>0?Color.white:PremiumTheme.Color("1E2E42"));
   if(v>0){cell.sprite=TileSprite(v);cell.type=Image.Type.Simple;var value=Label("Value",cell.transform,"{0}",Vector2.zero,new Vector2(34,34),v>=1024?12:15,v);Emphasis(value);value.color=TileText(v);}
  }
  bool hasActiveSession=session!=null && !session.board.Over;
  var heroTitle=Label("Hero Title",heroCard.transform,hasActiveSession?"EXPEDITION IN PROGRESS":"CLASSIC CHALLENGE",new Vector2(80,52),new Vector2(250,30),16);Emphasis(heroTitle);heroTitle.color=Gold;heroTitle.alignment=TextAnchor.MiddleLeft;
  var heroSubtitle=Label("Preview caption",heroCard.transform,hasActiveSession?"Resume your current board":"One swipe can change everything",new Vector2(80,16),new Vector2(250,42),16);heroSubtitle.color=PremiumTheme.Muted;heroSubtitle.alignment=TextAnchor.MiddleLeft;
  var heroStats=Label("Hero Stats",heroCard.transform,"{0} × {0}  ·  Best {1}",new Vector2(80,-28),new Vector2(250,26),15,hasActiveSession?session.board.size:4,stats.best[hasActiveSession?session.board.size-4:0]);heroStats.color=Hex("D4AF37");heroStats.alignment=TextAnchor.MiddleLeft;
  Button("New Game",home.transform,new Vector2(0,-125),new Vector2(500,76),ShowLevels,true);
  var resume=Button("Resume",home.transform,new Vector2(0,-215),new Vector2(500,76),ShowGame);
  var title=resume.GetComponentInChildren<Text>();title.rectTransform.anchoredPosition=new Vector2(-70,12);title.rectTransform.sizeDelta=new Vector2(290,32);title.alignment=TextAnchor.MiddleLeft;Size(title,22);
  resumeDetails=Label("Resume detail",resume.transform,"",new Vector2(-70,-18),new Vector2(290,28),15);resumeDetails.alignment=TextAnchor.MiddleLeft;resumeDetails.color=PremiumTheme.Muted;AddIcon(resume.transform,new Vector2(194,0),28,IconKind.Play,White);
  var statsPill=Panel("Stats Summary",home.transform,new Vector2(0,-295),new Vector2(500,52),Hex("132034"));
  int totalPlayed=stats.played;int totalWon=stats.won;int winRate=totalPlayed==0?0:Mathf.RoundToInt(100f*totalWon/totalPlayed);
  var statsLabel=Label("Stats Summary Label",statsPill.transform,"Played {0}  ·  Won {1}  ·  Win rate {2}%",Vector2.zero,new Vector2(480,40),16,totalPlayed,totalWon,winRate);statsLabel.color=PremiumTheme.Muted;
  Button("How to Play",home.transform,new Vector2(-128,-370),new Vector2(244,70),ShowInstructions);
  Button("High Scores",home.transform,new Vector2(128,-370),new Vector2(244,70),ShowScores);
  Label("Footer",home.transform,"SWIPE · MERGE · MASTER",new Vector2(0,-438),new Vector2(500,28),14).color=PremiumTheme.Muted;
 }
 void BuildGame() {
  IconButton("Menu",game.transform,new Vector2(-234,416),ShowMenu,IconKind.Menu);
  var wordmark=Label("Title",game.transform,"2048",new Vector2(-20,416),new Vector2(190,64),50);wordmark.color=Gold;Emphasis(wordmark);
  var mute=IconButton("Sound: On",game.transform,new Vector2(172,416),ToggleSound,sound?IconKind.Sound:IconKind.Mute);soundLabel=mute.GetComponentInChildren<Text>(true);
  IconButton("Restart",game.transform,new Vector2(234,416),ConfirmRestart,IconKind.Restart);
  score=ScoreCard("SCORE",new Vector2(-128,324));best=ScoreCard("BEST",new Vector2(128,324));addition=Label("Score addition",score.transform.parent,"",new Vector2(0,25),new Vector2(220,60),28);
  var levelPill=Panel("Level Pill",game.transform,new Vector2(0,246),new Vector2(496,40),Hex("111D30"));
  var levelBorder=Rect("Level Border",levelPill.transform,Vector2.zero,new Vector2(492,36)).gameObject.AddComponent<PremiumSelectionFrame>();levelBorder.color=Hex("223650");levelBorder.raycastTarget=false;
  level=Label("Level",levelPill.transform,"",Vector2.zero,new Vector2(480,36),17);level.color=Gold;Emphasis(level);
  grid=Panel("Board",game.transform,new Vector2(0,-35),new Vector2(520,520),Hex("0E182A")).rectTransform;
  var boardBorder=Rect("Board Border",grid,Vector2.zero,new Vector2(516,516)).gameObject.AddComponent<PremiumSelectionFrame>();boardBorder.color=Hex("223650");boardBorder.raycastTarget=false;
  var actionDock=Panel("Action Bar",game.transform,new Vector2(0,-338),new Vector2(520,80),Hex("111D30"));
  var dockBorder=Rect("Dock Border",actionDock.transform,Vector2.zero,new Vector2(516,76)).gameObject.AddComponent<PremiumSelectionFrame>();dockBorder.color=Hex("223650");dockBorder.raycastTarget=false;
  for(int i=0;i<3;i++) {
   int action=i;powers[i]=Button(((Power)i).ToString(),actionDock.transform,new Vector2(-168+i*168,0),new Vector2(156,70),()=>UsePower((Power)action));
   powerLabels[i]=powers[i].GetComponentInChildren<Text>();Size(powerLabels[i],17);powerLabels[i].rectTransform.anchoredPosition=new Vector2(16,11);powerLabels[i].rectTransform.sizeDelta=new Vector2(110,28);
   AddIcon(powers[i].transform,new Vector2(-48,11),22,(IconKind)((int)IconKind.Undo+i),White);
   powerStatuses[i]=Label("Availability",powers[i].transform,"LOCKED",new Vector2(0,-18),new Vector2(145,20),12);
  }
  hint=Label("Hint",game.transform,"",new Vector2(0,-396),new Vector2(520,32),15);hint.color=PremiumTheme.Muted;
  cancelSelection=Button("Cancel selection",game.transform,new Vector2(0,-438),new Vector2(230,50),()=>{CancelSelection();Refresh();}).gameObject;Size(cancelSelection.GetComponentInChildren<Text>(),15);cancelSelection.SetActive(false);
 }
 Text ScoreCard(string title,Vector2 position) {
  var card=Panel(title,game.transform,position,new Vector2(240,86),Hex("142236"));
  var cardBorder=Rect("Card Border",card.transform,Vector2.zero,new Vector2(236,82)).gameObject.AddComponent<PremiumSelectionFrame>();cardBorder.color=Hex("243854");cardBorder.raycastTarget=false;
  var caption=Label("Caption",card.transform,title,new Vector2(0,23),new Vector2(220,24),15);caption.color=Gold;Emphasis(caption);
  var number=Label("Number",card.transform,"0",new Vector2(0,-13),new Vector2(220,44),34);Emphasis(number);number.resizeTextForBestFit=true;number.resizeTextMinSize=18;number.resizeTextMaxSize=34;return number;
 }
 RectTransform CreateTile(Transform parent,int index,int value) {
  var image=Panel("Tile "+index,parent,CellPosition(index),Vector2.one*CellSize,Color.white);image.sprite=TileSprite(value);image.type=Image.Type.Simple;var number=Label("Value",image.transform,value.ToString(),Vector2.zero,Vector2.one*(CellSize-8),BoardDimension==4?39:BoardDimension==5?31:26);Emphasis(number);number.resizeTextForBestFit=true;number.resizeTextMinSize=16;number.resizeTextMaxSize=number.fontSize;number.color=TileText(value);var shadow=number.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,0.35f);shadow.effectDistance=new Vector2(0,-2);return image.rectTransform;
 }
 void BuildBoard() {
  if(session==null)return;
  foreach(Transform child in grid) {if(child.name=="Board Border")continue;child.gameObject.SetActive(false);Destroy(child.gameObject);}
  int count=session.board.cells.Length;tiles=new Image[count];numbers=new Text[count];
  for(int i=0;i<count;i++)Panel("Empty "+i,grid,CellPosition(i),Vector2.one*CellSize,TileColor(0));
  for(int i=0;i<count;i++) {var tile=CreateTile(grid,i,session.board.cells[i]);tiles[i]=tile.GetComponent<Image>();numbers[i]=tile.GetComponentInChildren<Text>();}
 }
 void Modal(string title,string description) {
  modal.transform.SetAsLastSibling();modal.SetActive(true);
  var motion=modalPanel.GetComponent<PremiumPanelMotion>();if(motion==null)motion=modalPanel.gameObject.AddComponent<PremiumPanelMotion>();motion.Reveal();
  foreach(Transform child in modalPanel){if(child.name=="Modal Border")continue;child.gameObject.SetActive(false);Destroy(child.gameObject);}
  var heading=Label("Title",modalPanel,title,new Vector2(0,296),new Vector2(456,58),32);Emphasis(heading);heading.color=Gold;
  Label("Description",modalPanel,description,new Vector2(0,225),new Vector2(456,78),19).color=PremiumTheme.Muted;
  Button("Close",modalPanel,new Vector2(0,-314),new Vector2(180,56),CloseModal);
 }
 void DialogButton(string title,float y,Action action,bool primary=false,bool destructive=false){Button(title,modalPanel,new Vector2(0,y),new Vector2(456,66),action,primary,destructive);}
 string T(string english,params object[] arguments){return GameLocalization.Translate(manager.Language,english,arguments);}
 void SetText(Text text,string english,params object[] arguments){if(text!=null)text.GetComponent<PremiumText>().Set(english,arguments);}
 void Size(Text text,int size){text.GetComponent<PremiumText>().Size(size);}
 void Emphasis(Text text){text.GetComponent<PremiumText>().Emphasis();}
 void ApplyPresentation() {
  foreach(var text in GetComponentsInChildren<PremiumText>(true))text.Apply();
  foreach(var control in GetComponentsInChildren<PremiumControl>(true))control.Apply();
  foreach(Transform child in home.transform){child.gameObject.SetActive(false);Destroy(child.gameObject);}BuildHome();RefreshHome();
  if(navigation.Dialog!=GameDialog.None)RenderDialog();
  if(session!=null && tiles!=null)Refresh();
 }
 void RefreshHome(){var resume=home.transform.Find("Resume");if(resume!=null)resume.gameObject.SetActive(session!=null && !session.board.Over);if(session!=null && resumeDetails!=null)SetText(resumeDetails,"{0} × {0}  ·  Score {1}",session.board.size,session.board.score);}
 void Brand(Transform parent,Vector2 position,float scale) {
  var group=Rect("Brand",parent,position,new Vector2(340,86));group.localScale=Vector3.one*scale;
  var mark=Panel("Tile monogram",group,new Vector2(-124,0),new Vector2(70,70),Color.white);mark.sprite=brandMark;mark.type=Image.Type.Simple;
  var wordmark=Label("Wordmark",group,"2048",new Vector2(35,2),new Vector2(245,86),66);wordmark.GetComponent<PremiumText>().Scales=false;Emphasis(wordmark);wordmark.color=Gold;
 }
 void LevelCard(int size,float y,string title,string description) {
  int levelIndex=size-3;
  var button=Button("Level "+levelIndex,modalPanel,new Vector2(0,y),new Vector2(456,134),()=>StartLevel(size));SetText(button.GetComponentInChildren<Text>(),"");
  bool current=session!=null && session.board.size==size && !session.board.Over;
  if(current){var border=Rect("Current board frame",button.transform,Vector2.zero,new Vector2(450,128)).gameObject.AddComponent<PremiumSelectionFrame>();border.color=Gold;border.raycastTarget=false;}
  var preview=Panel("Difficulty preview",button.transform,new Vector2(-158,0),new Vector2(104,104),Hex("0A1220"));
  float cell=90f/size;int targetVal=size==4?2048:size==5?4096:8192;
  for(int i=0;i<size*size;i++){
   int row=i/size,col=i%size;Vector2 pos=new Vector2(-45f+cell*0.5f+col*cell,45f-cell*0.5f-row*cell);
   bool isAccent=(row==size-1 && col==size-1);bool isSub=(row==0 && col==0);
   var c=Panel("Cell "+i,preview.transform,pos,Vector2.one*(cell-2.5f),isAccent?Gold:isSub?Hex("D4AF37"):Hex("1E2E44"));
   if(isAccent){c.sprite=TileSprite(targetVal);c.type=Image.Type.Simple;}
  }
  var levelTag=Label("Level tag",button.transform,"LEVEL "+levelIndex,new Vector2(38,36),new Vector2(280,22),13);levelTag.color=Gold;levelTag.alignment=TextAnchor.MiddleLeft;Emphasis(levelTag);
  var heading=Label("Difficulty",button.transform,title.ToUpper()+"  "+size+"×"+size,new Vector2(38,12),new Vector2(280,30),22);Emphasis(heading);heading.alignment=TextAnchor.MiddleLeft;heading.color=current?Gold:White;
  var detail=Label("Goal",button.transform,"Target {0}  ·  Best {1}",new Vector2(38,-14),new Vector2(280,24),15,targetVal,stats.best[size-4]);detail.alignment=TextAnchor.MiddleLeft;detail.color=Hex("D4AF37");
  var badge=Label("Difficulty note",button.transform,current?"CURRENT ACTIVE BOARD":description,new Vector2(38,-36),new Vector2(280,22),13);badge.alignment=TextAnchor.MiddleLeft;badge.color=current?Gold:PremiumTheme.Muted;
  AddIcon(button.transform,new Vector2(196,0),24,IconKind.Play,current?Gold:Hex("7A8EA8"));
 }
 void SettingRow(string title,float y,bool enabled,Action action) {
  var button=Button(title,modalPanel,new Vector2(0,y),new Vector2(456,64),action);var label=button.GetComponentInChildren<Text>();label.rectTransform.anchoredPosition=new Vector2(-45,0);label.rectTransform.sizeDelta=new Vector2(320,50);label.alignment=TextAnchor.MiddleLeft;Size(label,20);
  var status=Label("Setting state",button.transform,enabled?"ON":"OFF",new Vector2(142,0),new Vector2(75,45),17);status.color=enabled?Gold:PremiumTheme.Muted;
  AddIcon(button.transform,new Vector2(192,0),22,enabled?IconKind.Check:IconKind.Menu,status.color);
 }
 void ResultCard(string caption,long value){
  var card=Panel("Result summary",modalPanel,new Vector2(0,105),new Vector2(456,150),Hex("111E31"));
  var border=Rect("Result Border",card.transform,Vector2.zero,new Vector2(452,146)).gameObject.AddComponent<PremiumSelectionFrame>();border.color=Hex("2D4566");border.raycastTarget=false;
  int maxCell=0;if(session!=null && session.board!=null && session.board.cells!=null){foreach(int c in session.board.cells)if(c>maxCell)maxCell=c;}
  int showTile = (caption=="TARGET REACHED") ? (int)value : (maxCell>0 ? maxCell : 2048);
  if(showTile<2)showTile=2;
  var tilePreview = Panel("Result Tile",card.transform,new Vector2(-155,0),new Vector2(88,88),Color.white);
  tilePreview.sprite=TileSprite(showTile);tilePreview.type=Image.Type.Simple;
  var tileNumber=Label("Tile Value",tilePreview.transform,showTile.ToString(),Vector2.zero,new Vector2(84,84),showTile>=1024?22:30);
  Emphasis(tileNumber);tileNumber.color=TileText(showTile);
  var title=Label("Caption",card.transform,caption,new Vector2(48,38),new Vector2(280,28),15);title.color=Gold;title.alignment=TextAnchor.MiddleLeft;Emphasis(title);
  var number=Label("Result value",card.transform,"{0}",new Vector2(48,2),new Vector2(280,50),40,value);Emphasis(number);number.color=White;number.alignment=TextAnchor.MiddleLeft;
  long bestScore = session!=null && session.board!=null ? stats.best[session.board.size-4] : 0;
  bool isNewBest = value >= bestScore && value > 0;
  var subNote=Label("Best note",card.transform,isNewBest?"NEW BEST RECORD!":"BEST: {0}",new Vector2(48,-36),new Vector2(280,24),15,bestScore);
  subNote.color=isNewBest?Gold:PremiumTheme.Muted;subNote.alignment=TextAnchor.MiddleLeft;Emphasis(subNote);
 }
 void MarkSelection(int index) {
  if(selectionFrame!=null){selectionFrame.SetActive(false);Destroy(selectionFrame);}
  var frame=Rect("Selected tile 1",tiles[index].transform,Vector2.zero,Vector2.one*(CellSize-4));selectionFrame=frame.gameObject;
  var graphic=frame.gameObject.AddComponent<PremiumSelectionFrame>();graphic.color=White;graphic.raycastTarget=false;
  var badge=Panel("Selection number",frame,new Vector2(-CellSize*.5f+15,CellSize*.5f-15),new Vector2(26,26),Ink);
  Label("Step",badge.transform,"1",Vector2.zero,new Vector2(26,26),16).color=White;
 }
 void BuildInstructions() {
  const string rules="<b>MOVE & MERGE</b>\nSwipe in any direction. Matching tiles merge once per move. Reach your board's target.\n\n<b>YOUR TOOLS</b>\nUnlock after 2 valid moves. Undo reverses the last action. Swap two different adjacent occupied tiles. Delete removes one tile. Cancel spends nothing.\n\n<b>EARN MORE</b>\nAfter use, hold 128 / 256 / 512 to earn Undo / Swap / Delete on valid moves. Cooldown: 1 second. Lifetime cap: 4 / 5 / 6 per board.\n\n<b>ONE MORE CHANCE</b>\nOne continuation rewinds up to 5 actions and keeps your score.";
  var view=Panel("Rules viewport",modalPanel,new Vector2(0,-8),new Vector2(456,426),new Color(0,0,0,0),true).rectTransform;view.gameObject.AddComponent<RectMask2D>();
  var content=Rect("Rules content",view,Vector2.zero,new Vector2(436,426));content.anchorMin=content.anchorMax=new Vector2(.5f,1);content.pivot=new Vector2(.5f,1);
  var body=Label("Rules",content,rules,Vector2.zero,new Vector2(432,426),20);body.alignment=TextAnchor.UpperLeft;
  body.rectTransform.anchorMin=body.rectTransform.anchorMax=new Vector2(.5f,1);body.rectTransform.pivot=new Vector2(.5f,1);body.rectTransform.anchoredPosition=Vector2.zero;body.rectTransform.sizeDelta=new Vector2(432,body.preferredHeight+20);content.sizeDelta=body.rectTransform.sizeDelta;
  var scroll=view.gameObject.AddComponent<ScrollRect>();scroll.viewport=view;scroll.content=content;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.inertia=!manager.ReducedMotion;scroll.scrollSensitivity=22;
  Label("Scroll hint",modalPanel,"Scroll to read all rules",new Vector2(0,-252),new Vector2(456,28),14).color=PremiumTheme.Muted;
 }
 void CloseModal(){navigation.Back();manager.ShowResult();}
 void OnDestroy() {
  if(instance!=this)return;
  instance=null;
  if(manager!=null){manager.Changed-=OnGameChanged;navigation.Changed-=OnNavigationChanged;}
  if(moveTone!=null)Destroy(moveTone);
  if(mergeTone!=null)Destroy(mergeTone);
  if(clickTone!=null)Destroy(clickTone);
  if(winTone!=null)Destroy(winTone);
  foreach(var kv in tileSprites)if(kv.Value!=null){if(kv.Value.texture!=null)Destroy(kv.Value.texture);Destroy(kv.Value);}
  tileSprites.Clear();
  if(rounded!=null){if(rounded.texture!=null)Destroy(rounded.texture);Destroy(rounded);}
  if(contrastTile!=null){if(contrastTile.texture!=null)Destroy(contrastTile.texture);Destroy(contrastTile);}
 }
}
}
