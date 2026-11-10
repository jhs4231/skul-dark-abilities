using System;
using System.Linq;
using UnityEngine;

namespace FanDarkAbilities
{
    public sealed class RunHud : MonoBehaviour
    {
        Font font;bool details;
        void Update(){if(Input.GetKeyDown(KeyCode.F5))details=!details;}
        void OnGUI(){
            var h=GetComponent<EffectHost>();if(h==null||h.owner==null||h.owner.health.dead)return;
            if(font==null)font=Font.CreateDynamicFontFromOSFont("Malgun Gothic",18);
            var style=new GUIStyle(GUI.skin.label);style.font=font;style.fontSize=Math.Max(16,Screen.height/55);style.wordWrap=true;
            float x=Screen.width*.35f,y=10,w=Screen.width*.3f;
            if(h.Has(Gift.Fission)){int remaining=(int)Math.Ceiling(h.fissionTime/Math.Max(.01,h.Power(Gift.Fission)));style.normal.textColor=remaining<60?Color.red:Color.yellow;
                GUI.Box(new Rect(x,y,w,34),"");GUI.Label(new Rect(x+8,y,w-16,34),string.Format("핵분열  {0:00}:{1:00}",remaining/60,remaining%60),style);y+=38;
            }
            if(!details)return;
            string text=h.ActiveSummary();if(text.Length==0)return;
            style.normal.textColor=Color.white;float height=style.CalcHeight(new GUIContent(text),w-16)+18;
            GUI.Box(new Rect(x,y,w,height),"");GUI.Label(new Rect(x+8,y+8,w-16,height-16),text,style);
        }
    }
}
