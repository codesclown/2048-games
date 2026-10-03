# AdMob test integration

Google Mobile Ads Unity SDK 11.5.0 is installed through the official OpenUPM package. Google sample Android/iOS app IDs are saved in Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset. AdMobProvider uses only Google sample banner/rewarded unit IDs; no production requests are configured.

A 320×50 banner is anchored at the bottom. The UI reserves 90 logical points initially, and expands the reservation from the SDK-reported pixel height when needed. All gameplay, home and journey content sits above that space; dialogs keep a full-screen backdrop. Header controls have a 6-point top inset within the safe content area.

When the classic board has no valid moves and continuation is still available, the result dialog offers Watch ad · Rewind 5 moves. A completed reward callback followed by ad close grants the existing once-per-board rewind of up to five previous actions, preserving the score. Closing without earning or ad presentation failure returns to the result without granting anything. Callbacks are dispatched on Unity's main thread; duplicate completions are ignored. Rewarded ads are preloaded, expire after 3500 seconds, and failed loads retry at 30-second intervals. Desktop native builds do not initialize the mobile SDK.

The editor can show the SDK's simulated test ads. Android/iOS device builds are required to verify native ad delivery and safe-area positioning. Production rollout must replace sample IDs and configure consent before initialization; this change is test-only.

Official setup: https://developers.google.com/admob/unity/quick-start
Reward callback: https://developers.google.com/admob/unity/rewarded
