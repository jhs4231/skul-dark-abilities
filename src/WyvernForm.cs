using System;
using Characters;
using UnityEngine;

namespace FanDarkAbilities
{
    public sealed class WyvernForm : MonoBehaviour
    {
        EffectHost host;SpriteRenderer body,original,leftWing,rightWing;bool originalEnabled;
        static Sprite dragon,wing;
        static void CreateArt(){
            if(dragon!=null)return;
            var texture=new Texture2D(96,64);texture.filterMode=FilterMode.Point;var pixels=new Color[96*64];
            for(int y=0;y<64;y++)for(int x=0;x<96;x++){
                bool torso=(x-46)*(x-46)/4+(y-26)*(y-26)<150;
                bool head=(x-69)*(x-69)+(y-45)*(y-45)<85;
                bool neck=x>49&&x<65&&y>23&&y<49;
                bool jaw=x>=68&&x<88&&y>=39&&y<47;
                bool tail=x<40&&x>5&&Math.Abs(y-(14+x/4))<3;
                bool legs=y>5&&y<24&&((x>36&&x<42)||(x>51&&x<57));
                bool horn=x>60&&x<67&&y>=49&&y<61;
                if(torso||head||neck||jaw||tail||legs||horn)pixels[y*96+x]=new Color(.32f,.08f,.48f);
                if((x==74||x==75)&&y>=47&&y<=49)pixels[y*96+x]=new Color(1,.65f,.05f);
                if(jaw&&y==39&&x%4==0)pixels[y*96+x]=Color.white;
            }
            texture.SetPixels(pixels);texture.Apply();UnityEngine.Object.DontDestroyOnLoad(texture);dragon=Sprite.Create(texture,new Rect(0,0,96,64),new Vector2(.5f,.2f),32);UnityEngine.Object.DontDestroyOnLoad(dragon);
            var t=new Texture2D(64,48);t.filterMode=FilterMode.Point;var p=new Color[64*48];
            for(int y=0;y<48;y++)for(int x=0;x<64;x++)if(y<40&&y>Math.Abs(x-32)/2&&y<44-Math.Abs(x-32)/3)p[y*64+x]=new Color(.6f,.13f,.75f);
            t.SetPixels(p);t.Apply();UnityEngine.Object.DontDestroyOnLoad(t);wing=Sprite.Create(t,new Rect(0,0,64,48),new Vector2(.5f,0),32);UnityEngine.Object.DontDestroyOnLoad(wing);
        }
        public static void Begin(EffectHost h){
            var existing=h.owner.GetComponentInChildren<WyvernForm>();if(existing!=null)return;
            CreateArt();var go=new GameObject("FanWyvernForm");go.transform.SetParent(h.owner.transform,false);go.transform.localPosition=Vector3.zero;
            var f=go.AddComponent<WyvernForm>();f.host=h;f.original=h.owner.spriteEffectStack.mainRenderer;f.originalEnabled=f.original.enabled;
            f.body=go.AddComponent<SpriteRenderer>();f.body.sprite=dragon;f.body.sortingLayerID=f.original.sortingLayerID;f.body.sortingOrder=f.original.sortingOrder+2;
            f.leftWing=f.Wing("WingL",-1);f.rightWing=f.Wing("WingR",1);
        }
        SpriteRenderer Wing(string name,int sign){var go=new GameObject(name);go.transform.SetParent(transform,false);go.transform.localPosition=new Vector3(sign*.3f,.5f,0);var s=go.AddComponent<SpriteRenderer>();s.sprite=wing;s.sortingLayerID=body.sortingLayerID;s.sortingOrder=body.sortingOrder-1;return s;}
        void LateUpdate(){
            if(host==null||host.owner==null||host.owner.health.dead||host.time>=host.wyvernUntil||!host.Has(Gift.Wyvern)){Destroy(gameObject);return;}
            original.enabled=false;
            transform.localScale=new Vector3(host.owner.lookingDirection==Character.LookingDirection.Right?1:-1,1,1);
            float angle=Mathf.Sin(host.time*12)*25;
            leftWing.transform.localRotation=Quaternion.Euler(0,0,-35-angle);rightWing.transform.localRotation=Quaternion.Euler(0,0,35+angle);
        }
        void OnDestroy(){if(original!=null)original.enabled=originalEnabled;}
    }
}
