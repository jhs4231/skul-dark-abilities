using System;
using System.Collections.Generic;
using System.Linq;
using Characters;
using UnityEngine;

namespace FanDarkAbilities
{
    public sealed class FanEffect : MonoBehaviour
    {
        public EffectHost host;
        internal string key;
        float life,age,tick,radius; double damage;
        int mode;Vector3 start,velocity,destination;Character target;
        SpriteRenderer sprite;
        readonly HashSet<int> hit = new HashSet<int>();
        static Sprite disk;
        internal static Sprite Shape {
            get {
                if(disk!=null)return disk;
                var t=new Texture2D(32,32);t.filterMode=FilterMode.Point;var p=new Color[1024];
                for(int y=0;y<32;y++)for(int x=0;x<32;x++){float r=Vector2.Distance(new Vector2(x,y),new Vector2(15.5f,15.5f));p[y*32+x]=r<15?Color.white:Color.clear;}
                t.SetPixels(p);t.Apply();UnityEngine.Object.DontDestroyOnLoad(t);
                disk=Sprite.Create(t,new Rect(0,0,32,32),new Vector2(.5f,.5f),32);UnityEngine.Object.DontDestroyOnLoad(disk);return disk;
            }
        }
        static FanEffect Make(EffectHost h,Vector3 pos,float duration,float radius,double damage,string key,Color color,int mode) {
            var go=new GameObject("FanFX_"+key);if(Level.Map.Instance!=null)go.transform.SetParent(Level.Map.Instance.transform);
            go.transform.position=pos;
            var e=go.AddComponent<FanEffect>();e.host=h;e.life=duration;e.radius=radius;e.damage=damage;e.key=key;e.mode=mode;e.start=pos;
            e.sprite=go.AddComponent<SpriteRenderer>();e.sprite.sprite=Shape;e.sprite.color=color;e.sprite.sortingOrder=100;
            go.transform.localScale=Vector3.one*(radius*2);return e;
        }
        public static void Flash(EffectHost h,Vector3 pos,Color color,float radius) {Make(h,pos,.25f,radius,0,"Flash",color,0);}
        public static void Zone(EffectHost h,Vector3 pos,float radius,float duration,double damage,string key,Color color) {Make(h,pos,duration,radius,damage,key,color,1);}
        public static void Orb(EffectHost h,Vector3 pos,float duration,double damage,string key,Color color) {Make(h,pos,duration,.2f,damage,key,color,2);}
        public static void Storm(EffectHost h,Vector3 pos,float duration,double damage) {Make(h,pos,duration,1.1f,damage,"Storm",Color.cyan,3);}
        public static void Projectile(EffectHost h,Vector3 pos,Vector2 dir,float speed,double damage,string key,Color color) {
            var e=Make(h,pos,2,.2f,damage,key,color,4);e.velocity=(Vector3)dir*speed;e.transform.localScale=new Vector3(.8f,.12f,1);
        }
        public static void Fireball(EffectHost h,Vector3 start,Vector3 end,float flameDuration,double damage){
            var e=Make(h,start,.6f,.25f,damage,"Fireball",new Color(1,.35f,.05f),5);e.destination=end;e.tick=flameDuration;
        }
        public static bool InZone(EffectHost h,Vector3 point,string key){
            return FindObjectsOfType<FanEffect>().Any(e=>e.host==h&&e.key==key&&Vector3.Distance(point,e.transform.position)<e.radius);
        }
        void Update(){
            if(host==null||host.owner==null||host.owner.health.dead){Destroy(gameObject);return;}
            float dt=host.owner.chronometer.master.deltaTime;if(dt<=0)return;age+=dt;
            if(age>=life){
                if(mode==5){Zone(host,destination,1,tick,damage*.4,"Flame",new Color(1,.3f,.05f));host.Burst(destination,1.5f,damage,"Fireball",false);}
                Destroy(gameObject);return;
            }
            if(mode==0){var c=sprite.color;c.a=1-age/life;sprite.color=c;return;}
            if(mode==5){transform.position=Vector3.Lerp(start,destination,age/life)+Vector3.up*Mathf.Sin(age/life*Mathf.PI)*2;return;}
            if(mode==2){
                if(target==null||target.health.dead)target=host.Enemies().OrderBy(e=>(e.transform.position-transform.position).sqrMagnitude).FirstOrDefault();
                if(target==null){transform.position=host.owner.transform.position+new Vector3(Mathf.Sin(age*4),1,0);return;}
                transform.position=Vector3.MoveTowards(transform.position,target.collider.bounds.center,dt*7);
                if(Vector3.Distance(transform.position,target.collider.bounds.center)<.4f&&host.time>=tick){tick=host.time+.5f;host.Strike(target,damage,key);
                    if(key=="Ghost"){host.Burst(transform.position,1,damage*.5,key,false);Destroy(gameObject);}}
                return;
            }
            if(mode==4){
                Vector3 before=transform.position;transform.position+=velocity*dt;
                foreach(var e in host.Enemies()){
                    Vector3 point=e.collider.bounds.ClosestPoint(transform.position);float distance=DistanceSegment(point,before,transform.position);
                    if(distance>.4f||!hit.Add(e.GetInstanceID()))continue;
                    host.Strike(e,damage,key,key=="Wind",Damage.AttackType.Projectile);
                    if(key=="Wind"&&UnityEngine.Random.value<host.Scale(Gift.Wind,.05+.05*host.Level(Gift.Wind)))host.Status(e,CharacterStatus.Kind.Wound);
                }return;
            }
            if(mode==3)transform.position=start+Vector3.right*Mathf.Sin(age*2)*3;
            if(key=="Wyvern")transform.position=host.owner.transform.position;
            transform.localScale=mode==3?new Vector3(radius,2.5f,1):new Vector3(radius*2,.25f,1);
            var color=sprite.color;color.a=.4f;sprite.color=color;
            if(damage>0&&host.time>=tick){tick=host.time+.5f;
                foreach(var e in host.Enemies().Where(e=>Vector3.Distance(e.transform.position,transform.position)<radius))host.Strike(e,damage,key);
            }
        }
        static float DistanceSegment(Vector3 p,Vector3 a,Vector3 b){var d=b-a;float t=d.sqrMagnitude==0?0:Mathf.Clamp01(Vector3.Dot(p-a,d)/d.sqrMagnitude);return Vector3.Distance(p,a+d*t);}
    }
    // A damageable enemy-layer target rather than a real enemy: no loot or wave-clear membership.
    public sealed class FanDummy : Level.DestructibleObject, ITarget
    {
        public EffectHost host;BoxCollider2D body;float lastHit;
        public override Collider2D collider {get{return body;}}
        Character ITarget.character {get{return null;}}
        Level.DestructibleObject ITarget.damageable {get{return this;}}
        Transform ITarget.transform {get{return transform;}}
        public static void Create(EffectHost h){
            var go=new GameObject("FanScarecrow");if(Level.Map.Instance!=null)go.transform.SetParent(Level.Map.Instance.transform);
            int layer=LayerMask.NameToLayer("EnemyBody");go.layer=layer<0?10:layer;
            go.transform.position=h.owner.transform.position+Vector3.right*UnityEngine.Random.Range(-3f,3f);
            var d=go.AddComponent<FanDummy>();d.host=h;d.body=go.AddComponent<BoxCollider2D>();d.body.size=new Vector2(.7f,1.5f);d.body.isTrigger=true;
            var target=go.AddComponent<Target>();target.damageable=d;Plugin.Set(target,"_collider",d.body);
            var s=go.AddComponent<SpriteRenderer>();s.sprite=Catalog.Icon((int)Gift.Scarecrow,false);s.color=new Color(.8f,.6f,.3f);s.sortingOrder=5;go.transform.localScale=Vector3.one;
        }
        public override void Hit(Character from,ref Damage damage,Vector2 force){
            if(host==null||from!=host.owner||damage.amount<=0||host.time<lastHit+.12f)return;
            lastHit=host.time;host.ScarecrowHit();FanEffect.Flash(host,transform.position,Color.green,.4f);
        }
        void Update(){if(host==null||!host.Has(Gift.Scarecrow)||!host.Enemies().Any())Destroy(gameObject);}
    }
}
