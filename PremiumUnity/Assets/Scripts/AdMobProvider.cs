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
 string BannerId=>Application.platform==RuntimePlatform.IPhonePlayer?"ca-app-pub-3940256099942544/2934735716":"ca-app-pub-3940256099942544/6300978111";
 string RewardId=>Application.platform==RuntimePlatform.IPhonePlayer?"ca-app-pub-3940256099942544/1712485313":"ca-app-pub-3940256099942544/5224354917";
 void Start(){
  if(!Application.isMobilePlatform && !Application.isEditor)return;
  MobileAds.Initialize(status=>Dispatch(()=>{
   if(status==null)return;initialized=true;LoadBanner();LoadReward();
  }));
 }
 void Dispatch(Action action){if(!destroyed)callbacks.Enqueue(action);}
 void Update(){
  while(callbacks.TryDequeue(out var action))action();
  if(initialized && completion==null && Time.realtimeSinceStartup>=retryAt){
   if(rewarded==null || Time.realtimeSinceStartup-rewardLoadedAt>3500)LoadReward();
  }
 }
 void LoadBanner(){
  banner?.Destroy();banner=new BannerView(BannerId,AdSize.Banner,AdPosition.Bottom);
  banner.OnBannerAdLoaded+=()=>Dispatch(()=>BannerHeightPixels=banner.GetHeightInPixels());
  banner.LoadAd(new AdRequest());
 }
 void LoadReward(){
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
 }
 public override void Show(Action<bool> completed){
  if(!IsReady){completed(false);return;}
  earned=false;completion=completed;banner?.Hide();
  if(Application.isEditor){earned=true;}
  try{
   rewarded.Show(reward=>{
    earned=true;
   });
  }
  catch{Finish(false);}
 }
 void Finish(bool success){
  var callback=completion;completion=null;
  if(callback==null)return;
  banner?.Show();LoadReward();callback(success);
 }
 void OnDestroy(){
  destroyed=true;banner?.Destroy();rewarded?.Destroy();BannerHeightPixels=0;
  var callback=completion;completion=null;callback?.Invoke(false);
 }
}
}
