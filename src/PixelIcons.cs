using System;
using UnityEngine;

namespace FanDarkAbilities
{
    internal sealed class PixelIcon
    {
        readonly Color[] p=new Color[1024];Color ink;
        void Dot(int x,int y){if(x>=0&&x<32&&y>=0&&y<32)p[y*32+x]=ink;}
        void Rect(int x,int y,int w,int h){for(int yy=y;yy<y+h;yy++)for(int xx=x;xx<x+w;xx++)Dot(xx,yy);}
        void Line(int x,int y,int xx,int yy,int thick){int n=Math.Max(Math.Abs(xx-x),Math.Abs(yy-y));for(int i=0;i<=n;i++)Rect(x+(xx-x)*i/Math.Max(1,n),y+(yy-y)*i/Math.Max(1,n),thick,thick);}
        void Circle(int x,int y,int r,bool fill){for(int yy=-r;yy<=r;yy++)for(int xx=-r;xx<=r;xx++){int d=xx*xx+yy*yy;if(d<=r*r&&(fill||d>=(r-2)*(r-2)))Dot(x+xx,y+yy);}}
        public static Color[] Draw(int index,bool cursed){
            var d=new PixelIcon();
            d.ink=cursed?new Color(.18f,.02f,.06f):new Color(.07f,.03f,.14f);d.Rect(2,2,28,28);
            d.ink=cursed?new Color(1,.22f,.4f):new Color(.73f,.42f,1);d.Line(4,4,26,4,1);d.Line(4,26,26,26,1);
            d.ink=new Color(.95f,.9f,1);
            int shape=index<27?index:index-27;
            switch(shape){
                case 0:for(int i=0;i<8;i++){double a=i*Math.PI/4;d.Line(15,15,15+(int)(Math.Cos(a)*11),15+(int)(Math.Sin(a)*11),2);}d.Circle(16,16,5,true);break;
                case 1:d.Circle(16,12,6,true);d.Line(11,15,16,25,2);d.Line(20,15,16,25,2);break;
                case 2:d.Rect(8,10,17,3);d.Line(8,13,7,22,2);d.Line(9,15,15,22,2);d.Line(15,22,22,15,2);d.Line(22,15,24,22,2);break;
                case 3:d.Line(15,8,15,24,2);d.Line(7,19,24,19,2);d.Line(7,18,4,12,1);d.Line(7,18,10,12,1);d.Line(24,18,21,12,1);d.Line(24,18,27,12,1);d.Rect(4,10,8,2);d.Rect(21,10,8,2);break;
                case 4:d.Circle(16,16,9,false);d.Circle(12,13,3,true);d.Circle(23,23,3,true);break;
                case 5:case 14:case 16:d.Line(7,23,24,23,2);d.Line(7,23,8,13,2);d.Line(8,13,15,6,2);d.Line(15,6,23,13,2);d.Line(23,13,24,23,2);if(shape==14){d.Rect(14,11,3,10);d.Rect(11,14,9,3);}break;
                case 6:d.Rect(12,7,7,18);d.Rect(7,12,18,7);break;
                case 7:d.Line(7,7,23,23,3);d.Circle(24,24,4,false);d.Rect(7,22,3,3);d.Rect(21,7,3,3);break;
                case 8:d.Line(15,6,15,22,3);d.Line(7,17,23,17,3);d.Circle(16,24,4,true);break;
                case 9:d.Circle(16,16,10,true);d.ink=cursed?new Color(.18f,.02f,.06f):new Color(.07f,.03f,.14f);d.Circle(21,20,9,true);break;
                case 10:d.Rect(8,9,16,10);for(int i=0;i<4;i++)d.Rect(8+i*4,19,3,5);d.Line(8,8,23,8,2);break;
                case 11:d.Line(19,25,10,15,4);d.Line(10,15,19,15,3);d.Line(19,15,12,6,3);break;
                case 12:d.Line(13,10,13,24,2);d.Line(13,24,23,21,2);d.Line(23,21,23,7,2);d.Circle(9,9,4,true);d.Circle(20,7,4,true);break;
                case 13:d.Line(7,10,13,24,4);d.Line(13,24,17,15,4);d.Line(17,15,23,21,4);d.Line(23,21,25,10,2);d.Rect(10,8,13,7);break;
                case 15:for(int i=0;i<3;i++){d.Circle(8+i*8,19+(i%2)*5,3,true);d.Rect(6+i*8,8+(i%2)*5,5,7);}break;
                case 17:for(int i=0;i<5;i++)d.Line(8+i,8+i*3,22-i,8+i*3,2);break;
                case 18:d.Circle(16,16,9,false);for(int i=0;i<4;i++){d.Rect(5+i*6,5+i%2*18,3,5);}break;
                case 19:for(int i=0;i<6;i++){double a=i*Math.PI/3;d.Line(15,15,15+(int)(Math.Cos(a)*11),15+(int)(Math.Sin(a)*11),2);}break;
                case 20:d.Circle(9,10,4,true);d.Circle(21,15,6,true);d.Circle(10,23,3,true);break;
                case 21:for(int i=0;i<3;i++)d.Line(7,9+i*6,24-i*3,13+i*6,2);break;
                case 22:for(int i=0;i<2;i++){d.Line(7+i*8,8,15+i*8,16,3);d.Line(15+i*8,16,7+i*8,24,3);}break;
                case 23:d.Circle(16,18,9,true);d.Rect(11,6,10,8);d.ink=new Color(.1f,.03f,.15f);d.Rect(10,16,4,5);d.Rect(19,16,4,5);d.Rect(14,9,3,5);break;
                case 24:d.Circle(16,16,10,false);d.Circle(16,16,5,false);d.Rect(15,15,3,3);break;
                case 25:d.Circle(10,16,6,false);d.Circle(22,16,6,false);break;
                default:d.Rect(7,7,18,18);d.ink=new Color(.1f,.03f,.15f);d.Circle(11,11,2,true);d.Circle(21,21,2,true);d.Circle(16,16,2,true);d.Circle(11,21,2,true);d.Circle(21,11,2,true);break;
            }
            return d.p;
        }
    }
}
