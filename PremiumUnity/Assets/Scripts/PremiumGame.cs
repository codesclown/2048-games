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
 Font font;Sprite rounded;CanvasScaler scaler;
 RectTransform safeRoot, root, grid, modalPanel;
 GameObject home, game, loading, modal, cancelSelection;
 Text score,best,level,hint,addition,soundLabel,loadText;Image loadFill;
 Image[] tiles;Text[] numbers;Button[] powers=new Button[3];Text[] powerLabels=new Text[3];
 readonly Dictionary<int,Sprite> tileSprites=new Dictionary<int,Sprite>();
 readonly List<GameObject> ghosts=new List<GameObject>();
 bool ready,animating,appPaused,focusLost;
 int selected=-1;
 Coroutine moveAnimation,scoreAnimation;AudioSource audioSource;AudioClip moveTone,mergeTone,clickTone,winTone;
 int lastWidth,lastHeight;Rect lastSafe;
 Color Gold { get {return Hex("EAC780");} } Color Ink { get {return Hex("0F0F23");} } Color White { get {return Hex("E6E6FA");} }
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
  Application.targetFrameRate=60;Application.runInBackground=false;Screen.sleepTimeout=SleepTimeout.SystemSetting;font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
  // Keep the desktop QA harness advancing when another window has focus.
  if(Array.IndexOf(Environment.GetCommandLineArgs(),"-premiumSmoke")>=0)Application.runInBackground=true;
#endif
  rounded=RoundedSprite();BuildShell();
  if(rewardProvider==null) rewardProvider=GetComponent<RewardProvider>();
  StartCoroutine(Initialize());
 }
 IEnumerator Initialize() {
  Progress(0.15f,"Preparing premium interface");yield return null;
  Progress(0.4f,"Loading scores and saved game");yield return null;
  BuildHome();BuildGame();Progress(0.75f,"Preparing controls and audio");yield return null;
  audioSource=gameObject.AddComponent<AudioSource>();audioSource.playOnAwake=false;
  moveTone=Tone("Slide",220,0.07f);mergeTone=Tone("Merge",440,0.13f);clickTone=Tone("Click",660,0.06f);winTone=Tone("Win",880,0.35f);
  Progress(1,"Ready");yield return null;
  ready=true;ShowHome();FitScreen();
 }
 void Progress(float value,string text) {loadFill.fillAmount=value;loadText.text=text+"\n"+Mathf.RoundToInt(value*100)+"%";}
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
  if(navigation.Screen==GameScreen.Home){var resume=home.transform.Find("Resume");if(resume!=null)resume.gameObject.SetActive(session!=null && !session.board.Over);}
  modal.SetActive(navigation.Dialog!=GameDialog.None);
  if(navigation.Dialog!=GameDialog.None)RenderDialog();
  if(navigation.Screen==GameScreen.Game && session!=null){if(tiles==null || tiles.Length!=session.board.cells.Length)BuildBoard();Refresh();}
 }
 IEnumerator AnimateMove(long gain) {
  animating=true;var motions=session.board.motions;var moving=new List<RectTransform>();
  for(int i=0;i<tiles.Length;i++)tiles[i].gameObject.SetActive(false);
  foreach(var motion in motions){var tile=CreateTile(grid,motion.from,motion.value);moving.Add(tile);ghosts.Add(tile.gameObject);}
  float elapsed=0;
  while(elapsed<0.12f) {
   elapsed+=Time.unscaledDeltaTime;float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/0.12f));
   for(int i=0;i<moving.Count;i++)moving[i].anchoredPosition=Vector2.Lerp(CellPosition(motions[i].from),CellPosition(motions[i].to),t);
   yield return null;
  }
  ClearGhosts();Refresh(false);ScoreGain(gain);elapsed=0;
  while(elapsed<0.18f) {
   elapsed+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(elapsed/0.18f);
   foreach(int index in session.board.merges)tiles[index].transform.localScale=Vector3.one*(1+0.17f*Mathf.Sin(t*Mathf.PI));
   if(session.board.spawned>=0)tiles[session.board.spawned].transform.localScale=Vector3.one*Mathf.SmoothStep(0,1,t);
   yield return null;
  }
  animating=false;moveAnimation=null;manager.ResolveAnimation();Refresh();
 }
 void ClearGhosts(){foreach(var ghost in ghosts)Destroy(ghost);ghosts.Clear();}
 void FinishAnimation() {
  if(moveAnimation!=null)StopCoroutine(moveAnimation);moveAnimation=null;animating=false;controller.Cancel();ClearGhosts();
  if(tiles!=null)foreach(var tile in tiles){tile.gameObject.SetActive(true);tile.transform.localScale=Vector3.one;}
 }
 void NewGame(int dimension){if(rewardPending || navigation.Suspended)return;FinishAnimation();manager.StartGame(dimension);}
 void ShowGame(){manager.Resume();}
 void ShowHome(){manager.Home();}
 void OnApplicationPause(bool paused){appPaused=paused;if(manager!=null){controller.Cancel();manager.SetBackground(appPaused || focusLost);}}
 void OnApplicationFocus(bool focused){focusLost=!focused;if(manager!=null){controller.Cancel();manager.SetBackground(appPaused || focusLost);}}
 void OnApplicationQuit(){if(manager!=null)manager.Save();}
 void Refresh(bool results=true) {
  if(session==null || tiles==null)return;Board b=session.board;score.text=b.score.ToString();best.text=stats.best[b.size-4].ToString();
  level.text="LEVEL "+(b.size-3)+"  /  "+b.size+" × "+b.size+"  /  TARGET "+b.target;
  for(int i=0;i<tiles.Length;i++){int v=b.cells[i];tiles[i].gameObject.SetActive(v!=0);tiles[i].sprite=TileSprite(v);tiles[i].color=i==selected?Gold:Color.white;tiles[i].transform.localScale=Vector3.one;numbers[i].text=v.ToString();numbers[i].color=TileText(v);}
  for(int i=0;i<3;i++){powerLabels[i].text=((Power)i).ToString().ToUpper()+"  "+session.stock[i].available;powers[i].interactable=navigation.State==GameState.Playing && !navigation.Suspended && session.CanUse((Power)i);}
  cancelSelection.SetActive(selection>=0);
  if(selection<0)hint.text=session.Unlocked?"Earn more: 128 → Undo   256 → Swap   512 → Delete":"Swipe to merge · Tools unlock in "+Math.Max(0,2-session.moves)+" moves";
  else hint.text=selection==(int)Power.Swap?"Tap a tile, then a different adjacent tile. Cancel to exit.":"Tap the tile you want to delete. Cancel to exit.";
  soundLabel.text="";soundLabel.transform.parent.GetComponentInChildren<PremiumIcon>().Kind=sound?IconKind.Sound:IconKind.Mute;
  if(results && navigation.State!=GameState.Animating)manager.ShowResult();
 }
 void Result(){manager.ShowResult();}
 void ConfirmRestart(){if(session!=null && !rewardPending)navigation.Open(GameDialog.Restart);}
 void ShowMenu(){if(session!=null && !rewardPending)navigation.Open(GameDialog.Pause);}
 void StartLevel(int dimension){if(!navigation.Suspended && !rewardPending && navigation.State!=GameState.Loading)StartCoroutine(LoadLevel(dimension));}
 IEnumerator LoadLevel(int dimension) {
  FinishAnimation();navigation.Loading();Progress(0.25f,"Preparing "+dimension+" × "+dimension+" level");yield return null;
  // A backgrounded loading screen must not discard the saved game.
  while(navigation.Suspended)yield return null;
  manager.StartGame(dimension);Progress(1,"Level ready");
 }
 void ShowLevels(){navigation.Open(GameDialog.Levels);}
 void ShowInstructions(){navigation.Open(GameDialog.Instructions);}
 void ShowScores(){navigation.Open(GameDialog.Scores);}
 void RenderDialog() {
  switch(navigation.Dialog) {
   case GameDialog.Pause:
    Modal("Paused","Your game is saved. Choose where to go next.");
    DialogButton("Resume",100,ShowGame);DialogButton("Change level",30,ShowLevels);DialogButton("How to Play",-40,ShowInstructions);DialogButton("High Scores",-110,ShowScores);DialogButton("Home",-180,ShowHome);break;
   case GameDialog.Levels:
    Modal("Choose your level","Select a board size. Starting a level resets the current game.");
    DialogButton("Level 1 · 4×4 · Target 2048",80,()=>StartLevel(4));DialogButton("Level 2 · 5×5 · Target 4096",0,()=>StartLevel(5));DialogButton("Level 3 · 6×6 · Target 8192",-80,()=>StartLevel(6));break;
   case GameDialog.Instructions:
    Modal("How to Play","");
    var body=Label("Rules",modalPanel,"<b>MOVE & MERGE</b>\nSwipe up, down, left or right on the board. Matching tiles merge once per move. Reach 2048 / 4096 / 8192 for your level.\n\n<b>POWER-UPS</b>\nUnlock after 2 valid moves. Undo reverses the last action. Swap two different adjacent occupied tiles. Delete removes a selected tile. Tap Cancel to exit selection.\n\n<b>EARN & CONTINUE</b>\nAfter use, holding 128 / 256 / 512 earns Undo / Swap / Delete on valid moves (1s cooldown). Each type has a lifetime cap of 4 / 5 / 6 per level. One continuation rewinds up to 5 actions while keeping your score.",new Vector2(0,-5),new Vector2(430,425),18);body.alignment=TextAnchor.UpperLeft;break;
   case GameDialog.Scores:
    Modal("High Scores","Saved locally on this device");
    Label("Stats",modalPanel,"<b>CLASSIC 4×4</b>     "+stats.best[0]+"\n\n<b>ADVANCED 5×5</b>     "+stats.best[1]+"\n\n<b>EXPERT 6×6</b>     "+stats.best[2]+"\n\nGames played: "+stats.played+"\nGames won: "+stats.won+"\nWin rate: "+(stats.played==0?0:Mathf.RoundToInt(100f*stats.won/stats.played))+"%",new Vector2(0,10),new Vector2(420,280),25);
    DialogButton("Clear scores",-180,()=>navigation.Open(GameDialog.ClearScores));break;
   case GameDialog.ClearScores:
    Modal("Clear saved scores?","This clears scores, statistics and the saved game.");DialogButton("Clear",20,manager.ResetProgress);DialogButton("Cancel",-65,CloseModal);break;
   case GameDialog.Restart:
    Modal("Restart level?","Start a fresh board and reset power-ups. Your best score stays saved.");DialogButton("Restart",20,()=>NewGame(session.board.size));DialogButton("Cancel",-65,CloseModal);break;
   case GameDialog.Win:
    Play(winTone);Modal("You win!","You reached "+session.board.target+". Keep building your highest tile.");
    DialogButton("Keep going",70,()=>manager.KeepGoing());DialogButton("Play again",-5,()=>NewGame(session.board.size));DialogButton("Home",-80,ShowHome);break;
   case GameDialog.Loss:
    Modal("Game over","Score "+session.board.score+"\nNo moves remain on the board.");
    if(session.CanReward)DialogButton(rewardProvider==null?"Continue: rewind 5 moves":"Watch video to continue",70,AskReward);
    DialogButton("Play again",-5,()=>NewGame(session.board.size));DialogButton("Home",-80,ShowHome);break;
   case GameDialog.Continue:
    Modal("Continue playing?","Rewind up to 5 previous actions and keep your current score. Available once per game.");DialogButton("Continue",20,GrantOfflineReward);DialogButton("No thanks",-65,CloseModal);break;
   case GameDialog.VideoUnavailable:
    Modal("Video unavailable","The ad provider is not ready. Your board remains saved.");DialogButton("Back",0,CloseModal);break;
   case GameDialog.RewardVideo:
    Modal("Reward video","Waiting for the ad provider. Reward is granted only after completion.");break;
  }
 }
 void ToggleSound(){manager.ToggleSound();if(sound)Play(clickTone);}
 void UsePower(Power power){if(!Playing)return;if(power==Power.Undo){if(manager.Undo())Play(clickTone);}else manager.BeginSelection(power);}
 void ClearSelectionVisuals(){selected=-1;if(cancelSelection!=null)cancelSelection.SetActive(false);if(tiles!=null)foreach(var tile in tiles)tile.color=Color.white;}
 void CancelSelection(){manager.CancelSelection();ClearSelectionVisuals();}
 void SelectCell(Vector2 screen) {
  if(!Playing || selection<0)return;
  Vector2 local;if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(grid,screen,null,out local))return;
  int index=-1;for(int i=0;i<tiles.Length;i++){Vector2 d=local-CellPosition(i);if(Mathf.Abs(d.x)<=CellSize/2 && Mathf.Abs(d.y)<=CellSize/2){index=i;break;}}
  if(index<0 || session.board.cells[index]==0)return;
  if(selection==(int)Power.Delete){if(manager.Delete(index))Play(clickTone);return;}
  if(selected<0){selected=index;tiles[index].color=Gold;hint.text="Now tap a different adjacent tile.";return;}
  if(index==selected){CancelSelection();Refresh();return;}
  if(manager.Swap(selected,index))Play(clickTone);else hint.text="Select a different tile directly next to the gold tile.";
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
 void GrantOfflineReward(){if(manager.ContinueOffline())Play(clickTone);}
 void StopScore() {if(scoreAnimation!=null)StopCoroutine(scoreAnimation);scoreAnimation=null;addition.text="";}
 void ScoreGain(long gain) {if(gain<=0)return;StopScore();scoreAnimation=StartCoroutine(FloatScore(gain));}
 IEnumerator FloatScore(long gain) {
  addition.text="+"+gain;float elapsed=0;
  while(elapsed<0.65f) {elapsed+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(elapsed/0.65f);addition.rectTransform.anchoredPosition=new Vector2(0,25+40*t);addition.color=new Color(1,0.84f,0,1-t);yield return null;}
  addition.text="";scoreAnimation=null;
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
  safeRoot.anchorMin=new Vector2(lastSafe.xMin/lastWidth,lastSafe.yMin/lastHeight);safeRoot.anchorMax=new Vector2(lastSafe.xMax/lastWidth,lastSafe.yMax/lastHeight);safeRoot.offsetMin=safeRoot.offsetMax=Vector2.zero;
  scaler.scaleFactor=Mathf.Max(0.01f,Mathf.Min(lastSafe.width/600f,lastSafe.height/960f));
 }
 static Color Hex(string value) {ColorUtility.TryParseHtmlString("#"+value,out var c);return c;}
 static Color TileColor(int value) {return value==0?Hex("253149"):Color.white;}
 Color TileText(int value) {return value==64 || value==128?Hex("2D1B69"):value==1024 || value==2048?Hex("1A1A3A"):value>2048?Gold:Color.white;}
 Sprite TileSprite(int value) {
  if(tileSprites.TryGetValue(value,out var sprite))return sprite;
  string first,second;
  switch(value) {
   case 0:first="253149";second="253149";break;
   case 2:first="546C91";second="3C5277";break;
   case 4:first="657DB1";second="4B6092";break;
   case 8:first="7984C6";second="5D67A4";break;
   case 16:first="9C83C6";second="795FA3";break;
   case 32:first="BA87AD";second="95648A";break;
   case 64:first="E1B7AA";second="CB9488";break;
   case 128:first="E9C8A1";second="D5A776";break;
   case 256:first="699FAD";second="477887";break;
   case 512:first="589B94";second="38776F";break;
   case 1024:first="E9CF8F";second="C9A85E";break;
   case 2048:first="F4DB9C";second="D7B269";break;
   default:first="729D98";second="3E726B";break;
  }
  sprite=GradientRounded(Hex(first),Hex(second));tileSprites[value]=sprite;return sprite;
 }
 Sprite GradientRounded(Color first,Color second) {
  var texture=new Texture2D(64,64,TextureFormat.RGBA32,false);texture.wrapMode=TextureWrapMode.Clamp;texture.filterMode=FilterMode.Bilinear;
  for(int y=0;y<64;y++)for(int x=0;x<64;x++) {
   float dx=Mathf.Max(7-x,x-56),dy=Mathf.Max(7-y,y-56);float distance=Mathf.Sqrt(Mathf.Max(0,dx)*Mathf.Max(0,dx)+Mathf.Max(0,dy)*Mathf.Max(0,dy));
   var color=Color.Lerp(first,second,(x+63-y)/126f);color.a=Mathf.Clamp01(7.5f-distance);texture.SetPixel(x,y,color);
  }
  texture.Apply();return Sprite.Create(texture,new Rect(0,0,64,64),new Vector2(0.5f,0.5f));
 }
 float CellSize {get{return (496f-(session.board.size-1)*10)/session.board.size;}}
 Vector2 CellPosition(int i) {float step=CellSize+10;float edge=248-CellSize/2;return new Vector2(-edge+(i%session.board.size)*step,edge-(i/session.board.size)*step);}
 RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size) {
  var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var rt=go.GetComponent<RectTransform>();rt.anchorMin=rt.anchorMax=new Vector2(0.5f,0.5f);rt.anchoredPosition=position;rt.sizeDelta=size;return rt;
 }
 Image Panel(string name,Transform parent,Vector2 position,Vector2 size,Color color,bool raycast=false) {
  var rt=Rect(name,parent,position,size);var img=rt.gameObject.AddComponent<Image>();img.sprite=rounded;img.type=Image.Type.Sliced;img.color=color;img.raycastTarget=raycast;return img;
 }
 Text Label(string name,Transform parent,string text,Vector2 position,Vector2 size,int fontSize) {
  var rt=Rect(name,parent,position,size);var label=rt.gameObject.AddComponent<Text>();label.font=font;label.text=text;label.fontSize=fontSize;label.alignment=TextAnchor.MiddleCenter;label.color=White;label.raycastTarget=false;return label;
 }
 Button Button(string name,Transform parent,Vector2 position,Vector2 size,Action action,bool primary=false) {
  var img=Panel(name,parent,position,size,primary?Gold:Hex("26334B"),true);var button=img.gameObject.AddComponent<Button>();button.targetGraphic=img;button.navigation=new Navigation{mode=Navigation.Mode.None};
  var colors=button.colors;colors.highlightedColor=new Color(1,0.93f,0.7f);colors.pressedColor=new Color(0.75f,0.7f,0.55f);colors.disabledColor=new Color(0.5f,0.5f,0.5f,0.6f);button.colors=colors;
  var label=Label("Label",img.transform,name,Vector2.zero,size-Vector2.one*12,22);label.color=primary?Ink:White;label.fontStyle=FontStyle.Bold;
  img.gameObject.AddComponent<PremiumButtonMotion>();
  button.onClick.AddListener(()=>{Play(clickTone);action();});return button;
 }
 Button IconButton(string name,Transform parent,Vector2 position,Action action,IconKind kind) {
  var button=Button(name,parent,position,new Vector2(74,74),action);
  button.GetComponentInChildren<Text>().text="";
  AddIcon(button.transform,Vector2.zero,28,kind,White);return button;
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
  Label("Logo",loading.transform,"2048",new Vector2(0,100),new Vector2(500,150),110).color=Gold;
  Label("Subtitle",loading.transform,"THE NUMBER COLLECTION",new Vector2(0,5),new Vector2(500,50),24).color=Gold;
  Panel("Track",loading.transform,new Vector2(0,-100),new Vector2(380,12),Hex("26334B"));loadFill=Panel("Progress",loading.transform,new Vector2(0,-100),new Vector2(380,12),Gold);loadFill.type=Image.Type.Filled;loadFill.fillMethod=Image.FillMethod.Horizontal;
  loadText=Label("Status",loading.transform,"",new Vector2(0,-160),new Vector2(500,70),19);
  modal=Panel("Modal Overlay",safeRoot,Vector2.zero,Vector2.zero,new Color(0.04f,0.04f,0.12f,0.97f),true).gameObject;
  var overlay=modal.GetComponent<RectTransform>();overlay.anchorMin=Vector2.zero;overlay.anchorMax=Vector2.one;overlay.offsetMin=overlay.offsetMax=Vector2.zero;
  modalPanel=Panel("Dialog",modal.transform,Vector2.zero,new Vector2(490,660),Hex("18263B"),true).rectTransform;
  home.SetActive(false);game.SetActive(false);modal.SetActive(false);FitScreen();
 }
 Sprite GradientSprite() {
  var texture=new Texture2D(64,64,TextureFormat.RGBA32,false);for(int y=0;y<64;y++)for(int x=0;x<64;x++)texture.SetPixel(x,y,Color.Lerp(Hex("091321"),Hex("1C2D47"),(x+y)/126f));texture.Apply();return Sprite.Create(texture,new Rect(0,0,64,64),new Vector2(0.5f,0.5f));
 }
 void BuildHome() {
  Label("Home Logo",home.transform,"2048",new Vector2(0,370),new Vector2(500,145),110).color=Gold;
  Label("Edition",home.transform,"THE NUMBER COLLECTION",new Vector2(0,275),new Vector2(500,45),24).color=Gold;
  Label("Description",home.transform,"Join the numbers. Master the board.\nMerge your way to the highest tile.",new Vector2(0,202),new Vector2(500,70),22);
  var preview=Panel("Preview Board",home.transform,new Vector2(0,10),new Vector2(280,280),Hex("111D30"));int[] values={0,2,4,0,8,0,16,2,0,4,0,8,2,0,4,0};
  for(int i=0;i<16;i++) {var cell=Panel("Preview "+i,preview.transform,new Vector2(-99+(i%4)*66,99-(i/4)*66),new Vector2(58,58),TileColor(values[i]));if(values[i]>0){cell.sprite=TileSprite(values[i]);cell.type=Image.Type.Simple;}if(values[i]>0)Label("Value",cell.transform,values[i].ToString(),Vector2.zero,new Vector2(58,58),27).color=TileText(values[i]);}
  Button("New Game",home.transform,new Vector2(0,-190),new Vector2(360,74),ShowLevels,true);
  Button("Resume",home.transform,new Vector2(0,-262),new Vector2(360,64),ShowGame);
  Button("How to Play",home.transform,new Vector2(-95,-335),new Vector2(180,72),ShowInstructions);
  Button("High Scores",home.transform,new Vector2(95,-335),new Vector2(180,72),ShowScores);
  Label("Footer",home.transform,"SWIPE · MERGE · MASTER",new Vector2(0,-422),new Vector2(500,40),16).color=Gold;
 }
 void BuildGame() {
  IconButton("Menu",game.transform,new Vector2(-224,412),ShowMenu,IconKind.Menu);
  Label("Title",game.transform,"2048",new Vector2(-85,406),new Vector2(190,85),60).color=Gold;
  var mute=IconButton("Sound: On",game.transform,new Vector2(150,412),ToggleSound,IconKind.Sound);soundLabel=mute.GetComponentInChildren<Text>();
  IconButton("Restart",game.transform,new Vector2(224,412),ConfirmRestart,IconKind.Restart);
  score=ScoreCard("SCORE",new Vector2(-130,315));best=ScoreCard("BEST",new Vector2(130,315));addition=Label("Score addition",score.transform.parent,"",new Vector2(0,25),new Vector2(220,60),28);
  level=Label("Level",game.transform,"",new Vector2(0,240),new Vector2(540,40),20);level.color=Gold;
  grid=Panel("Board",game.transform,new Vector2(0,-40),new Vector2(520,520),Hex("111D30")).rectTransform;
  Panel("Action Bar",game.transform,new Vector2(0,-350),new Vector2(520,78),Hex("18263B"));
  for(int i=0;i<3;i++) {int action=i;powers[i]=Button(((Power)i).ToString(),game.transform,new Vector2(-170+i*170,-350),new Vector2(155,72),()=>UsePower((Power)action));powerLabels[i]=powers[i].GetComponentInChildren<Text>();powerLabels[i].fontSize=18;powerLabels[i].rectTransform.anchoredPosition=new Vector2(14,0);powerLabels[i].rectTransform.sizeDelta=new Vector2(112,43);AddIcon(powers[i].transform,new Vector2(-55,0),26,(IconKind)((int)IconKind.Undo+i),White);}
  hint=Label("Hint",game.transform,"",new Vector2(0,-408),new Vector2(550,50),17);
  cancelSelection=Button("Cancel selection",game.transform,new Vector2(0,-456),new Vector2(240,52),()=>{CancelSelection();Refresh();}).gameObject;cancelSelection.GetComponentInChildren<Text>().fontSize=16;cancelSelection.SetActive(false);
 }
 Text ScoreCard(string title,Vector2 position) {
  var card=Panel(title,game.transform,position,new Vector2(240,85),Hex("1D2B41"));Label("Caption",card.transform,title,new Vector2(0,23),new Vector2(220,25),16).color=Gold;
  var number=Label("Number",card.transform,"0",new Vector2(0,-13),new Vector2(220,45),34);number.fontStyle=FontStyle.Bold;number.resizeTextForBestFit=true;number.resizeTextMinSize=18;number.resizeTextMaxSize=34;return number;
 }
 RectTransform CreateTile(Transform parent,int index,int value) {
  var image=Panel("Tile "+index,parent,CellPosition(index),Vector2.one*CellSize,Color.white);image.sprite=TileSprite(value);image.type=Image.Type.Simple;var number=Label("Value",image.transform,value.ToString(),Vector2.zero,Vector2.one*(CellSize-8),session.board.size==4?39:session.board.size==5?31:26);number.fontStyle=FontStyle.Bold;number.resizeTextForBestFit=true;number.resizeTextMinSize=12;number.resizeTextMaxSize=number.fontSize;number.color=TileText(value);var shadow=number.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,0.35f);shadow.effectDistance=new Vector2(0,-2);return image.rectTransform;
 }
 void BuildBoard() {
  if(session==null)return;
  foreach(Transform child in grid) {child.gameObject.SetActive(false);Destroy(child.gameObject);}
  int count=session.board.cells.Length;tiles=new Image[count];numbers=new Text[count];
  for(int i=0;i<count;i++)Panel("Empty "+i,grid,CellPosition(i),Vector2.one*CellSize,TileColor(0));
  for(int i=0;i<count;i++) {var tile=CreateTile(grid,i,session.board.cells[i]);tiles[i]=tile.GetComponent<Image>();numbers[i]=tile.GetComponentInChildren<Text>();}
 }
 void Modal(string title,string description) {
  modal.transform.SetAsLastSibling();modal.SetActive(true);
  var motion=modalPanel.GetComponent<PremiumPanelMotion>();if(motion==null)motion=modalPanel.gameObject.AddComponent<PremiumPanelMotion>();motion.Reveal();
  foreach(Transform child in modalPanel){child.gameObject.SetActive(false);Destroy(child.gameObject);}
  Label("Title",modalPanel,title,new Vector2(0,250),new Vector2(440,65),37).color=Gold;
  Label("Description",modalPanel,description,new Vector2(0,165),new Vector2(430,100),21);
  Button("Close",modalPanel,new Vector2(0,-275),new Vector2(180,45),CloseModal).GetComponentInChildren<Text>().fontSize=18;
 }
 void DialogButton(string title,float y,Action action) {Button(title,modalPanel,new Vector2(0,y),new Vector2(425,60),action,true);}
 void CloseModal(){navigation.Back();manager.ShowResult();}
 void OnDestroy() {if(instance!=this)return;instance=null;if(manager!=null){manager.Changed-=OnGameChanged;navigation.Changed-=OnNavigationChanged;}if(moveTone!=null)Destroy(moveTone);if(mergeTone!=null)Destroy(mergeTone);if(clickTone!=null)Destroy(clickTone);if(winTone!=null)Destroy(winTone);}
}
}
