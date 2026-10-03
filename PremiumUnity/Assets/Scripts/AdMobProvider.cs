using System;
using System.Collections.Concurrent;
using GoogleMobileAds.Api;
using UnityEngine;

namespace Premium2048 {
public sealed class AdMobProvider : RewardProvider {
 public static float BannerHeightPixels {get;private set;}
 readonly ConcurrentQueue<Action> callbacks=new ConcurrentQueue<Action>();
 BannerView banner;RewardedAd rewarded;
 Action<bool> completion;bool earned,initialized,destroyed;
 float retryAt,rewardLoadedAt;
 public override bool IsReady=>completion==null && rewarded!=null && rewarded.CanShowAd();
 
 [Header("Production Ad Unit IDs (Leave empty to use official Google test IDs)")]
 [SerializeField] string customAndroidBannerId;
 [SerializeField] string customAndroidRewardId;

 string BannerId {
  get {
   if (!string.IsNullOrEmpty(customAndroidBannerId) && Application.platform == RuntimePlatform.Android) return customAndroidBannerId;
   string pref = PlayerPrefs.GetString("AdMob_AndroidBannerId", "");
   if (!string.IsNullOrEmpty(pref) && Application.platform == RuntimePlatform.Android) return pref;
   return Application.platform==RuntimePlatform.IPhonePlayer?"ca-app-pub-3940256099942544/2934735716":"ca-app-pub-3940256099942544/6300978111";
  }
 }
 string RewardId {
  get {
   if (!string.IsNullOrEmpty(customAndroidRewardId) && Application.platform == RuntimePlatform.Android) return customAndroidRewardId;
   string pref = PlayerPrefs.GetString("AdMob_AndroidRewardId", "");
   if (!string.IsNullOrEmpty(pref) && Application.platform == RuntimePlatform.Android) return pref;
   return Application.platform==RuntimePlatform.IPhonePlayer?"ca-app-pub-3940256099942544/1712485313":"ca-app-pub-3940256099942544/5224354917";
  }
 }

 void Start(){
  if(!Application.isMobilePlatform && !Application.isEditor)return;
  try {
   MobileAds.Initialize(status=>Dispatch(()=>{
    if(status==null)return;initialized=true;LoadBanner();LoadReward();
   }));
  } catch (Exception ex) {
   Debug.LogWarning("AdMob initialization skipped or failed: " + ex.Message);
  }
 }
 void Dispatch(Action action){if(!destroyed && action!=null)callbacks.Enqueue(action);}
 void Update(){
  while(callbacks.TryDequeue(out var action)){
   try { action(); } catch (Exception ex) { Debug.LogWarning("AdMob dispatched action warning: " + ex.Message); }
  }
  if(initialized && completion==null && Time.realtimeSinceStartup>=retryAt){
   if(rewarded==null || Time.realtimeSinceStartup-rewardLoadedAt>3500)LoadReward();
  }
 }
 void LoadBanner(){
  try {
   banner?.Destroy();banner=new BannerView(BannerId,AdSize.Banner,AdPosition.Bottom);
   banner.OnBannerAdLoaded+=()=>Dispatch(()=>BannerHeightPixels=banner.GetHeightInPixels());
   banner.LoadAd(new AdRequest());
  } catch (Exception ex) {
   Debug.LogWarning("AdMob Banner load error: " + ex.Message);
  }
 }
 void LoadReward(){
  try {
   retryAt=Time.realtimeSinceStartup+30;rewarded?.Destroy();rewarded=null;
   RewardedAd.Load(RewardId,new AdRequest(),(ad,error)=>{
    if(destroyed){ad?.Destroy();return;}
    Dispatch(()=>{
     if(error!=null || ad==null){ad?.Destroy();return;}
     rewarded=ad;rewardLoadedAt=Time.realtimeSinceStartup;
     ad.OnAdFullScreenContentClosed+=()=>{
      Dispatch(()=>{
       bool success=earned || Application.isEditor;
       Finish(success);
      });
     };
     ad.OnAdFullScreenContentFailed+=errorInfo=>Dispatch(()=>Finish(false));
    });
   });
  } catch (Exception ex) {
   Debug.LogWarning("AdMob Reward load error: " + ex.Message);
  }
 }
 public override void Show(Action<bool> completed){
  if(!IsReady){completed?.Invoke(false);return;}
  earned=false;completion=completed;
  try { banner?.Hide(); } catch {}
  if(Application.isEditor){earned=true;}
  try{
   rewarded.Show(reward=>{
    earned=true;
   });
  }
  catch (Exception ex){
   Debug.LogWarning("Ad presentation caught: " + ex.Message);
   Finish(false);
  }
 }
 void Finish(bool success){
  var callback=completion;completion=null;
  if(callback==null)return;
  try { banner?.Show(); } catch {}
  try { LoadReward(); } catch {}
  try { callback(success); } catch (Exception ex) { Debug.LogError("Reward callback exception: " + ex.Message); }
 }
 void OnDestroy(){
  destroyed=true;
  try { banner?.Destroy(); } catch {}
  try { rewarded?.Destroy(); } catch {}
  BannerHeightPixels=0;
  var callback=completion;completion=null;
  try { callback?.Invoke(false); } catch {}
 }
}
}
