using UnityEngine;
namespace Premium2048 {
public static class PremiumTheme {
 public static Color Color(string hex){ColorUtility.TryParseHtmlString("#"+hex,out var value);return value;}
 public static Color Ink=>Color("0B1627");
 public static Color Text=>Color("F7F8FC");
 public static Color Muted=>Color("BCC8DA");
 public static Color Accent=>Color("EAC780");
 public static Color Surface=>Color("18283F");
 public static void TileColors(int value,out Color first,out Color second) {
  string a,b;
  switch(value) {
   case 0:a=b="24334A";break;
   case 2:a="405977";b="30435C";break;
   case 4:a="546DA0";b="435989";break;
   case 8:a="879DCE";b="7489B6";break;
   case 16:a="AC96CE";b="9A83BF";break;
   case 32:a="C8A0BA";b="B58FA8";break;
   case 64:a="E1B7AA";b="CB9488";break;
   case 128:a="E9C8A1";b="D5A776";break;
   case 256:a="91BDC8";b="7BACB9";break;
   case 512:a="92C6B8";b="7AAFA1";break;
   case 1024:a="E9CF8F";b="C9A85E";break;
   case 2048:a="F4DB9C";b="D7B269";break;
   case 4096:a="F28B6A";b="D95F3B";break;
   case 8192:a="5130A4";b="3E2282";break;
   default:a="9AC3B8";b="82AA9E";break;
  }
  first=Color(a);second=Color(b);
 }
 public static Color TileText(int value,bool contrast=false){if(contrast)return Ink;TileColors(value,out var a,out var b);return Mathf.Min(Contrast(Text,a),Contrast(Text,b))>=4.5f?Text:Ink;}
 public static float Contrast(Color a,Color b){float x=Luminance(a),y=Luminance(b);return (Mathf.Max(x,y)+.05f)/(Mathf.Min(x,y)+.05f);}
 static float Luminance(Color c){return .2126f*Linear(c.r)+.7152f*Linear(c.g)+.0722f*Linear(c.b);}
 static float Linear(float value){return value<=.04045f?value/12.92f:Mathf.Pow((value+.055f)/1.055f,2.4f);}
}
}
