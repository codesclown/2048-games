using UnityEngine;
namespace Premium2048 {
// Baked scenes are editable previews. Play mode renders the authoritative interactive UI.
public sealed class PremiumPreviewCanvas : MonoBehaviour {
 void Awake(){gameObject.SetActive(false);}
}
}
