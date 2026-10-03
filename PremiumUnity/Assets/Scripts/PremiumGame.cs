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
 Font font;Sprite rounded,brandMark;CanvasScaler scaler;
 RectTransform safeRoot, root, grid, modalPanel;
 PremiumMergeFeedback mergeFeedback;
 GameObject home, game, loading, modal, cancelSelection, expeditionMap;
 Text score,best,level,hint,addition,soundLabel,loadText;Image loadFill;
 Image[] tiles;Text[] numbers;Button[] powers=new Button[3];Text[] powerLabels=new Text[3];Text[] powerStatuses=new Text[3];Text resumeDetails;GameObject selectionFrame;
 readonly Dictionary<int,Sprite> tileSprites=new Dictionary<int,Sprite>();
 readonly List<RectTransform> ghostPool=new List<RectTransform>();
 readonly List<RectTransform> activeGhosts=new List<RectTransform>();
 bool ready,animating,appPaused,focusLost;
 GameScreen? presentedScreen; GameDialog presentedDialog; bool boardWasExpedition;
 Direction? queuedMove;
 int selected=-1,pendingDimension=4,powerFirst=-1,powerSecond=-1;int[] powerBefore;
 Coroutine moveAnimation,scoreAnimation,boardPulseAnimation;AudioSource audioSource;AudioClip moveTone,mergeTone,clickTone,winTone,slideTone,undoTone,swapTone,deleteTone,lossTone,spawnTone;
 readonly Dictionary<int,AudioClip> mergeClips=new Dictionary<int,AudioClip>();
 int lastWidth,lastHeight;Rect lastSafe;float lastBannerHeight;
 Color Gold=>PremiumTheme.Accent;Color Ink=>PremiumTheme.Ink;Color White=>PremiumTheme.Text;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Boot() { if(FindFirstObjectByType<PremiumGame>()==null) new GameObject("2048 Premium").AddComponent<PremiumGame>(); }
 void Awake() {
  if(instance!=null && instance!=this){ReleaseObject(this);return;}instance=this;
  manager=GetComponent<GameManager>();if(manager==null)manager=gameObject.AddComponent<GameManager>();
  controller=GetComponent<GameController>();if(controller==null)controller=gameObject.AddComponent<GameController>();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
  if((Array.IndexOf(Environment.GetCommandLineArgs(),"-premiumSmoke")>=0 || Array.IndexOf(Environment.GetCommandLineArgs(),"-premiumMotionSmoke")>=0)){persistenceNamespace="Premium2048.QA";new GamePersistence(persistenceNamespace).Reset();}
#endif
  manager.Initialize(persistenceNamespace);
  manager.Changed+=OnGameChanged;navigation.Changed+=OnNavigationChanged;
  Application.targetFrameRate=60;QualitySettings.vSyncCount=0;Application.runInBackground=false;Screen.sleepTimeout=SleepTimeout.SystemSetting;font=Resources.Load<Font>("Fonts/NotoSans-Regular");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
  if((Array.IndexOf(Environment.GetCommandLineArgs(),"-premiumSmoke")>=0 || Array.IndexOf(Environment.GetCommandLineArgs(),"-premiumMotionSmoke")>=0))Application.runInBackground=true;
#endif
  rounded=RoundedSprite();brandMark=Resources.Load<Sprite>("Brand/Mark");
  if(brandMark==null){var mt=Resources.Load<Texture2D>("Brand/Mark");if(mt!=null)brandMark=Sprite.Create(mt,new Rect(0,0,mt.width,mt.height),new Vector2(0.5f,0.5f));}
  BuildShell();
  if(rewardProvider==null) rewardProvider=GetComponent<RewardProvider>();
  if(rewardProvider==null) rewardProvider=gameObject.AddComponent<AdMobProvider>();
  BuildHome();BuildGame();BuildExpeditionMap();
  audioSource=gameObject.AddComponent<AudioSource>();audioSource.playOnAwake=false;audioSource.volume=1f;
  if(GetComponent<AudioListener>()==null && FindFirstObjectByType<AudioListener>()==null) gameObject.AddComponent<AudioListener>();
  slideTone=PremiumAudio.Slide();undoTone=PremiumAudio.Undo();swapTone=PremiumAudio.Swap();
  deleteTone=PremiumAudio.Delete();lossTone=PremiumAudio.Loss();spawnTone=PremiumAudio.Spawn();
  winTone=PremiumAudio.Win();clickTone=PremiumAudio.Click();
  moveTone=slideTone;mergeTone=PremiumAudio.Merge(4);mergeClips[4]=mergeTone;
  ready=true;ShowHome();FitScreen(true);
 }
 void Progress(float value,string text) {if(loadFill!=null)loadFill.fillAmount=value;if(loadText!=null)loadText.text=text+"\n"+Mathf.RoundToInt(value*100)+"%";}
 void Update() {
  FitScreen();if(!ready)return;
  if(Input.GetKeyDown(KeyCode.Escape)) {
   if(rewardPending || navigation.Suspended)return;
   if(navigation.Dialog!=GameDialog.None)CloseModal();
   else if(selection>=0){CancelSelection();Refresh();}
   else if(navigation.Screen==GameScreen.Game||navigation.Screen==GameScreen.Expedition)ShowMenu();
   else if(navigation.Screen==GameScreen.ExpeditionMap)ShowHome();
   return;
  }
  if(animating && navigation.State==GameState.Animating && !navigation.Suspended){controller.Poll(grid,false,QueueMove,_=>{},ConfirmRestart);return;}
  if(!Playing)return;
  controller.Poll(grid,selection>=0,Move,SelectCell,ConfirmRestart);
 }
 bool Playing {
  get {
   if(!ready || animating || !navigation.AcceptsBoardInput)return false;
   if(manager.Mode==GameMode.Classic)return session!=null && !session.board.Blocked;
   return manager.Expedition!=null && !manager.Expedition.completed && !manager.Expedition.failed;
  }
 }
 void QueueMove(Direction direction){if(!queuedMove.HasValue && animating && navigation.State==GameState.Animating && !navigation.Suspended)queuedMove=direction;}
 void Move(Direction direction){if(ready)manager.Move(direction,Time.realtimeSinceStartupAsDouble);}
 void OnGameChanged(GameChange change) {
  if(!ready)return;
  if(change==GameChange.Settings){ApplyPresentation();return;}
  if(manager.Mode==GameMode.Expedition){
   if(change==GameChange.Move && manager.ReducedMotion){StopScore();Refresh(false);PremiumHaptics.Trigger(HapticImpact.Light);manager.ResolveAnimation();return;}
   if(change==GameChange.Move){
    AudioClip clip=slideTone;
    if(manager.Expedition!=null && manager.Expedition.events.Exists(e=>e.kind==MissionTurnEventKind.LockDamaged||e.kind==MissionTurnEventKind.LockOpened)){
     clip=mergeTone;
    }
    Play(slideTone);
    moveAnimation=StartCoroutine(AnimateExpeditionMove(clip));
    return;
   }
   if(change==GameChange.NewGame||change==GameChange.Undo){StopScore();BuildBoard();}
   Refresh();
   return;
  }
  if(change==GameChange.Move && manager.ReducedMotion){StopScore();Refresh(false);PremiumHaptics.Trigger(manager.LastScoreGain>0?HapticImpact.Medium:HapticImpact.Light);manager.ResolveAnimation();return;}
  if(change==GameChange.Move){
   AudioClip clip=slideTone;
   if(manager.LastScoreGain>0){
    int maxMerge=4;
    if(session!=null && session.board!=null && session.board.merges!=null){
     foreach(int idx in session.board.merges){
      if(idx>=0 && idx<session.board.cells.Length && session.board.cells[idx]>maxMerge)maxMerge=session.board.cells[idx];
     }
    }
    if(!mergeClips.TryGetValue(maxMerge,out clip)){clip=PremiumAudio.Merge(maxMerge);mergeClips[maxMerge]=clip;}
   }
   Play(slideTone);
   moveAnimation=StartCoroutine(AnimateMove(manager.LastScoreGain,clip));
   return;
  }
  if(change==GameChange.NewGame || change==GameChange.Undo || change==GameChange.Continue){StopScore();BuildBoard();}
  Refresh();
  if((change==GameChange.Swap || change==GameChange.Delete) && powerBefore!=null && !manager.ReducedMotion && navigation.Dialog==GameDialog.None){moveAnimation=StartCoroutine(AnimatePower(change));}
  powerBefore=null;
 }
 void OnNavigationChanged() {
  if(navigation.Suspended || (navigation.State!=GameState.Playing && navigation.State!=GameState.Animating && navigation.State!=GameState.Selecting)){controller.Cancel();queuedMove=null;}if(!ready)return;
  bool screenChanged=presentedScreen!=navigation.Screen;
  bool dialogChanged=presentedDialog!=navigation.Dialog;
  presentedScreen=navigation.Screen;presentedDialog=navigation.Dialog;
  home.SetActive(navigation.Screen==GameScreen.Home);
  game.SetActive(navigation.Screen==GameScreen.Game || navigation.Screen==GameScreen.Expedition);
  if(expeditionMap!=null)expeditionMap.SetActive(navigation.Screen==GameScreen.ExpeditionMap);
  loading.SetActive(navigation.Screen==GameScreen.Loading);FitScreen(true);
  if(navigation.State==GameState.Animating)return;
  if(animating)FinishAnimation();
  if(selection<0)ClearSelectionVisuals();
  if(navigation.Screen==GameScreen.Home && screenChanged)RebuildHome();
  if(navigation.Screen==GameScreen.ExpeditionMap && screenChanged){RefreshExpeditionMap();FitExpeditionMap();}
  if(screenChanged && !manager.ReducedMotion && (navigation.Screen==GameScreen.Game || navigation.Screen==GameScreen.Expedition)){var reveal=game.GetComponent<PremiumPanelMotion>();if(reveal==null)reveal=game.AddComponent<PremiumPanelMotion>();reveal.Reveal(.985f);}
  if(navigation.Screen==GameScreen.ExpeditionMap && screenChanged){var entrance=expeditionMap.GetComponent<PremiumPanelMotion>();if(entrance==null)entrance=expeditionMap.AddComponent<PremiumPanelMotion>();entrance.Reveal();}
  if(navigation.Screen==GameScreen.Home && screenChanged){var motion=home.GetComponent<PremiumHomeMotion>();if(motion==null)motion=home.AddComponent<PremiumHomeMotion>();if(navigation.Dialog==GameDialog.None)motion.Reveal(manager);}
  modal.SetActive(navigation.Dialog!=GameDialog.None);
  if(navigation.Dialog!=GameDialog.None && (dialogChanged || screenChanged))RenderDialog();
  if((navigation.Screen==GameScreen.Game || navigation.Screen==GameScreen.Expedition) && manager.ActiveSession!=null){BuildBoard();Refresh();}
 }
 IEnumerator AnimateExpeditionMove(AudioClip contactTone) {
  yield return AnimateMove(manager.LastScoreGain,contactTone);
 }
 IEnumerator AnimateMove(long gain,AudioClip contactTone) {
  mergeFeedback?.Stop();
  bool expedition=manager.Mode==GameMode.Expedition;
  var motions=expedition?manager.Expedition.lastTurn.motions:session.board.motions;
  var merges=expedition?manager.Expedition.lastTurn.merges:session.board.merges;
  int spawned=expedition?-1:session.board.spawned;
  if(expedition)foreach(var turnEvent in manager.Expedition.events)if(turnEvent.kind==MissionTurnEventKind.Spawn)spawned=turnEvent.cell;
  long finalScore=manager.ActiveSession.Score;
  animating=true;var moving=activeGhosts;
  for(int i=0;i<tiles.Length;i++)tiles[i].gameObject.SetActive(false);
  foreach(var motion in motions)AcquireGhost(grid,motion.from,motion.value);
  yield return null;
  float elapsed=0;
  const float slideDuration=0.11f,settleDuration=0.10f;
  while(elapsed<slideDuration) {
   elapsed+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(elapsed/slideDuration);float ease=1f-Mathf.Pow(1f-t,3f);
   for(int i=0;i<moving.Count;i++)moving[i].anchoredPosition=Vector2.Lerp(CellPosition(motions[i].from),CellPosition(motions[i].to),ease);
   yield return null;
  }
  ClearGhosts();Refresh(false);if(!expedition)ScoreGain(gain);
  if(gain>0){
   Play(contactTone);
   if(mergeFeedback==null){var effect=Rect("Merge Feedback",grid,Vector2.zero,grid.sizeDelta);mergeFeedback=effect.gameObject.AddComponent<PremiumMergeFeedback>();}
   mergeFeedback.Begin(CellSize);foreach(int index in merges)mergeFeedback.Add(CellPosition(index));
   if(!expedition)SetText(score,"{0}",finalScore-gain);
  }
  PremiumHaptics.Trigger(gain>0?HapticImpact.Medium:HapticImpact.Light);
  if(spawned>=0)tiles[spawned].transform.localScale=Vector3.zero;
  elapsed=0;
  while(elapsed<settleDuration) {
   elapsed+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(elapsed/settleDuration);
   if(gain>0 && !expedition)SetText(score,"{0}",finalScore-gain+(long)Math.Round(gain*(double)(1f-Mathf.Pow(1f-t,2f))));
   float pop=1f+0.16f*Mathf.Sin(t*Mathf.PI);
   foreach(int index in merges)tiles[index].transform.localScale=Vector3.one*pop;
   if(spawned>=0)tiles[spawned].transform.localScale=Vector3.one*(1f-Mathf.Pow(1f-t,2f));
   yield return null;
  }
  animating=false;moveAnimation=null;manager.ResolveAnimation();Refresh();
  if(queuedMove.HasValue && Playing){var next=queuedMove.Value;queuedMove=null;Move(next);}else queuedMove=null;
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
  tile.SetAsLastSibling();
  tile.anchoredPosition=CellPosition(index);tile.sizeDelta=Vector2.one*CellSize;
  var img=tile.GetComponent<Image>();if(img!=null){img.sprite=TileSprite(value);img.color=Color.white;tile.localScale=Vector3.one;}
  var txt=tile.GetComponentInChildren<Text>();if(txt!=null){txt.rectTransform.sizeDelta=Vector2.one*(CellSize-8);Size(txt,BoardDimension==4?39:BoardDimension==5?31:26);SetText(txt,"{0}",value);txt.color=TileText(value);}
  activeGhosts.Add(tile);return tile;
 }
 void ClearGhosts(){foreach(var ghost in activeGhosts)if(ghost!=null && ghost.gameObject!=null){ghost.gameObject.SetActive(false);ghostPool.Add(ghost);}activeGhosts.Clear();}
 void FinishAnimation() {
  if(moveAnimation!=null)StopCoroutine(moveAnimation);moveAnimation=null;animating=false;queuedMove=null;controller.Cancel();ClearGhosts();mergeFeedback?.Stop();
  if(boardPulseAnimation!=null)StopCoroutine(boardPulseAnimation);boardPulseAnimation=null;
  if(tiles!=null)foreach(var tile in tiles){tile.gameObject.SetActive(true);tile.transform.localScale=Vector3.one;}
  if(grid!=null)FitScreen(true);
 }
 void NewGame(int dimension){if(rewardPending || navigation.Suspended)return;FinishAnimation();manager.StartGame(dimension);}
 void ShowGame(){manager.Resume();}
 void ShowHome(){manager.Home();}
 void ShowExpeditionMap(){manager.ExpeditionMap();}
 void OnApplicationPause(bool paused){appPaused=paused;if(paused && audioSource!=null)audioSource.Stop();if(manager!=null){controller.Cancel();manager.SetBackground(appPaused || focusLost);}}
 void OnApplicationFocus(bool focused){focusLost=!focused;if(!focused && audioSource!=null)audioSource.Stop();if(manager!=null){controller.Cancel();manager.SetBackground(appPaused || focusLost);}}
 void OnApplicationQuit(){if(manager!=null)manager.Save();}
 void Refresh(bool results=true) {
  var scoreCaption=game.transform.Find("SCORE/Caption")?.GetComponent<Text>();
  var bestCaption=game.transform.Find("BEST/Caption")?.GetComponent<Text>();
  if(scoreCaption!=null)SetText(scoreCaption,manager.Mode==GameMode.Expedition?"ACTIONS":"SCORE");
  if(bestCaption!=null)SetText(bestCaption,manager.Mode==GameMode.Expedition?"STARS":"BEST");
  if(manager.Mode==GameMode.Expedition){
   if(manager.Expedition==null || tiles==null)return;
   var exp=manager.Expedition;
   SetText(score,"{0} / {1}",exp.actions,exp.mission.actionLimit);
   SetText(best,exp.completed?(exp.CalculateStars()+" ★"):"★ ★ ★");
   SetText(level,"{0}  ·  Chapter {1}",exp.mission.title,exp.mission.chapter);
   for(int i=0;i<tiles.Length;i++){
    int v=(i<exp.tiles.Length)?exp.tiles[i]:0;
    int lockHp=(i<exp.terrain.Length)?exp.terrain[i]:0;
    var lockIcon=tiles[i].transform.Find("Terrain lock");if(lockIcon!=null)lockIcon.gameObject.SetActive(lockHp>0);
    numbers[i].rectTransform.anchoredPosition=lockHp>0?new Vector2(0,-25):Vector2.zero;
    if(lockHp>0){
     tiles[i].gameObject.SetActive(true);
     tiles[i].sprite=rounded;
     tiles[i].color=Hex("182428");
     tiles[i].transform.localScale=Vector3.one;
     SetText(numbers[i],"{0}",lockHp);
     numbers[i].color=Gold;
    } else if(v>0){
     tiles[i].gameObject.SetActive(true);
     tiles[i].sprite=TileSprite(v);
     tiles[i].color=Color.white;
     tiles[i].transform.localScale=Vector3.one;
     SetText(numbers[i],"{0}",v);
     numbers[i].color=TileText(v);
    } else {
     tiles[i].gameObject.SetActive(false);
    }
   }
   for(int i=0;i<3;i++){
    bool active=selection==i;
    int available=exp.tools[i];
    powers[i].interactable=!animating && !navigation.Suspended && exp.CanUseTool((Power)i);
    powers[i].GetComponent<PremiumControl>().State(active,available<=0);
    SetText(powerLabels[i],"{0}",available);powerLabels[i].color=active?Gold:Ink;
    var badge=powers[i].transform.Find("Count Badge");
    if(badge!=null){badge.gameObject.SetActive(available>0);badge.GetComponent<Image>().color=active?Hex("192327"):Gold;}
    var iconImg=powers[i].transform.Find("Icon")?.GetComponent<PremiumIcon>();
    if(iconImg!=null)iconImg.color=active?Hex("192327"):available>0?White:PremiumTheme.Muted;
    var toolLabel=powers[i].transform.Find("ToolName")?.GetComponent<Text>();
    if(toolLabel!=null)toolLabel.color=active?Hex("192327"):White;
    SetText(powerStatuses[i],active?"ACTIVE":available>0?"READY":"EMPTY");
    powerStatuses[i].color=active?Hex("192327"):available>0?PremiumTheme.Cyan:PremiumTheme.Muted;
   }
   cancelSelection.SetActive(selection>=0);FitScreen(true);
   if(selection<0)SetText(hint,exp.mission.description);
   else SetText(hint,selection==(int)Power.Swap?"Tap two different adjacent tiles to swap":"Tap a tile to remove");
   if(results && navigation.State!=GameState.Animating)manager.ShowResult();
   return;
  }
  if(session==null || tiles==null)return;Board b=session.board;SetText(score,"{0}",b.score);SetText(best,"{0}",stats.best[b.size-4]);
  SetText(level,"Level {0}  /  {1} × {1}  /  Target {2}",b.size-3,b.size,b.target);
  for(int i=0;i<tiles.Length;i++){int v=b.cells[i];tiles[i].gameObject.SetActive(v!=0);tiles[i].sprite=TileSprite(v);tiles[i].color=Color.white;tiles[i].transform.localScale=Vector3.one;SetText(numbers[i],"{0}",v);numbers[i].color=TileText(v);}
  for(int i=0;i<3;i++) {
   bool active=selection==i,locked=!session.Unlocked;
   int available=session.stock[i].available;
   powers[i].interactable=!animating && !navigation.Suspended && session.CanUse((Power)i);
   powers[i].GetComponent<PremiumControl>().State(active,locked||available<=0);
   SetText(powerLabels[i],"{0}",available);powerLabels[i].color=active?Gold:Ink;
   var badge=powers[i].transform.Find("Count Badge");
   if(badge!=null){badge.gameObject.SetActive(!locked&&available>0);badge.GetComponent<Image>().color=active?Hex("192327"):Gold;}
   var iconImg=powers[i].transform.Find("Icon")?.GetComponent<PremiumIcon>();
   if(iconImg!=null)iconImg.color=active?Hex("192327"):locked||available<=0?PremiumTheme.Muted:White;
   var toolLabel=powers[i].transform.Find("ToolName")?.GetComponent<Text>();
   if(toolLabel!=null)toolLabel.color=active?Hex("192327"):White;
   string st=active?"ACTIVE":locked?"LOCKED":available>0?"READY":"EMPTY";
   SetText(powerStatuses[i],st);
   powerStatuses[i].color=active?Hex("192327"):locked?PremiumTheme.Muted:available>0?PremiumTheme.Cyan:PremiumTheme.Muted;
  }
  cancelSelection.SetActive(selection>=0);FitScreen(true);
  if(selection<0)SetText(hint,session.Unlocked?"Hold 128 / 256 / 512 to earn tools":"Tools unlock after {0} more moves",Math.Max(0,2-session.moves));
  else SetText(hint,selection==(int)Power.Swap?"Tap two different adjacent tiles":"Tap the tile to remove · Cancel to exit");
  foreach(var control in GetComponentsInChildren<PremiumControl>(true))if(control.AccessibleName=="Sound: On"){var icon=control.GetComponentInChildren<PremiumIcon>();if(icon!=null)icon.Kind=sound?IconKind.Sound:IconKind.Mute;}
  if(results && navigation.State!=GameState.Animating)manager.ShowResult();
 }
 void Result(){manager.ShowResult();}
 void ConfirmRestart(){
  if(!rewardPending){
   if(manager.Mode==GameMode.Expedition)navigation.Open(GameDialog.Restart);
   else if(session!=null)navigation.Open(GameDialog.Restart);
  }
 }
 void ShowMenu(){if(!rewardPending)navigation.Open(GameDialog.Pause);}
 void StartLevel(int dimension){
  if(navigation.Suspended || rewardPending)return;
  if(session!=null && !session.board.Over && session.moves>0){pendingDimension=dimension;navigation.Open(GameDialog.ReplaceBoard);return;}
  NewGame(dimension);
 }
 void ShowLevels(){navigation.Open(GameDialog.Levels);}
 void ShowInstructions(){navigation.Open(GameDialog.Instructions);}
 void ShowScores(){navigation.Open(GameDialog.Scores);}
 void ShowSettings(){navigation.Open(GameDialog.Settings);}
 void RenderDialog() {
  switch(navigation.Dialog) {
   case GameDialog.Pause:
    if(manager.Mode==GameMode.Expedition && manager.Expedition!=null){
     Modal("Paused",manager.Expedition.mission.title);
     DialogButton("Resume",148,ShowGame,true);
     DialogButton("Restart mission",72,()=>manager.RetryMission());
     DialogButton("Journey map",-4,ShowExpeditionMap);
     DialogButton("Settings",-80,ShowSettings);
     DialogButton("Home",-156,ShowHome);
     break;
    }
    Modal("Paused","Take a breath. Your board is saved.");
    DialogButton("Resume",148,ShowGame,true);
    DialogButton("Change level",72,ShowLevels);
    DialogButton("Restart",-4,ConfirmRestart);
    DialogButton("How to Play",-80,ShowInstructions);
    DialogButton("High Scores",-156,ShowScores);
    DialogButton("Settings",-232,ShowSettings);
    DialogButton("Home",-308,ShowHome);break;
   case GameDialog.Levels:
    Modal("Choose your board",session!=null?"Starting a board replaces your saved game.":"Three boards. One satisfying challenge.");
    LevelCard(4,136,"Classic","Short and focused");
    LevelCard(5,-8,"Advanced","Room to build");
    LevelCard(6,-152,"Expert","Think a few moves ahead");
    Button("Back",modalPanel,new Vector2(0,-285),new Vector2(240,64),CloseModal);break;
   case GameDialog.Instructions:
    Modal("How to play","Simple moves. Thoughtful choices.");
    BuildInstructions();
    Button("Close",modalPanel,new Vector2(0,-285),new Vector2(200,72),CloseModal,true);break;
   case GameDialog.Scores:
    Modal("Your records","Every personal best, saved on this device.");
    string[] modeNames=new[]{"CLASSIC 4×4","ADVANCED 5×5","EXPERT 6×6"};
    int[] targets=new[]{2048,4096,8192};
    for(int i=0;i<3;i++){
     var card=Panel("Record "+i,modalPanel,new Vector2(0,140-i*92),new Vector2(456,82),Hex("223035"));
     var border=Rect("Record Border",card.transform,Vector2.zero,new Vector2(452,78)).gameObject.AddComponent<PremiumSelectionFrame>();border.color=Hex("3A4B50");border.raycastTarget=false;
     var emblem=Panel("Trophy Emblem",card.transform,new Vector2(-172,0),new Vector2(52,52),Hex("192327"));
     AddIcon(emblem.transform,Vector2.zero,26,IconKind.Trophy,Gold);
     var modeTitle=Label("Mode",card.transform,modeNames[i],new Vector2(-30,12),new Vector2(190,26),18);Emphasis(modeTitle);modeTitle.alignment=TextAnchor.MiddleLeft;
     var targetLabel=Label("Grid",card.transform,"Target {0}  ·  Level {1}",new Vector2(-30,-14),new Vector2(190,22),13,targets[i],i+1);targetLabel.color=PremiumTheme.Muted;targetLabel.alignment=TextAnchor.MiddleLeft;
     var scoreBox=Panel("Score Box",card.transform,new Vector2(148,0),new Vector2(128,58),Hex("192327"));
     var scoreCap=Label("Score Cap",scoreBox.transform,"RECORD",new Vector2(0,14),new Vector2(120,18),11);scoreCap.color=Gold;Emphasis(scoreCap);
     var scoreVal=Label("Best score",scoreBox.transform,stats.best[i].ToString("N0"),new Vector2(0,-8),new Vector2(120,32),22);Emphasis(scoreVal);scoreVal.color=White;
    }
    var statsCard=Panel("Summary Card",modalPanel,new Vector2(0,-132),new Vector2(456,72),Hex("223035"));
    var statsBorder=Rect("Stats Border",statsCard.transform,Vector2.zero,new Vector2(452,62)).gameObject.AddComponent<PremiumSelectionFrame>();statsBorder.color=Hex("3A4B50");statsBorder.raycastTarget=false;
    int totalP=stats.played, totalW=stats.won, winR=totalP==0?0:Mathf.RoundToInt(100f*totalW/totalP);
    var pBox=Rect("Played Col",statsCard.transform,new Vector2(-140,0),new Vector2(130,58));
    Label("Played Cap",pBox,"PLAYED",new Vector2(0,14),new Vector2(130,18),11).color=PremiumTheme.Muted;
    var pVal=Label("Played Val",pBox,totalP.ToString(),new Vector2(0,-10),new Vector2(130,30),18);Emphasis(pVal);pVal.color=Gold;
    var wBox=Rect("Won Col",statsCard.transform,new Vector2(0,0),new Vector2(130,58));
    Label("Won Cap",wBox,"VICTORIES",new Vector2(0,14),new Vector2(130,18),11).color=PremiumTheme.Muted;
    var wVal=Label("Won Val",wBox,totalW.ToString(),new Vector2(0,-10),new Vector2(130,30),18);Emphasis(wVal);wVal.color=Gold;
    var rBox=Rect("Rate Col",statsCard.transform,new Vector2(140,0),new Vector2(130,58));
    Label("Rate Cap",rBox,"WIN RATE",new Vector2(0,14),new Vector2(130,18),11).color=PremiumTheme.Muted;
    var rVal=Label("Rate Val",rBox,winR.ToString()+"%",new Vector2(0,-10),new Vector2(130,30),18);Emphasis(rVal);rVal.color=Gold;
    Button("Close",modalPanel,new Vector2(-118,-205),new Vector2(210,72),CloseModal,true);
    Button("Clear scores",modalPanel,new Vector2(118,-205),new Vector2(210,72),()=>navigation.Open(GameDialog.ClearScores),false,true);break;
   case GameDialog.Settings:
    Modal("Settings","Make the game comfortable for you");
    SettingRow("Sound",148,sound,ToggleSound);
    SettingRow("Haptic feedback",70,manager.Haptics,ToggleHaptics);
    SettingRow("Reduced motion",-8,manager.ReducedMotion,manager.ToggleReducedMotion);
    SettingRow("Large text",-86,manager.LargeText,manager.ToggleLargeText);
    SettingRow("High contrast",-164,manager.HighContrast,manager.ToggleHighContrast);
    var languageButton=Button("Language",modalPanel,new Vector2(0,-242),new Vector2(456,72),manager.ToggleLanguage);
    SetText(languageButton.GetComponentInChildren<Text>(),"Language · {0}",manager.Language=="es"?"Español":"English");
    Button("Close",modalPanel,new Vector2(-118,-320),new Vector2(210,72),CloseModal,true);
    Button("About",modalPanel,new Vector2(118,-320),new Vector2(210,72),()=>navigation.Open(GameDialog.About));break;
   case GameDialog.About:
    Modal("2048 Premium","A quieter kind of play.");
    Brand(modalPanel,new Vector2(0,100),.9f);
    Label("Credits",modalPanel,"An offline puzzle by GSV Infotech.\nVersion {0}\n\nTypography: Noto Sans\nSIL Open Font License 1.1\n\nYour progress stays on this device.",new Vector2(0,-80),new Vector2(436,260),20,Application.version).color=PremiumTheme.TextSecondary;
    Button("Back",modalPanel,new Vector2(0,-295),new Vector2(240,72),CloseModal,true);break;
   case GameDialog.ClearScores:
    Modal("Clear saved scores?","This clears scores, statistics and the saved game.");
    DialogButton("Clear all",10,manager.ResetProgress,false,true);
    DialogButton("Cancel",-66,CloseModal);break;
   case GameDialog.ReplaceBoard:
    Modal("Start a new board?","Your current board will be replaced. Your records stay saved.");
    DialogButton("Start new board",10,()=>NewGame(pendingDimension),true);
    DialogButton("Keep current board",-66,CloseModal);break;
   case GameDialog.Restart:
    if(manager.Mode==GameMode.Expedition){
     Modal("Restart mission?","Restart this mission with full starting moves and tools.");
     DialogButton("Restart",10,()=>manager.RetryMission(),false,true);
     DialogButton("Cancel",-66,CloseModal);
     break;
    }
    Modal("Restart level?","Start a fresh board and reset power-ups. Your best score stays saved.");
    DialogButton("Restart",10,()=>NewGame(session.board.size),false,true);
    DialogButton("Cancel",-66,CloseModal);break;
   case GameDialog.Win:
    if(manager.Mode==GameMode.Expedition && manager.Expedition!=null){
     Play(winTone);PremiumHaptics.Trigger(HapticImpact.Success);
     Modal("Mission Complete",manager.Expedition.mission.title);
     int stars=manager.Expedition.CalculateStars();
     string starTag=stars==3?"★★★ 3 STARS":stars==2?"★★☆ 2 STARS":"★☆☆ 1 STAR";
     ResultCard(starTag,manager.Expedition.actions);
     if(!manager.ReducedMotion)Rect("Celebration",modalPanel,Vector2.zero,modalPanel.sizeDelta).gameObject.AddComponent<PremiumCelebration>();
     float winY=-35;
     int curIdx=ExpeditionCatalog.Missions.FindIndex(m=>m.id==manager.Expedition.mission.id);
     bool hasNext=curIdx>=0 && curIdx<ExpeditionCatalog.Missions.Count-1;
     if(hasNext){DialogButton("Next mission",winY,()=>manager.NextMission(),true);winY-=75;}
     DialogButton("Replay",winY,()=>manager.RetryMission(),!hasNext);winY-=75;
     DialogButton("Journey map",winY,ShowExpeditionMap);winY-=75;
     DialogButton("Home",winY,ShowHome);
     break;
    }
    Play(winTone);PremiumHaptics.Trigger(HapticImpact.Success);
    Modal("Level complete",T("Target tile reached: {0}",session.board.target));
    ResultCard("YOUR SCORE",session.board.score);
    if(!manager.ReducedMotion)Rect("Celebration",modalPanel,Vector2.zero,modalPanel.sizeDelta).gameObject.AddComponent<PremiumCelebration>();
    float wY=-35;bool hNext=session.board.size<6;
    if(hNext){DialogButton("Next level",wY,()=>NewGame(session.board.size+1),true);wY-=75;}
    if(!session.board.Over){DialogButton("Keep going",wY,()=>manager.KeepGoing(),!hNext);wY-=75;}
    DialogButton("Play again",wY,()=>NewGame(session.board.size),!hNext && session.board.Over);
    DialogButton("Home",wY-75,ShowHome);break;
   case GameDialog.Loss:
    if(manager.Mode==GameMode.Expedition && manager.Expedition!=null){
     Play(lossTone);
     string reason=manager.Expedition.actions>=manager.Expedition.mission.actionLimit?"Action budget exhausted.":"No valid moves remain.";
     Modal("Mission Failed",reason);
     ResultCard("ACTIONS USED",manager.Expedition.actions);
     DialogButton("Retry mission",-35,()=>manager.RetryMission(),true);
     DialogButton("Journey map",-110,ShowExpeditionMap);
     DialogButton("Home",-185,ShowHome);
     break;
    }
    Play(lossTone);
    Modal("A good run.","No moves remain. Keep your record and try again.");
    ResultCard("FINAL SCORE",session.board.score);
    if(session.CanReward){
     DialogButton(rewardProvider==null?"Continue: rewind 5 moves":"Watch ad · Rewind 5 moves",-35,AskReward,true);
     DialogButton("Play again",-110,()=>NewGame(session.board.size));
     DialogButton("Home",-185,ShowHome);
    } else {
     DialogButton("Play again",-45,()=>NewGame(session.board.size),true);
     DialogButton("Home",-125,ShowHome);
    }break;
   case GameDialog.Continue:
    Modal("Continue playing?","Rewind up to 5 previous actions and keep your current score. Available once per game.");
    DialogButton("Continue",10,GrantOfflineReward,true);
    DialogButton("No thanks",-66,CloseModal);break;
   case GameDialog.VideoUnavailable:
    Modal("Video unavailable","The ad provider is not ready. Your board remains saved.");
    DialogButton("Back",0,CloseModal,true);break;
   case GameDialog.RewardVideo:
    Modal("Reward video","Waiting for the ad provider. Reward is granted only after completion.");break;
  }
  FitDialog(modalPanel);
 }
 void ToggleSound(){manager.ToggleSound();if(sound)Play(spawnTone);}
 void ToggleHaptics(){manager.ToggleHaptics();if(manager.Haptics)PremiumHaptics.Trigger(HapticImpact.Medium);}
 void UsePower(Power power){
  if(!Playing)return;
  if(power==Power.Undo){
   if(manager.Undo()){
    Play(undoTone);PremiumHaptics.Trigger(HapticImpact.Light);
    if(!manager.ReducedMotion){if(boardPulseAnimation!=null)StopCoroutine(boardPulseAnimation);boardPulseAnimation=StartCoroutine(PulseBoard());}
   }
  }else manager.BeginSelection(power);
 }
 IEnumerator PulseBoard() {
  if(grid==null)yield break;
  float elapsed=0;
  var restingScale=grid.localScale;
  while(elapsed<0.14f){elapsed+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(elapsed/0.14f);grid.localScale=restingScale*(1f-0.035f*Mathf.Sin(t*Mathf.PI));yield return null;}
  grid.localScale=restingScale;boardPulseAnimation=null;
 }
 void ClearSelectionVisuals(){selected=-1;if(cancelSelection!=null)cancelSelection.SetActive(false);if(selectionFrame!=null){selectionFrame.SetActive(false);ReleaseObject(selectionFrame);selectionFrame=null;}}
 void CancelSelection(){manager.CancelSelection();ClearSelectionVisuals();}
 void SelectCell(Vector2 screen) {
  if(!Playing || selection<0)return;
  Vector2 local;if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(grid,screen,null,out local))return;
  int index=-1;for(int i=0;i<tiles.Length;i++){Vector2 d=local-CellPosition(i);if(Mathf.Abs(d.x)<=CellSize/2 && Mathf.Abs(d.y)<=CellSize/2){index=i;break;}}
  if(index<0)return;
  if(manager.Mode==GameMode.Expedition){
   if(manager.Expedition==null || manager.Expedition.terrain[index]>0 || manager.Expedition.tiles[index]==0)return;
   if(selection==(int)Power.Delete){if(manager.Delete(index)){Play(deleteTone);PremiumHaptics.Trigger(HapticImpact.Medium);}return;}
   if(selected<0){selected=index;MarkSelection(index);SetText(hint,"Tile 1 selected · Tap an adjacent tile");return;}
   if(index==selected){CancelSelection();Refresh();return;}
   if(manager.Swap(selected,index)){Play(swapTone);PremiumHaptics.Trigger(HapticImpact.Medium);}else SetText(hint,"Choose a different tile next to tile 1");
   return;
  }
  if(session.board.cells[index]==0)return;
  if(selection==(int)Power.Delete){powerBefore=(int[])session.board.cells.Clone();powerFirst=index;powerSecond=-1;if(manager.Delete(index)){Play(deleteTone);PremiumHaptics.Trigger(HapticImpact.Medium);}return;}
  if(selected<0){selected=index;MarkSelection(index);SetText(hint,"Tile 1 selected · Tap an adjacent tile");return;}
  if(index==selected){CancelSelection();Refresh();return;}
  powerBefore=(int[])session.board.cells.Clone();powerFirst=selected;powerSecond=index;
  if(manager.Swap(selected,index)){Play(swapTone);PremiumHaptics.Trigger(HapticImpact.Medium);}else SetText(hint,"Choose a different tile next to tile 1");
 }
 void AskReward() {
  if(!session.CanReward || rewardPending)return;
  if(rewardProvider==null){navigation.Open(GameDialog.Continue);return;}
  if(!rewardProvider.IsReady){navigation.Open(GameDialog.VideoUnavailable);return;}
  int request=manager.BeginReward();if(request<0)return;StartCoroutine(RewardTimeout(request));
  try{rewardProvider.Show(success=>manager.CompleteReward(request,success));}
  catch(Exception e){Debug.LogWarning("Reward provider failed: "+e.Message);manager.CompleteReward(request,false);}
 }
 IEnumerator RewardTimeout(int request){yield return new WaitForSecondsRealtime(180);manager.CompleteReward(request,false);}
 void GrantOfflineReward(){if(manager.ContinueOffline()){Play(clickTone);PremiumHaptics.Trigger(HapticImpact.Medium);}}
 IEnumerator AnimatePower(GameChange change) {
  var before=powerBefore;int first=powerFirst,second=powerSecond;
  animating=true;Refresh(false);tiles[first].gameObject.SetActive(false);
  var a=AcquireGhost(grid,first,before[first]);RectTransform b=null;
  if(change==GameChange.Swap){tiles[second].gameObject.SetActive(false);b=AcquireGhost(grid,second,before[second]);}
  float elapsed=0;
  while(elapsed<.16f){elapsed+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(elapsed/.16f);float ease=1-Mathf.Pow(1-t,3);
   if(b!=null){a.anchoredPosition=Vector2.Lerp(CellPosition(first),CellPosition(second),ease);b.anchoredPosition=Vector2.Lerp(CellPosition(second),CellPosition(first),ease);a.localScale=b.localScale=Vector3.one*(1-.08f*Mathf.Sin(t*Mathf.PI));}
   else {a.localScale=Vector3.one*(1-ease);a.GetComponent<Image>().color=new Color(1,1,1,1-t);}
   yield return null;
  }
  ClearGhosts();animating=false;moveAnimation=null;Refresh();
 }
 void StopScore() {if(scoreAnimation!=null)StopCoroutine(scoreAnimation);scoreAnimation=null;SetText(addition,"");}
 void ScoreGain(long gain) {if(gain<=0)return;StopScore();scoreAnimation=StartCoroutine(FloatScore(gain));}
 IEnumerator FloatScore(long gain) {
  SetText(addition,"+{0}",gain);float elapsed=0;
  while(elapsed<0.65f) {elapsed+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(elapsed/0.65f);addition.rectTransform.anchoredPosition=new Vector2(0,25+40*t);addition.color=new Color(Gold.r,Gold.g,Gold.b,1-t);yield return null;}
  SetText(addition,"");scoreAnimation=null;
 }
 void Play(AudioClip clip,float volume=0.85f) {if(sound && clip!=null && audioSource!=null)audioSource.PlayOneShot(clip,Mathf.Clamp01(volume));}
 void FitScreen(bool force=false) {
  if(!force && (safeRoot==null || (lastWidth==Screen.width && lastHeight==Screen.height && lastSafe==Screen.safeArea && lastBannerHeight==AdMobProvider.BannerHeightPixels)))return;
  lastBannerHeight=AdMobProvider.BannerHeightPixels;
  lastWidth=Screen.width;lastHeight=Screen.height;lastSafe=Screen.safeArea;if(lastWidth==0 || lastHeight==0)return;
  var anchors=PremiumLayout.CalculateSafeAnchors(lastWidth,lastHeight,lastSafe);
  safeRoot.anchorMin=new Vector2(anchors.xMin,anchors.yMin);safeRoot.anchorMax=new Vector2(anchors.xMax,anchors.yMax);safeRoot.offsetMin=safeRoot.offsetMax=Vector2.zero;
  scaler.scaleFactor=PremiumLayout.CalculateScale(lastWidth,lastHeight,lastSafe,new Vector2(540,960));
  float safeH=(lastSafe.height>0?lastSafe.height:lastHeight)/Mathf.Max(0.01f,scaler.scaleFactor);
  safeH=Mathf.Max(960f,safeH);
  if(root!=null){root.anchoredPosition=Vector2.zero;root.sizeDelta=new Vector2(540,safeH);}
  if(modalPanel!=null)modalPanel.anchoredPosition=Vector2.zero;
  var bannerSlot=safeRoot.Find("Banner Ad Slot") as RectTransform;
  if(bannerSlot!=null){bannerSlot.sizeDelta=new Vector2(540,PremiumLayout.BannerReserve);bannerSlot.anchoredPosition=new Vector2(0,-safeH*.5f+PremiumLayout.BannerReserve*.5f);}
  if(root!=null)root.sizeDelta=new Vector2(540,safeH);
  if(home!=null)home.GetComponent<RectTransform>().sizeDelta=new Vector2(540,safeH);
  if(game!=null)game.GetComponent<RectTransform>().sizeDelta=new Vector2(540,safeH);
  if(expeditionMap!=null)expeditionMap.GetComponent<RectTransform>().sizeDelta=new Vector2(540,safeH);
  if(loading!=null)loading.GetComponent<RectTransform>().sizeDelta=new Vector2(540,safeH);
  RepositionResponsiveElements(safeH);FitExpeditionMap();
  if(home!=null)home.GetComponent<PremiumHomeMotion>()?.Rebase();
 }
 void RepositionResponsiveElements(float H) {PositionScreens(home,game,modalPanel,H,session!=null && !session.board.Over,selection>=0,CompactDialog);}
 public static void PositionScreens(GameObject home,GameObject game,RectTransform modalPanel,float H,bool hasResume,bool selecting,bool compactDialog) {
  int selection=selecting?0:-1;
  var score=game!=null?game.transform.Find("SCORE/Number")?.GetComponent<Text>():null;
  var best=game!=null?game.transform.Find("BEST/Number")?.GetComponent<Text>():null;
  var grid=game!=null?game.transform.Find("Board") as RectTransform:null;
  var hint=game!=null?game.transform.Find("Hint")?.GetComponent<Text>():null;
  var cancelSelection=game!=null?game.transform.Find("Cancel selection")?.gameObject:null;
  float bannerReserve=PremiumLayout.BannerReserve;
  float topY=H*0.5f-36f;float botY=-H*0.5f+bannerReserve+24f;
  if(home!=null) {
   var hSound=home.transform.Find("Sound: On");if(hSound!=null)hSound.GetComponent<RectTransform>().anchoredPosition=new Vector2(-224,topY);
   var hSettings=home.transform.Find("Settings");if(hSettings!=null)hSettings.GetComponent<RectTransform>().anchoredPosition=new Vector2(224,topY);
   var hBrand=home.transform.Find("Brand");if(hBrand!=null)hBrand.GetComponent<RectTransform>().anchoredPosition=new Vector2(0,topY-58f);
   var hEdition=home.transform.Find("Edition");if(hEdition!=null)hEdition.GetComponent<RectTransform>().anchoredPosition=new Vector2(0,topY-112f);
   float homeTop=H*.5f-36f;
   float statsOffset=hasResume?706f:624f;
   float footerOffset=statsOffset+148f;
   float contentSpan=H-36f-(bannerReserve+30f);
   float stretch=Mathf.Clamp(contentSpan/footerOffset,0.92f,1.25f);
   Place(home.transform,"Sound: On",-224,homeTop);
   Place(home.transform,"Settings",224,homeTop);
   PlaceHome(home.transform,"Brand",0,58,homeTop,stretch);
   PlaceHome(home.transform,"Edition",0,112,homeTop,stretch);
   PlaceHome(home.transform,"Preview Board",0,242,homeTop,stretch,200);
   PlaceHome(home.transform,"New Game",0,394,homeTop,stretch,84);
   PlaceHome(home.transform,"Expedition",-128,520,homeTop,stretch,132);
   PlaceHome(home.transform,"Daily Puzzle",128,520,homeTop,stretch,132);
   var resume=home.transform.Find("Resume");if(resume!=null)resume.gameObject.SetActive(hasResume);
   PlaceHome(home.transform,"Resume",0,636,homeTop,stretch,72);
   PlaceHome(home.transform,"Stats Summary",0,statsOffset,homeTop,stretch,56);
   PlaceHome(home.transform,"How to Play",-128,statsOffset+84,homeTop,stretch,72);
   PlaceHome(home.transform,"High Scores",128,statsOffset+84,homeTop,stretch,72);
   PlaceHome(home.transform,"Footer",0,footerOffset,homeTop,stretch);
  }
  if(game!=null) {
   var menu=game.transform.Find("Menu");if(menu!=null)menu.GetComponent<RectTransform>().anchoredPosition=new Vector2(-224,topY);
   var title=game.transform.Find("Title");if(title!=null)title.GetComponent<RectTransform>().anchoredPosition=new Vector2(-20,topY+6);
   var sound=game.transform.Find("Sound: On");if(sound!=null)sound.GetComponent<RectTransform>().anchoredPosition=new Vector2(140,topY);
   var restart=game.transform.Find("Restart");if(restart!=null)restart.GetComponent<RectTransform>().anchoredPosition=new Vector2(224,topY);
   if(score!=null && score.transform.parent!=null)score.transform.parent.GetComponent<RectTransform>().anchoredPosition=new Vector2(-128,topY-106f);
   if(best!=null && best.transform.parent!=null)best.transform.parent.GetComponent<RectTransform>().anchoredPosition=new Vector2(128,topY-106f);
   var lvlPill=game.transform.Find("Level Pill");if(lvlPill!=null)lvlPill.GetComponent<RectTransform>().anchoredPosition=new Vector2(0,topY-190f);
   if(grid!=null){
    float upper=topY-228,lower=botY+(selection>=0?220:186);
    float scale=Mathf.Min(1,(upper-lower)/520f);
    grid.anchoredPosition=new Vector2(0,(upper+lower)*.5f);grid.localScale=Vector3.one*scale;
   }
   var actionDock=game.transform.Find("Action Bar");if(actionDock!=null)actionDock.GetComponent<RectTransform>().anchoredPosition=new Vector2(0,botY+(selection>=0?144f:110f));
   if(hint!=null)hint.rectTransform.anchoredPosition=new Vector2(0,botY+(selection>=0?64f:28f));
   if(cancelSelection!=null)cancelSelection.GetComponent<RectTransform>().anchoredPosition=new Vector2(0,botY+4f);
  }
  if(modalPanel!=null)FitDialog(modalPanel);
 }
 public static void FitDialog(RectTransform panel) {
  if(panel==null)return;
  var rows=new List<RectTransform>();
  foreach(Transform child in panel){
   if(!child.gameObject.activeSelf || child.name=="Modal Border" || child.name=="Celebration")continue;
   var rect=child as RectTransform;if(rect==null)continue;
   var text=child.GetComponent<Text>();
   if(text!=null && (child.name=="Title" || child.name=="Description")){
    rect.sizeDelta=new Vector2(452,Mathf.Max(child.name=="Title"?48:44,text.preferredHeight+8));
   }
   rows.Add(rect);
  }
  rows.Sort((a,b)=>b.anchoredPosition.y.CompareTo(a.anchoredPosition.y));
  float cursor=28f;
  for(int i=0;i<rows.Count;){
   int end=i+1;float originalY=rows[i].anchoredPosition.y;
   while(end<rows.Count && Mathf.Abs(rows[end].anchoredPosition.y-originalY)<.5f)end++;
   float height=0;for(int j=i;j<end;j++)height=Mathf.Max(height,rows[j].sizeDelta.y);
   for(int j=i;j<end;j++)rows[j].anchoredPosition=new Vector2(rows[j].anchoredPosition.x,-cursor-height*.5f);
   cursor+=height+(rows[i].name=="Title"?8f:rows[i].name=="Description"?24f:12f);
   i=end;
  }
  float total=cursor-12f+28f;
  panel.sizeDelta=new Vector2(500,total);
  foreach(var rect in rows)rect.anchoredPosition+=new Vector2(0,total*.5f);
  var celebration=panel.Find("Celebration") as RectTransform;if(celebration!=null)celebration.sizeDelta=panel.sizeDelta;
 }
 static void PlaceHome(Transform parent,string name,float x,float offset,float top,float stretch,float height=0){
  var rect=parent.Find(name) as RectTransform;if(rect==null)return;
  rect.anchoredPosition=new Vector2(x,top-offset*stretch);
  if(height>0)rect.sizeDelta=new Vector2(rect.sizeDelta.x,height*Mathf.Min(stretch,1.4f));
 }
 static void Place(Transform parent,string name,float x,float y){var rect=parent.Find(name) as RectTransform;if(rect!=null)rect.anchoredPosition=new Vector2(x,y);}
 void FitExpeditionMap(){
  if(expeditionMap==null || root==null)return;
  float height=root.sizeDelta.y,top=height*.5f-36;
  Place(expeditionMap.transform,"Back",-224,top);Place(expeditionMap.transform,"Map Title",-20,top);
  Place(expeditionMap.transform,"Sound: On",140,top);Place(expeditionMap.transform,"Settings",224,top);
  var resume=expeditionMap.transform.Find("Resume expedition");bool hasResume=resume!=null;
  Place(expeditionMap.transform,"Resume expedition",0,top-92);
  var viewport=expeditionMap.transform.Find("Map Viewport") as RectTransform;
  if(viewport!=null){float upper=top-(hasResume?148:58),lower=-height*.5f+PremiumLayout.BannerReserve+16f;viewport.sizeDelta=new Vector2(500,upper-lower);viewport.anchoredPosition=new Vector2(0,(upper+lower)*.5f);}
 }
 static Color Hex(string value) {
  switch(value){case "192327":return PremiumTheme.Background;case "223035":return PremiumTheme.Surface;case "2B3A3F":return PremiumTheme.SurfaceElevated;case "3A4B50":return PremiumTheme.Border;case "253236":return PremiumTheme.Border;case "314247":return PremiumTheme.Color("203040");case "182428":case "121A1D":return PremiumTheme.Background;case "E5BB7A":return PremiumTheme.Accent;case "B1BEBA":return PremiumTheme.Muted;}
  ColorUtility.TryParseHtmlString("#"+value,out var c);return c;
 }
 static Color TileColor(int value) {return value==0?Hex("314247"):Color.white;}
 Color TileText(int value){return PremiumTheme.TileText(value,manager.HighContrast);}
 Sprite TileSprite(int value) {
  if(tileSprites.TryGetValue(value,out var sprite))return sprite;
  PremiumTheme.TileColors(value,out var first,out var second);sprite=GradientRounded(first,second);tileSprites[value]=sprite;return sprite;
 }
 Sprite GradientRounded(Color first,Color second) {
  var texture=new Texture2D(64,64,TextureFormat.RGBA32,false);texture.wrapMode=TextureWrapMode.Clamp;texture.filterMode=FilterMode.Bilinear;
  for(int y=0;y<64;y++)for(int x=0;x<64;x++) {
   float dx=Mathf.Max(7-x,x-56),dy=Mathf.Max(7-y,y-56);float distance=Mathf.Sqrt(Mathf.Max(0,dx)*Mathf.Max(0,dx)+Mathf.Max(0,dy)*Mathf.Max(0,dy));
   var color=Color.Lerp(first,second,(x+63-y)/126f);
   if(distance>5f)color=Color.Lerp(color,y>31?Color.white:Color.black,y>31?.18f:.12f);
   color.a=Mathf.Clamp01(7.5f-distance);texture.SetPixel(x,y,color);
  }
  texture.Apply();return Sprite.Create(texture,new Rect(0,0,64,64),new Vector2(0.5f,0.5f));
 }
 int BoardDimension {get{return manager.Mode==GameMode.Expedition?4:(session!=null && session.board!=null?session.board.size:4);}}
 float CellSize {get{return PremiumLayout.CalculateCellSize(BoardDimension,496f);}}
 Vector2 CellPosition(int i) {return PremiumLayout.CalculateCellPosition(i,BoardDimension,CellSize);}
 RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size) {
  var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var rt=go.GetComponent<RectTransform>();rt.anchorMin=rt.anchorMax=new Vector2(0.5f,0.5f);rt.anchoredPosition=position;rt.sizeDelta=size;return rt;
 }
 Image Panel(string name,Transform parent,Vector2 position,Vector2 size,Color color,bool raycast=false) {
  var rt=Rect(name,parent,position,size);var img=rt.gameObject.AddComponent<Image>();img.sprite=name=="Background" || name=="Modal Overlay" || name=="Map Viewport" ? null : name.StartsWith("Mark cell ") || name.StartsWith("Preview ") ? rounded : PremiumTheme.SurfaceSprite();img.type=Image.Type.Sliced;img.color=color;img.raycastTarget=raycast;
  if(size.x>=200 && size.y>=72 && name!="Board" && name!="Background" && name!="Modal Overlay" && name!="Map Viewport") {var depth=rt.gameObject.AddComponent<Shadow>();depth.effectColor=new Color(0,0,0,.22f);depth.effectDistance=new Vector2(0,-4);depth.useGraphicAlpha=true;}
  return img;
 }
 Text Label(string name,Transform parent,string text,Vector2 position,Vector2 size,int fontSize,params object[] arguments) {
  var rt=Rect(name,parent,position,size);var label=rt.gameObject.AddComponent<Text>();label.font=font;label.text=text;label.fontSize=fontSize;label.alignment=TextAnchor.MiddleCenter;label.color=White;label.raycastTarget=false;label.horizontalOverflow=HorizontalWrapMode.Wrap;label.verticalOverflow=VerticalWrapMode.Overflow;rt.gameObject.AddComponent<PremiumText>().Initialize(manager,text,fontSize,arguments);return label;
 }
 Button Button(string name,Transform parent,Vector2 position,Vector2 size,Action action,bool primary=false,bool destructive=false) {
  var img=Panel(name,parent,position,size,primary?Gold:Hex("2B3A3F"),true);var button=img.gameObject.AddComponent<Button>();button.targetGraphic=img;button.navigation=new Navigation{mode=Navigation.Mode.None};
  var colors=button.colors;colors.highlightedColor=Color.white;colors.pressedColor=new Color(.82f,.82f,.82f);colors.disabledColor=Color.white;button.colors=colors;
  var label=Label("Label",img.transform,name,Vector2.zero,size-Vector2.one*12,22);label.color=primary?Ink:White;Emphasis(label);
  img.gameObject.AddComponent<PremiumControl>().Initialize(manager,destructive?ControlTone.Destructive:primary?ControlTone.Primary:ControlTone.Secondary,name);
  img.gameObject.AddComponent<PremiumButtonMotion>();
  button.onClick.AddListener(()=>{Play(clickTone);action();});return button;
 }
 Button IconButton(string name,Transform parent,Vector2 position,Action action,IconKind kind) {
  var button=Button(name,parent,position,new Vector2(72,72),action);
  var txt=button.GetComponentInChildren<Text>();
  if(txt!=null){SetText(txt,"");txt.gameObject.SetActive(false);}
  AddIcon(button.transform,Vector2.zero,28,kind,White);
  return button;
 }
 void AddIcon(Transform parent,Vector2 position,float size,IconKind kind,Color color) {
  var rt=Rect("Icon",parent,position,Vector2.one*size);var icon=rt.gameObject.AddComponent<PremiumIcon>();icon.Kind=kind;icon.color=color;icon.raycastTarget=false;
  if(kind==IconKind.Settings)rt.gameObject.AddComponent<PremiumIconRotation>();
 }
 static Sprite RoundedSprite() {
  var texture=new Texture2D(32,32,TextureFormat.RGBA32,false);texture.wrapMode=TextureWrapMode.Clamp;texture.filterMode=FilterMode.Bilinear;
  for(int y=0;y<32;y++)for(int x=0;x<32;x++) {float dx=Mathf.Max(7-x,x-24),dy=Mathf.Max(7-y,y-24);float distance=Mathf.Sqrt(Mathf.Max(0,dx)*Mathf.Max(0,dx)+Mathf.Max(0,dy)*Mathf.Max(0,dy));texture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(7.5f-distance)));}
  texture.Apply();return Sprite.Create(texture,new Rect(0,0,32,32),new Vector2(0.5f,0.5f),100,0,SpriteMeshType.FullRect,new Vector4(8,8,8,8));
 }
 void BuildShell() {
  // Destroy any external canvases (not under this GameObject)
  foreach(var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None)) {
   if(c.gameObject != gameObject && !c.transform.IsChildOf(transform)) {
    c.gameObject.SetActive(false);
    Destroy(c.gameObject);
   }
  }
  // Also destroy any static scene canvases that are children of THIS transform (e.g. Canvas_Premium)
  for(int i=transform.childCount-1;i>=0;i--) {
   var child=transform.GetChild(i);
   if(child.GetComponent<Canvas>()!=null) {
    child.gameObject.SetActive(false);
    Destroy(child.gameObject);
   }
  }
  if(FindFirstObjectByType<EventSystem>()==null)new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
  var canvas=new GameObject("Premium Canvas",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvas.transform.SetParent(transform,false);
  var cv=canvas.GetComponent<Canvas>();cv.renderMode=RenderMode.ScreenSpaceOverlay;cv.sortingOrder=999;
  scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;
  var bg=Panel("Background",canvas.transform,Vector2.zero,Vector2.zero,Color.white);bg.rectTransform.anchorMin=Vector2.zero;bg.rectTransform.anchorMax=Vector2.one;bg.rectTransform.offsetMin=bg.rectTransform.offsetMax=Vector2.zero;bg.sprite=GradientSprite();bg.type=Image.Type.Simple;
  safeRoot=Rect("Safe Area",canvas.transform,Vector2.zero,Vector2.zero);root=Rect("Content",safeRoot,Vector2.zero,new Vector2(600,960));
  var bannerSlot=Panel("Banner Ad Slot",safeRoot,Vector2.zero,new Vector2(540,PremiumLayout.BannerReserve),new Color(0,0,0,0));
  home=Rect("Home",root,Vector2.zero,new Vector2(600,960)).gameObject;
  game=Rect("Game",root,Vector2.zero,new Vector2(600,960)).gameObject;
  expeditionMap=Rect("ExpeditionMap",root,Vector2.zero,new Vector2(600,960)).gameObject;
  loading=Rect("Loading",root,Vector2.zero,new Vector2(600,960)).gameObject;
  Brand(loading.transform,new Vector2(0,90),1.1f);
  Label("Subtitle",loading.transform,"A moment of focus",new Vector2(0,-12),new Vector2(500,48),22).color=PremiumTheme.Muted;
  Panel("Track",loading.transform,new Vector2(0,-100),new Vector2(380,12),Hex("314247"));loadFill=Panel("Progress",loading.transform,new Vector2(0,-100),new Vector2(380,12),Gold);loadFill.type=Image.Type.Filled;loadFill.fillMethod=Image.FillMethod.Horizontal;
  loadText=Label("Status",loading.transform,"",new Vector2(0,-160),new Vector2(500,70),19);
  modal=Panel("Modal Overlay",safeRoot,Vector2.zero,Vector2.zero,Color.clear,true).gameObject;
  var overlay=modal.GetComponent<RectTransform>();overlay.anchorMin=Vector2.zero;overlay.anchorMax=Vector2.one;overlay.offsetMin=overlay.offsetMax=Vector2.zero;
  var backdrop=Panel("Full Screen Backdrop",modal.transform,Vector2.zero,Vector2.zero,new Color(0.02f,0.03f,0.04f,0.92f),true);
  backdrop.sprite=null;
  backdrop.gameObject.AddComponent<PremiumFullScreenBackdrop>().Fit();
  modalPanel=Panel("Dialog",modal.transform,Vector2.zero,new Vector2(500,740),Hex("223035"),true).rectTransform;
  home.SetActive(false);game.SetActive(false);expeditionMap.SetActive(false);loading.SetActive(false);modal.SetActive(false);FitScreen();
 }
 Sprite GradientSprite() {
  const int width=64,height=128;var texture=new Texture2D(width,height,TextureFormat.RGBA32,false);texture.wrapMode=TextureWrapMode.Clamp;texture.filterMode=FilterMode.Bilinear;
  for(int y=0;y<height;y++)for(int x=0;x<width;x++) {
   float u=(float)x/(width-1),v=(float)y/(height-1);
   var color=Color.Lerp(Hex("0A1220"),PremiumTheme.Background,v);
   float glow=Mathf.Exp(-((u-.28f)*(u-.28f)*5f+(v-.88f)*(v-.88f)*9f));
   color=Color.Lerp(color,Hex("244354"),glow*.5f);texture.SetPixel(x,y,color);
  }
  texture.Apply();return Sprite.Create(texture,new Rect(0,0,width,height),new Vector2(.5f,.5f));
 }
 void BuildHome() {
  // Top bar
  var mute=IconButton("Sound: On",home.transform,new Vector2(-224,420),ToggleSound,sound?IconKind.Sound:IconKind.Mute);soundLabel=mute.GetComponentInChildren<Text>(true);
  IconButton("Settings",home.transform,new Vector2(224,420),ShowSettings,IconKind.Settings);
  // Brand block
  Brand(home.transform,new Vector2(0,360),1.0f);
  Label("Edition",home.transform,"A GAME OF SMALL MOVES",new Vector2(0,305),new Vector2(500,24),13).color=Gold;
  // Hero card (compact 140 h)
  bool hasActiveSession=session!=null && !session.board.Over;
  var heroCard=Panel("Preview Board",home.transform,new Vector2(0,195),new Vector2(500,200),PremiumTheme.Surface);
  int dimension=session!=null?session.board.size:4;
  int[] previewValues=session!=null?session.board.cells:new int[]{2,4,8,16,32,64,128,256,512,1024,2048,0,0,0,0,0};
  float previewStep=156f/dimension;
  var previewBox=Panel("Hero Preview",heroCard.transform,new Vector2(-150,0),new Vector2(156,156),Hex("111E22"));
  for(int i=0;i<dimension*dimension;i++){
   int v=(i<previewValues.Length)?previewValues[i]:0;
   var cell=Panel("Preview "+i,previewBox.transform,new Vector2(-78+previewStep*.5f+(i%dimension)*previewStep,78-previewStep*.5f-(i/dimension)*previewStep),Vector2.one*(previewStep-2),v>0?Color.white:PremiumTheme.Color("253236"));
   if(v>0){cell.sprite=TileSprite(v);cell.type=Image.Type.Simple;var value=Label("Value",cell.transform,"{0}",Vector2.zero,Vector2.one*(previewStep-3),v>=1024?12:17,v);Emphasis(value);value.color=TileText(v);}
  }
  var heroRim=Rect("Hero frame",heroCard.transform,Vector2.zero,new Vector2(498,198)).gameObject.AddComponent<PremiumSelectionFrame>();heroRim.color=PremiumTheme.Border;heroRim.raycastTarget=false;heroRim.rectTransform.anchorMin=Vector2.zero;heroRim.rectTransform.anchorMax=Vector2.one;heroRim.rectTransform.sizeDelta=new Vector2(-2,-2);
  var heroTitle=Label("Hero Title",heroCard.transform,hasActiveSession?"YOUR BOARD":"CLASSIC MODE",new Vector2(66,46),new Vector2(252,30),21);
  Emphasis(heroTitle);heroTitle.color=Gold;heroTitle.alignment=TextAnchor.MiddleLeft;
  var heroSubtitle=Label("Preview caption",heroCard.transform,hasActiveSession?"Resume where you left off":"One swipe at a time",new Vector2(66,6),new Vector2(252,52),17);
  heroSubtitle.color=PremiumTheme.Muted;heroSubtitle.alignment=TextAnchor.MiddleLeft;
  var heroStats=Label("Hero Stats",heroCard.transform,"{0}x{0}  Best {1}",new Vector2(66,-46),new Vector2(252,28),16,hasActiveSession?session.board.size:4,stats.best[hasActiveSession?session.board.size-4:0]);
  heroStats.color=Hex("E5BB7A");heroStats.alignment=TextAnchor.MiddleLeft;
  AddIcon(heroCard.transform,new Vector2(216,-46),20,IconKind.Play,hasActiveSession?Gold:PremiumTheme.Muted);
  var heroBtn=heroCard.gameObject.AddComponent<Button>();heroBtn.targetGraphic=heroCard;
  heroBtn.navigation=new Navigation{mode=Navigation.Mode.None};
  var heroColors=heroBtn.colors;heroColors.highlightedColor=Color.white;heroColors.pressedColor=new Color(.85f,.85f,.85f);heroBtn.colors=heroColors;
  heroCard.gameObject.AddComponent<PremiumButtonMotion>();
  heroBtn.onClick.AddListener(()=>{Play(clickTone);if(hasActiveSession)ShowGame();else ShowLevels();});
  // Primary: PLAY CLASSIC
  var classic=Button("New Game",home.transform,new Vector2(0,70),new Vector2(500,84),ShowLevels,true);
  SetText(classic.GetComponentInChildren<Text>(),"PLAY CLASSIC");Size(classic.GetComponentInChildren<Text>(),24);
  AddIcon(classic.transform,new Vector2(207,0),22,IconKind.Play,Ink);
  // Expedition Journey
  int totalStars=0;if(manager!=null&&manager.Campaign!=null){foreach(var m in manager.Campaign.missions)if(m!=null)totalStars+=m.stars;}
  HomeModeCard("Expedition", "Expedition", "10 missions", "{0}/30 stars",totalStars, -128, ShowExpeditionMap,IconKind.Map,Gold);
  string todayKey=DailyPuzzleService.GetTodayKey();
  bool dailyDone=manager!=null&&manager.Daily!=null&&manager.Daily.IsCompleted(todayKey);
  int streak=manager!=null&&manager.Daily!=null?manager.Daily.currentStreak:0;
  HomeModeCard("Daily Puzzle", "Daily Puzzle",dailyDone?"Completed today":"A fresh challenge", "Streak {0}",streak,128,()=>manager.StartDailyPuzzle(),IconKind.Star,PremiumTheme.Cyan);
  // Resume Classic
  var resume=Button("Resume",home.transform,new Vector2(0,0),new Vector2(500,76),ShowGame);
  var resumeTitle=resume.GetComponentInChildren<Text>();
  resumeTitle.rectTransform.anchoredPosition=new Vector2(14,10);resumeTitle.rectTransform.sizeDelta=new Vector2(310,24);resumeTitle.alignment=TextAnchor.MiddleLeft;Size(resumeTitle,17);
  SetText(resumeTitle,"RESUME GAME");
  resumeDetails=Label("Resume detail",resume.transform,"",new Vector2(14,-14),new Vector2(310,20),13);
  resumeDetails.alignment=TextAnchor.MiddleLeft;resumeDetails.color=PremiumTheme.Muted;
  AddIcon(resume.transform,new Vector2(-210,0),20,IconKind.Play,Gold);
  AddIcon(resume.transform,new Vector2(216,0),20,IconKind.Play,White);
  // Stats bar
  var statsPill=Rect("Stats Summary",home.transform,Vector2.zero,new Vector2(500,56));
  int winRate=stats.played==0?0:Mathf.RoundToInt(100f*stats.won/stats.played);
  string[] captions={"PLAYED","VICTORIES","WIN RATE"};string[] values={stats.played.ToString(),stats.won.ToString(),winRate+"%"};
  for(int i=0;i<3;i++){
   float x=(i-1)*166;
   var value=Label("Stat value "+i,statsPill,values[i],new Vector2(x,12),new Vector2(150,30),22);Emphasis(value);value.color=White;
   Label("Stat caption "+i,statsPill,captions[i],new Vector2(x,-17),new Vector2(150,20),12).color=PremiumTheme.Muted;
   if(i<2)Panel("Stat divider "+i,statsPill,new Vector2(x+83,0),new Vector2(1,36),PremiumTheme.Border);
  }
  // Bottom nav
  Button("How to Play",home.transform,new Vector2(-128,0),new Vector2(236,72),ShowInstructions);
  Button("High Scores",home.transform,new Vector2(128,0),new Vector2(236,72),ShowScores);
  Label("Footer",home.transform,"OFFLINE. ALWAYS YOURS.",new Vector2(0,0),new Vector2(500,22),12).color=PremiumTheme.Muted;
 }
 void HomeModeCard(string name,string title,string detail,string progress,int value,float x,Action action,IconKind icon,Color accent){
  var button=Button(name,home.transform,new Vector2(x,0),new Vector2(244,132),action);
  var label=button.GetComponentInChildren<Text>();SetText(label,title);Size(label,20);
  label.rectTransform.anchoredPosition=new Vector2(0,-4);label.rectTransform.sizeDelta=new Vector2(208,30);label.alignment=TextAnchor.MiddleLeft;
  var emblem=Panel("Mode emblem",button.transform,new Vector2(-84,42),new Vector2(40,40),PremiumTheme.Background);AddIcon(emblem.transform,Vector2.zero,24,icon,accent);
  var tag=Label("Progress",button.transform,progress,new Vector2(30,42),new Vector2(142,24),13,value);tag.color=accent;tag.alignment=TextAnchor.MiddleRight;
  var subtitle=Label("Detail",button.transform,detail,new Vector2(0,-38),new Vector2(208,30),14);subtitle.alignment=TextAnchor.MiddleLeft;subtitle.color=PremiumTheme.Muted;
 }
 void BuildExpeditionMap() {
  if(expeditionMap==null)return;
  for(int i=expeditionMap.transform.childCount-1;i>=0;i--){var child=expeditionMap.transform.GetChild(i);child.gameObject.SetActive(false);ReleaseObject(child.gameObject);}
  IconButton("Back",expeditionMap.transform,new Vector2(-224,420),ShowHome,IconKind.Menu);
  var title=Label("Map Title",expeditionMap.transform,"EXPEDITION",new Vector2(-20,420),new Vector2(240,64),32);title.color=Gold;Emphasis(title);
  var mute=IconButton("Sound: On",expeditionMap.transform,new Vector2(140,420),ToggleSound,sound?IconKind.Sound:IconKind.Mute);
  IconButton("Settings",expeditionMap.transform,new Vector2(224,420),ShowSettings,IconKind.Settings);

  bool hasMissionResume=manager.CanResumeExpedition && !manager.Expedition.completed && !manager.Expedition.failed;
  if(hasMissionResume)Button("Resume expedition",expeditionMap.transform,new Vector2(0,330),new Vector2(500,72),()=>manager.Resume(GameMode.Expedition),true);
  var scrollArea=Panel("Map Viewport",expeditionMap.transform,new Vector2(0,hasMissionResume?-65:-20),new Vector2(500,hasMissionResume?660:760),new Color(0,0,0,0),true).rectTransform;
  scrollArea.gameObject.AddComponent<RectMask2D>();
  var content=Rect("Map Content",scrollArea,Vector2.zero,new Vector2(480,1200));
  content.anchorMin=content.anchorMax=new Vector2(.5f,1);content.pivot=new Vector2(.5f,1);content.anchoredPosition=Vector2.zero;

  var scroll=scrollArea.gameObject.AddComponent<ScrollRect>();
  scroll.viewport=scrollArea;scroll.content=content;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
  scroll.inertia=!manager.ReducedMotion;scroll.scrollSensitivity=25;

  float y=-20f;
  for(int i=0;i<ExpeditionCatalog.Missions.Count;i++){
   int missionIndex=i;
   var def=ExpeditionCatalog.Get(missionIndex);
   var record=manager.Campaign.Get(def.id);
   int stars=record!=null?record.stars:0;
   bool isPrevDone=(i==0)||(manager.Campaign.Get(ExpeditionCatalog.Get(i-1).id)?.completed??false);
   bool unlocked=isPrevDone||stars>0;

   if(i==0 || def.chapter!=ExpeditionCatalog.Get(i-1).chapter){
    string chapName=def.chapter==1?"CHAPTER 1: DISCOVER":def.chapter==2?"CHAPTER 2: EXPLORE":"CHAPTER 3: MASTER";
    var chapHeader=Label("Chapter Header "+def.chapter,content,chapName,new Vector2(0,y-10f),new Vector2(460,30),16);
    chapHeader.color=Gold;chapHeader.alignment=TextAnchor.MiddleLeft;Emphasis(chapHeader);
    y-=45f;
   }

   var card=Panel("Mission Card "+i,content,new Vector2(0,y-45f),new Vector2(460,84),unlocked?Hex("223035"):Hex("192327"));
   var cardBorder=Rect("Border",card.transform,Vector2.zero,new Vector2(456,80)).gameObject.AddComponent<PremiumSelectionFrame>();
   cardBorder.color=unlocked?(stars==3?Gold:Hex("3A4B50")):Hex("253236");cardBorder.raycastTarget=false;

   var numBadge=Panel("Number Badge",card.transform,new Vector2(-180,0),new Vector2(50,50),unlocked?Hex("192327"):Hex("121A1D"));
   var numText=Label("Num",numBadge.transform,(i+1).ToString("D2"),Vector2.zero,new Vector2(50,50),20);
   Emphasis(numText);numText.color=unlocked?Gold:PremiumTheme.Muted;

   var mTitle=Label("Mission Title",card.transform,def.title,new Vector2(10,18),new Vector2(260,26),18);
   Emphasis(mTitle);mTitle.alignment=TextAnchor.MiddleLeft;mTitle.color=unlocked?White:PremiumTheme.Muted;

   string starText=stars==3?"★★★ GOLD":stars==2?"★★☆ SILVER":stars==1?"★☆☆ BRONZE":unlocked?"UNPLAYED":"LOCKED";
   var mStatus=Label("Status",card.transform,starText,new Vector2(10,-14),new Vector2(260,22),14);
   mStatus.alignment=TextAnchor.MiddleLeft;mStatus.color=stars>0?Gold:PremiumTheme.Muted;

   if(unlocked){
    var playBtn=Button("Play "+i,card.transform,new Vector2(175,0),new Vector2(72,72),()=>{manager.StartMission(def);},stars==0);
    var btnTxt=playBtn.GetComponentInChildren<Text>();
    SetText(btnTxt,stars>0?"REPLAY":"PLAY");
    Size(btnTxt,13);
   } else {
    var lockIcon=Panel("Lock",card.transform,new Vector2(175,0),new Vector2(36,36),Color.clear);
    AddIcon(lockIcon.transform,Vector2.zero,24,IconKind.Lock,PremiumTheme.Muted);
   }

   y-=95f;
  }
  foreach(RectTransform row in content){var position=row.anchoredPosition;row.anchorMin=row.anchorMax=new Vector2(.5f,1);row.anchoredPosition=position;}
  content.sizeDelta=new Vector2(480,-y+40f);
  content.anchoredPosition=Vector2.zero;scroll.verticalNormalizedPosition=1f;
 }
 void RefreshExpeditionMap(){BuildExpeditionMap();}
 void BuildGame() {
  IconButton("Menu",game.transform,new Vector2(-230,420),ShowMenu,IconKind.Menu);
  var wordmark=Label("Title",game.transform,"2048",new Vector2(-20,426),new Vector2(220,64),52);wordmark.color=Gold;Emphasis(wordmark);
  var mute=IconButton("Sound: On",game.transform,new Vector2(140,420),ToggleSound,sound?IconKind.Sound:IconKind.Mute);soundLabel=mute.GetComponentInChildren<Text>(true);
  IconButton("Restart",game.transform,new Vector2(230,420),ConfirmRestart,IconKind.Restart);
  score=ScoreCard("SCORE",new Vector2(-128,330),Hex("223035"),Hex("3A4B50"));best=ScoreCard("BEST",new Vector2(128,330),Hex("223035"),Hex("3A4B50"));addition=Label("Score addition",score.transform.parent,"",new Vector2(0,25),new Vector2(220,60),28);
  var levelPill=Panel("Level Pill",game.transform,new Vector2(0,252),new Vector2(496,44),PremiumTheme.Surface);

  level=Label("Level",levelPill.transform,"",Vector2.zero,new Vector2(480,36),17);level.color=Gold;Emphasis(level);
  grid=Panel("Board",game.transform,new Vector2(0,-25),new Vector2(520,520),PremiumTheme.Background).rectTransform;
  var boardRim=Rect("Board Border",grid,Vector2.zero,new Vector2(518,518)).gameObject.AddComponent<PremiumSelectionFrame>();boardRim.color=PremiumTheme.Border;boardRim.raycastTarget=false;

  // Action Bar — premium redesign: centered icon, tool name, count badge, status
  var actionDock=Panel("Action Bar",game.transform,new Vector2(0,-328),new Vector2(520,112),PremiumTheme.Background);
  string[] toolNames={"UNDO","SWAP","DELETE"};
  for(int i=0;i<3;i++) {
   int action=i;powers[i]=Button(((Power)i).ToString(),actionDock.transform,new Vector2(-168+i*168,0),new Vector2(160,104),()=>UsePower((Power)action));
   var defLabel=powers[i].GetComponentInChildren<Text>();if(defLabel!=null){SetText(defLabel,"");defLabel.gameObject.SetActive(false);}
   AddIcon(powers[i].transform,new Vector2(0,23),34,(IconKind)((int)IconKind.Undo+i),White);
   var badge=Panel("Count Badge",powers[i].transform,new Vector2(58,30),new Vector2(28,28),Gold);
   badge.rectTransform.anchorMin=badge.rectTransform.anchorMax=new Vector2(0.5f,0.5f);
   var badgeNum=Label("Count",badge.rectTransform,"0",Vector2.zero,new Vector2(26,26),14);
   Emphasis(badgeNum);badgeNum.color=Hex("192327");badgeNum.alignment=TextAnchor.MiddleCenter;
   powerLabels[i]=badgeNum;
   var toolName=Label("ToolName",powers[i].transform,toolNames[i],new Vector2(0,-10),new Vector2(150,24),16);
   Emphasis(toolName);toolName.alignment=TextAnchor.MiddleCenter;toolName.color=White;
   powerStatuses[i]=Label("Availability",powers[i].transform,"LOCKED",new Vector2(0,-34),new Vector2(150,18),11);
   powerStatuses[i].alignment=TextAnchor.MiddleCenter;powerStatuses[i].color=PremiumTheme.Muted;
  }
  hint=Label("Hint",game.transform,"",new Vector2(0,-388),new Vector2(500,32),16);hint.color=PremiumTheme.Muted;
  cancelSelection=Button("Cancel selection",game.transform,new Vector2(0,-432),new Vector2(230,72),()=>{CancelSelection();Refresh();}).gameObject;Size(cancelSelection.GetComponentInChildren<Text>(),15);cancelSelection.SetActive(false);
 }
 Text ScoreCard(string title,Vector2 position,Color cardBg,Color borderColor) {
  var card=Panel(title,game.transform,position,new Vector2(240,100),cardBg);
  var rim=Rect("Card frame",card.transform,Vector2.zero,new Vector2(238,98)).gameObject.AddComponent<PremiumSelectionFrame>();rim.color=borderColor;rim.raycastTarget=false;
  var caption=Label("Caption",card.transform,title,new Vector2(0,28),new Vector2(204,22),14);caption.color=PremiumTheme.Muted;caption.alignment=TextAnchor.MiddleLeft;Emphasis(caption);
  AddIcon(card.transform,new Vector2(90,28),22,title=="BEST"?IconKind.Trophy:IconKind.Star,title=="BEST"?Gold:PremiumTheme.Cyan);
  var number=Label("Number",card.transform,"0",new Vector2(0,-14),new Vector2(204,52),44);Emphasis(number);number.alignment=TextAnchor.MiddleLeft;number.resizeTextForBestFit=true;number.resizeTextMinSize=18;number.resizeTextMaxSize=44;return number;
 }

 RectTransform CreateTile(Transform parent,int index,int value) {
  var image=Panel("Tile "+index,parent,CellPosition(index),Vector2.one*CellSize,Color.white);image.sprite=TileSprite(value);image.type=Image.Type.Simple;var number=Label("Value",image.transform,value.ToString(),Vector2.zero,Vector2.one*(CellSize-8),BoardDimension==4?39:BoardDimension==5?31:26);Emphasis(number);number.resizeTextForBestFit=true;number.resizeTextMinSize=16;number.resizeTextMaxSize=number.fontSize;number.color=TileText(value);return image.rectTransform;
 }
 void BuildBoard() {
  mergeFeedback?.Stop();
  if(manager.ActiveSession==null)return;
  int expected=manager.Mode==GameMode.Expedition?16:session.board.cells.Length;
  bool expeditionBoard=manager.Mode==GameMode.Expedition;
  if(tiles!=null && tiles.Length==expected && boardWasExpedition==expeditionBoard)return;
  boardWasExpedition=expeditionBoard;
  for(int i=grid.childCount-1;i>=0;i--){var child=grid.GetChild(i);if(child.name=="Board Border" || child.name=="Merge Feedback" || ghostPool.Contains(child as RectTransform))continue;child.gameObject.SetActive(false);ReleaseObject(child.gameObject);}
  int count=manager.Mode==GameMode.Expedition?16:session.board.cells.Length;tiles=new Image[count];numbers=new Text[count];
  for(int i=0;i<count;i++)Panel("Empty "+i,grid,CellPosition(i),Vector2.one*CellSize,TileColor(0));
  int[] currentValues=manager.Mode==GameMode.Expedition?manager.Expedition.tiles:session.board.cells;
  for(int i=0;i<count;i++) {var tile=CreateTile(grid,i,(i<currentValues.Length)?currentValues[i]:0);tiles[i]=tile.GetComponent<Image>();numbers[i]=tile.GetComponentInChildren<Text>();if(manager.Mode==GameMode.Expedition){AddIcon(tile,new Vector2(0,20),36,IconKind.Lock,Gold);tile.Find("Icon").name="Terrain lock";}}
  ghostPool.RemoveAll(tile=>tile==null);
  for(int i=ghostPool.Count;i<count;i++){var ghost=CreateTile(grid,0,2);ghost.gameObject.SetActive(false);ghostPool.Add(ghost);}
 }
 bool CompactDialog => navigation.Dialog==GameDialog.Restart || navigation.Dialog==GameDialog.ClearScores || navigation.Dialog==GameDialog.ReplaceBoard || navigation.Dialog==GameDialog.Continue || navigation.Dialog==GameDialog.VideoUnavailable;
 void Modal(string title,string description) {
  modal.transform.SetAsLastSibling();modal.SetActive(true);
  var oldCelebration=modalPanel.GetComponent<PremiumCelebration>();if(oldCelebration!=null)ReleaseObject(oldCelebration);
  var motion=modalPanel.GetComponent<PremiumPanelMotion>();if(motion==null)motion=modalPanel.gameObject.AddComponent<PremiumPanelMotion>();motion.Reveal();
  for(int i=modalPanel.childCount-1;i>=0;i--){var child=modalPanel.GetChild(i);if(child.name=="Modal Border")continue;child.gameObject.SetActive(false);ReleaseObject(child.gameObject);}
  modalPanel.sizeDelta=new Vector2(500,CompactDialog?430:740);
  var heading=Label("Title",modalPanel,title,new Vector2(0,CompactDialog?136:296),new Vector2(456,48),30);Emphasis(heading);heading.color=Gold;
  Label("Description",modalPanel,description,new Vector2(0,CompactDialog?68:240),new Vector2(456,44),17).color=PremiumTheme.Muted;
 }
 void DialogButton(string title,float y,Action action,bool primary=false,bool destructive=false){Button(title,modalPanel,new Vector2(0,CompactDialog?y-60:y),new Vector2(456,72),action,primary,destructive);}
 string T(string english,params object[] arguments){return GameLocalization.Translate(manager.Language,english,arguments);}
 void SetText(Text text,string english,params object[] arguments){if(text!=null)text.GetComponent<PremiumText>().Set(english,arguments);}
 void Size(Text text,int size){text.GetComponent<PremiumText>().Size(size);}
 void Emphasis(Text text){text.GetComponent<PremiumText>().Emphasis();}
 void ApplyPresentation() {
  foreach(var text in GetComponentsInChildren<PremiumText>(true))text.Apply();
  foreach(var control in GetComponentsInChildren<PremiumControl>(true))control.Apply();
  RebuildHome();
  if(navigation.Dialog!=GameDialog.None)RenderDialog();
  if(manager.ActiveSession!=null && tiles!=null)Refresh();
 }
 void RebuildHome(){for(int i=home.transform.childCount-1;i>=0;i--){var child=home.transform.GetChild(i);child.gameObject.SetActive(false);child.SetParent(null,false);ReleaseObject(child.gameObject);}BuildHome();RefreshHome();}
 void RefreshHome(){var resume=home.transform.Find("Resume");if(resume!=null)resume.gameObject.SetActive(session!=null && !session.board.Over);if(session!=null && resumeDetails!=null)SetText(resumeDetails,"{0} × {0}  ·  Score {1}",session.board.size,session.board.score);FitScreen(true);}
 void Brand(Transform parent,Vector2 position,float scale) {
  var group=Rect("Brand",parent,position,new Vector2(340,86));group.localScale=Vector3.one*scale;
  var mark=Rect("Tile monogram",group,new Vector2(-124,0),new Vector2(70,70));
  int[] digits={2,0,4,8};
  for(int i=0;i<4;i++){var cell=Panel("Mark cell "+i,mark,new Vector2(i%2==0?-18:18,i<2?18:-18),new Vector2(32,32),i==3?Gold:PremiumTheme.SurfaceElevated);var digit=Label("Digit",cell.transform,digits[i].ToString(),Vector2.zero,new Vector2(32,32),20);Emphasis(digit);digit.color=i==3?Ink:White;}
  var wordmark=Label("Wordmark",group,"2048",new Vector2(35,2),new Vector2(245,86),66);wordmark.GetComponent<PremiumText>().Scales=false;Emphasis(wordmark);wordmark.color=Gold;
 }
 void LevelCard(int size,float y,string title,string description) {
  int levelIndex=size-3;
  var button=Button("Level "+levelIndex,modalPanel,new Vector2(0,y),new Vector2(456,126),()=>StartLevel(size));SetText(button.GetComponentInChildren<Text>(),"");
  bool current=session!=null && session.board.size==size && !session.board.Over;
  if(current){var border=Rect("Current board frame",button.transform,Vector2.zero,new Vector2(450,120)).gameObject.AddComponent<PremiumSelectionFrame>();border.color=Gold;border.raycastTarget=false;}
  var preview=Panel("Difficulty preview",button.transform,new Vector2(-158,0),new Vector2(96,96),Hex("192327"));
  float cell=84f/size;int targetVal=size==4?2048:size==5?4096:8192;
  for(int i=0;i<size*size;i++){
   int row=i/size,col=i%size;Vector2 pos=new Vector2(-42f+cell*0.5f+col*cell,42f-cell*0.5f-row*cell);
   bool isAccent=(row==size-1 && col==size-1);bool isSub=(row==0 && col==0);
   var c=Panel("Cell "+i,preview.transform,pos,Vector2.one*(cell-2.5f),isAccent?Gold:isSub?Hex("E5BB7A"):Hex("314247"));
   if(isAccent){c.sprite=TileSprite(targetVal);c.type=Image.Type.Simple;}
  }
  var levelTag=Label("Level tag",button.transform,"LEVEL "+levelIndex,new Vector2(38,34),new Vector2(280,20),13);levelTag.color=Gold;levelTag.alignment=TextAnchor.MiddleLeft;Emphasis(levelTag);
  var heading=Label("Difficulty",button.transform,title.ToUpper()+"  "+size+"×"+size,new Vector2(38,11),new Vector2(280,26),21);Emphasis(heading);heading.alignment=TextAnchor.MiddleLeft;heading.color=current?Gold:White;
  var detail=Label("Goal",button.transform,"Target {0}  ·  Best {1}",new Vector2(38,-13),new Vector2(280,22),14,targetVal,stats.best[size-4]);detail.alignment=TextAnchor.MiddleLeft;detail.color=Hex("E5BB7A");
  var badge=Label("Difficulty note",button.transform,current?"CURRENT ACTIVE BOARD":description,new Vector2(38,-34),new Vector2(280,20),12);badge.alignment=TextAnchor.MiddleLeft;badge.color=current?Gold:PremiumTheme.Muted;
  AddIcon(button.transform,new Vector2(196,0),24,IconKind.Play,current?Gold:Hex("B1BEBA"));
 }
 void SettingRow(string title,float y,bool enabled,Action action) {
  var button=Button(title,modalPanel,new Vector2(0,y),new Vector2(456,72),action);var label=button.GetComponentInChildren<Text>();label.rectTransform.anchoredPosition=new Vector2(-45,0);label.rectTransform.sizeDelta=new Vector2(320,50);label.alignment=TextAnchor.MiddleLeft;Size(label,20);
  var status=Label("Setting state",button.transform,enabled?"ON":"OFF",new Vector2(142,0),new Vector2(75,45),17);status.color=enabled?Gold:PremiumTheme.Muted;
  var track=Panel("Toggle",button.transform,new Vector2(192,0),new Vector2(40,24),enabled?Gold:PremiumTheme.Border);var knob=Panel("Knob",track.transform,new Vector2(enabled?8:-8,0),new Vector2(18,18),enabled?Ink:PremiumTheme.TextSecondary);knob.sprite=PremiumTheme.CreateRoundedCard(32,16,Color.white,Color.clear,0);
 }
 void ResultCard(string caption,long value){
  var card=Panel("Result summary",modalPanel,new Vector2(0,105),new Vector2(456,174),Hex("223035"));
  var border=Rect("Result Border",card.transform,Vector2.zero,new Vector2(452,170)).gameObject.AddComponent<PremiumSelectionFrame>();border.color=Hex("3A4B50");border.raycastTarget=false;
  bool expedition=manager.Mode==GameMode.Expedition;
  int maxCell=0;if(manager.ActiveSession!=null && manager.ActiveSession.Cells!=null){foreach(int c in manager.ActiveSession.Cells)if(c>maxCell)maxCell=c;}
  int showTile = maxCell>0 ? maxCell : 2048;
  if(showTile<2)showTile=2;
  var tilePreview = Panel("Result Tile",card.transform,new Vector2(-155,0),new Vector2(88,88),Color.white);
  tilePreview.sprite=TileSprite(showTile);tilePreview.type=Image.Type.Simple;
  var tileNumber=Label("Tile Value",tilePreview.transform,showTile.ToString(),Vector2.zero,new Vector2(84,84),showTile>=1024?22:30);
  Emphasis(tileNumber);tileNumber.color=TileText(showTile);
  var title=Label("Caption",card.transform,caption,new Vector2(40,36),new Vector2(260,26),14);title.color=Gold;title.alignment=TextAnchor.MiddleLeft;Emphasis(title);
  var number=Label("Result value",card.transform,value.ToString("N0"),new Vector2(40,2),new Vector2(260,54),34);Emphasis(number);number.color=White;number.alignment=TextAnchor.MiddleLeft;
  long bestScore = session!=null && session.board!=null ? stats.best[session.board.size-4] : 0;
  bool isNewBest = value >= bestScore && value > 0;
  var subNote=Label("Best note",card.transform,expedition?("ACTION LIMIT: "+manager.Expedition.mission.actionLimit):isNewBest?"PERSONAL BEST":("BEST: "+bestScore.ToString("N0")),new Vector2(40,-34),new Vector2(260,24),14);
  subNote.color=isNewBest?Gold:PremiumTheme.Muted;subNote.alignment=TextAnchor.MiddleLeft;Emphasis(subNote);
  var run=Label("Run details",card.transform,"{0} moves  ·  Best tile {1}",new Vector2(40,-63),new Vector2(260,26),15,expedition?manager.Expedition.actions:session!=null?session.moves:0,maxCell);run.alignment=TextAnchor.MiddleLeft;run.color=PremiumTheme.Muted;
 }
 void MarkSelection(int index) {
  if(selectionFrame!=null){selectionFrame.SetActive(false);ReleaseObject(selectionFrame);}
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
 void CloseModal(){navigation.Back();}
#if UNITY_EDITOR
 public void BuildEditorPreview(GameDialog dialog, bool gameplay, bool savedHome=false) {
  manager=gameObject.AddComponent<GameManager>();controller=gameObject.AddComponent<GameController>();
  rewardProvider=gameObject.AddComponent<AdMobProvider>();
  manager.Initialize(savedHome?"Premium2048":"Premium2048.EditorPreview",new PreviewStore(savedHome?new NativeSaveStore():null));
  if(!savedHome){manager.StartGame(4);
  var b=session.board;b.cells=new[]{2,4,8,16,4,8,16,32,8,16,32,64,0,2,64,128};b.score=2684;session.moves=24;
  session.history.Add(b.Copy());stats.best=new long[]{12480,18640,28672};stats.played=18;stats.won=3;
  if(dialog==GameDialog.Win){b.cells[15]=2048;b.won=true;}
  if(dialog==GameDialog.Loss || dialog==GameDialog.Continue){for(int i=0;i<16;i++)b.cells[i]=(i%4+i/4)%2==0?2:4;}
  }
  font=Resources.Load<Font>("Fonts/NotoSans-Regular");rounded=RoundedSprite();
  BuildShell();BuildHome();BuildGame();BuildExpeditionMap();
  manager.Changed+=OnGameChanged;navigation.Changed+=OnNavigationChanged;ready=true;
  if(gameplay || dialog==GameDialog.Pause || dialog==GameDialog.Restart || dialog==GameDialog.Win || dialog==GameDialog.Loss || dialog==GameDialog.Continue){manager.Resume();}else ShowHome();
  if(dialog!=GameDialog.None)navigation.Open(dialog);
  FitScreen(true);Canvas.ForceUpdateCanvases();
  var canvas=GetComponentInChildren<Canvas>();
  scaler.enabled=false;canvas.renderMode=RenderMode.ScreenSpaceOverlay;scaler.scaleFactor=2;
  safeRoot.anchorMin=Vector2.zero;safeRoot.anchorMax=Vector2.one;safeRoot.offsetMin=safeRoot.offsetMax=Vector2.zero;
  root.anchorMin=root.anchorMax=new Vector2(.5f,.5f);root.sizeDelta=new Vector2(540,1100);
  home.GetComponent<RectTransform>().sizeDelta=game.GetComponent<RectTransform>().sizeDelta=new Vector2(540,1100);
  RepositionResponsiveElements(1100);
  root.localScale=Vector3.one*2;modalPanel.localScale=Vector3.one*2;
  var group=modalPanel.GetComponent<CanvasGroup>();if(group!=null)group.alpha=1;
  foreach(var groupItem in GetComponentsInChildren<CanvasGroup>(true)){groupItem.alpha=1;groupItem.transform.localScale=Vector3.one;}
  root.localScale=Vector3.one*2;modalPanel.localScale=Vector3.one*2;
  foreach(var motion in GetComponentsInChildren<PremiumHomeMotion>(true)){motion.StopAndRestore();DestroyImmediate(motion);}
  RepositionResponsiveElements(1100);
  foreach(var motion in GetComponentsInChildren<PremiumPanelMotion>(true))DestroyImmediate(motion);
  foreach(var celebration in GetComponentsInChildren<PremiumCelebration>(true))DestroyImmediate(celebration);
 }
 sealed class PreviewStore : IPreferenceStore {
  readonly Dictionary<string,string> values=new Dictionary<string,string>();
  readonly IPreferenceStore source;
  public PreviewStore(IPreferenceStore source=null){this.source=source;}
  public string Read(string key,string fallback=""){return values.TryGetValue(key,out var value)?value:source!=null?source.Read(key,fallback):fallback;}
  public int ReadInt(string key,int fallback){return int.TryParse(Read(key),out var value)?value:source!=null?source.ReadInt(key,fallback):fallback;}
  public void Write(string key,string value){values[key]=value;}public void Delete(string key){values.Remove(key);}public void Flush(){}
 }
#endif

 static void ReleaseObject(UnityEngine.Object item) {
#if UNITY_EDITOR
  if(!Application.isPlaying){DestroyImmediate(item);return;}
#endif
  Destroy(item);
 }
 void OnDestroy() {
  if(instance!=this)return;
  instance=null;
  if(manager!=null){manager.Changed-=OnGameChanged;navigation.Changed-=OnNavigationChanged;}
  if(moveTone!=null)ReleaseObject(moveTone);
  if(mergeTone!=null)ReleaseObject(mergeTone);
  if(clickTone!=null)ReleaseObject(clickTone);
  if(winTone!=null)ReleaseObject(winTone);
  if(slideTone!=null && slideTone!=moveTone)ReleaseObject(slideTone);
  if(undoTone!=null)ReleaseObject(undoTone);
  if(swapTone!=null)ReleaseObject(swapTone);
  if(deleteTone!=null)ReleaseObject(deleteTone);
  if(lossTone!=null)ReleaseObject(lossTone);
  if(spawnTone!=null)ReleaseObject(spawnTone);
  foreach(var kv in mergeClips)if(kv.Value!=null && kv.Value!=mergeTone)ReleaseObject(kv.Value);
  mergeClips.Clear();
  foreach(var kv in tileSprites)if(kv.Value!=null){if(kv.Value.texture!=null)ReleaseObject(kv.Value.texture);ReleaseObject(kv.Value);}
  tileSprites.Clear();
  if(rounded!=null){if(rounded.texture!=null)ReleaseObject(rounded.texture);ReleaseObject(rounded);}
 }
}
}
