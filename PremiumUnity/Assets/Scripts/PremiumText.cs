using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Premium2048 {
[Serializable] public sealed class TranslationEntry {public string en,es;}
[Serializable] sealed class TranslationCatalog {public TranslationEntry[] entries;}
public static class GameLocalization {
 static Dictionary<string,string> spanish;
 public static IEnumerable<string> Sources {get{Ensure();return spanish.Keys;}}
 static void Ensure(){if(spanish!=null)return;spanish=new Dictionary<string,string>();var catalog=Resources.Load<TextAsset>("Text/strings");if(catalog==null)throw new InvalidOperationException("Missing bundled UI translations.");foreach(var entry in JsonUtility.FromJson<TranslationCatalog>(catalog.text).entries)spanish.Add(entry.en,entry.es);}
 public static bool Has(string english){Ensure();return spanish.ContainsKey(english);}
 public static string Translate(string language,string english,params object[] arguments){Ensure();string value=language=="es" && spanish.TryGetValue(english,out var translated)?translated:english;return arguments!=null && arguments.Length>0?string.Format(language=="es"?new System.Globalization.CultureInfo("es"):System.Globalization.CultureInfo.InvariantCulture,value,arguments):value;}
}
// Source text and metrics survive live language/text-size changes without rebuilding the game.
public sealed class PremiumText : MonoBehaviour {
 public string Source {get;private set;}
 public int BaseSize {get;private set;}
 public bool Scales=true;
 object[] arguments;Text label;GameManager manager;
 static Font regular,bold;
 public void Initialize(GameManager manager,string source,int size,params object[] arguments){this.manager=manager;label=GetComponent<Text>();BaseSize=size;Set(source,arguments);}
 public void Set(string source,params object[] arguments){Source=source;this.arguments=arguments;Apply();}
 public void Size(int size){BaseSize=size;Apply();}
 public void Emphasis(){label.fontStyle=FontStyle.Bold;Apply();}
 public void Apply() {
  if(label==null || manager==null)return;
  if(regular==null)regular=Resources.Load<Font>("Fonts/NotoSans-Regular");if(bold==null)bold=Resources.Load<Font>("Fonts/NotoSans-Bold");
  label.font=label.fontStyle==FontStyle.Bold?bold:regular;
  if(label.font==null)label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
  if(label.font==null)label.font=Font.CreateDynamicFontFromOSFont(new[]{"Arial","Helvetica","sans-serif"},label.fontSize);
  label.text=GameLocalization.Translate(manager.Language,Source??"",arguments);
  label.fontSize=Mathf.RoundToInt(BaseSize*(manager.LargeText && Scales?1.15f:1));
  if(label.resizeTextForBestFit)label.resizeTextMaxSize=label.fontSize;
 }
}
}
