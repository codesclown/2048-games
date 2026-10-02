using UnityEngine;

namespace Premium2048 {
public static class PremiumTheme {
 public static Color Color(string hex){ColorUtility.TryParseHtmlString("#"+hex,out var value);return value;}
 public static Color Ink=>Color("0F0F23");
 public static Color Text=>Color("F7F8FC");
 public static Color Muted=>Color("B8B8FF");
 public static Color Accent=>Color("FFD700");
 public static Color Surface=>Color("1A1A3A");

 public static void TileColors(int value,out Color first,out Color second) {
  string a,b;
  switch(value) {
   case 0:a="2D1B69";b="1A1A3A";break; // empty tile
   case 2:a="667EEA";b="764BA2";break; // 2: purple-blue
   case 4:a="F093FB";b="F5576C";break; // 4: pink-coral
   case 8:a="4FACFE";b="00F2FE";break; // 8: cyan-blue
   case 16:a="43E97B";b="38F9D7";break; // 16: emerald-mint
   case 32:a="FA709A";b="FEE140";break; // 32: sunset pink-yellow
   case 64:a="A8EDEA";b="FED6E3";break; // 64: pastel aqua-pink
   case 128:a="FFECD2";b="FCB69F";break; // 128: peach cream-salmon
   case 256:a="FFD89B";b="19547B";break; // 256: golden sand-navy
   case 512:a="667EEA";b="764BA2";break; // 512: deep royal violet
   case 1024:a="FFD700";b="FFB347";break; // 1024: radiant gold
   case 2048:a="FF6B6B";b="FFD93D";break; // 2048: sunburst flame
   case 4096:a="2D1B69";b="11998E";break; // 4096: cosmic violet-teal
   case 8192:a="FA709A";b="764BA2";break; // 8192: rose-violet
   default:a="2D1B69";b="11998E";break;
  }
  first=Color(a);second=Color(b);
 }

 public static Color TileText(int value,bool contrast=false) {
  if(contrast)return Ink;
  if(value==64 || value==128) return Color("2D1B69");
  if(value==1024 || value==2048) return Color("1A1A3A");
  if(value>=4096) return Color("FFD700");
  return Color("FFFFFF");
 }

 public static float Contrast(Color a,Color b){float x=Luminance(a),y=Luminance(b);return (Mathf.Max(x,y)+.05f)/(Mathf.Min(x,y)+.05f);}
 static float Luminance(Color c){return .2126f*Linear(c.r)+.7152f*Linear(c.g)+.0722f*Linear(c.b);}
 static float Linear(float value){return value<=.04045f?value/12.92f:Mathf.Pow((value+.055f)/1.055f,2.4f);}
}
}
