using System;
using UnityEngine;
namespace Premium2048 {
// Implement this component with your native advertising SDK and assign it to the game.
// Call completed(true) only after the SDK reports a completed rewarded video.
public abstract class RewardProvider : MonoBehaviour {
 public abstract bool IsReady { get; }
 public abstract void Show(Action<bool> completed);
}
}
