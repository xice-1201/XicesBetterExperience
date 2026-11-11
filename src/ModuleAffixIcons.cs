using System;
using System.Collections.Generic;
using UnityEngine;

namespace XicesBetterExperience
{
    internal static class ModuleAffixIcons
    {
        private static readonly string[] ids = {
            "MODULE_EFFECT_PREMIUM", "MODULE_EFFECT_INFERIOR", "MODULE_EFFECT_RECEIVER",
            "MODULE_EFFECT_TOP_MOUNT", "MODULE_EFFECT_SIDE_MOUNT", "MODULE_EFFECT_OVERVOLTED",
            "MODULE_EFFECT_DEGRADING", "MODULE_EFFECT_LEARNING_ALGORITHM", "MODULE_EFFECT_NEGATIVE_FEEDBACK"
        };
        private static readonly Color dark = Rgb(23, 33, 53);
        private static readonly Color pale = Rgb(243, 247, 255);
        private static readonly Color[] colors = { Rgb(255,211,99), Rgb(184,193,207), Rgb(104,200,255),
            Rgb(104,200,255), Rgb(104,200,255), Rgb(255,152,91), Rgb(255,187,103), Rgb(105,229,214), Rgb(196,160,255) };
        private static readonly Dictionary<string, List<MarkerTriangle>> cache = new Dictionary<string, List<MarkerTriangle>>();
        private static float cachedSize = -1;
#if REGRESSION_TESTS
        internal static int BuildCount;
#endif
        internal static List<MarkerTriangle> Get(string id, float size)
        {
            if (cachedSize != size) { cache.Clear(); cachedSize = size; }
            int index = Array.IndexOf(ids, id);
            string key = index < 0 ? "unknown" : id;
            List<MarkerTriangle> result;
            if (cache.TryGetValue(key, out result)) return result;
            result = Build(index, size);
            cache.Add(key, result);
            return result;
        }
        private static Color Rgb(int r, int g, int b) { return new Color(r/255f, g/255f, b/255f, 1); }
        private static List<MarkerTriangle> Build(int index, float size)
        {
#if REGRESSION_TESTS
            BuildCount++;
#endif
            var mesh = new List<MarkerTriangle>();
            Polygon(mesh, size, pale, 16,4,84,4,96,16,96,84,84,96,16,96,4,84,4,16);
            Polygon(mesh, size, dark, 18,9,82,9,91,18,91,82,82,91,18,91,9,82,9,18);
            Color color = index < 0 ? pale : colors[index];
            switch (index)
            {
                case 0:
                    Polygon(mesh,size,color,50,17,61,39,83,50,61,61,50,83,39,61,17,50,39,39);
                    Polygon(mesh,size,Rgb(255,242,186),50,31,55,45,69,50,55,55,50,69,45,55,31,50,45,45); break;
                case 1:
                    Polygon(mesh,size,color,50,17,81,48,57,82,62,57,44,52,51,34,39,43,23,48);
                    Polygon(mesh,size,color,21,55,38,50,43,64,50,59,46,82); break;
                case 2:
                    Line(mesh,size,color,9,50,79,50,44);
                    Polygon(mesh,size,color,32,81,68,81,61,72,39,72);
                    Polygon(mesh,size,color,43,35,47,32,53,32,57,35,57,43,53,46,47,46,43,43);
                    Line(mesh,size,color,7,34,28,26,40,26,53); Line(mesh,size,color,7,66,28,74,40,74,53);
                    Line(mesh,size,color,5,24,18,15,36,15,58); Line(mesh,size,color,5,76,18,85,36,85,58); break;
                case 3:
                    Rect(mesh,size,color,20,19,80,28);
                    Polygon(mesh,size,color,50,34,76,59,60,59,60,82,40,82,40,59,24,59); break;
                case 4:
                    Rect(mesh,size,color,19,20,28,80);
                    Polygon(mesh,size,color,34,50,59,24,59,40,82,40,82,60,59,60,59,76); break;
                case 5: Polygon(mesh,size,color,50,15,75,15,58,43,79,43,36,86,43,57,23,57); break;
                case 6: Polygon(mesh,size,color,20,23,37,23,37,39,53,39,53,55,68,55,68,68,81,68,65,85,49,68,60,68,60,63,45,63,45,47,29,47,29,31,20,31); break;
                case 7:
                    Rect(mesh,size,color,29,29,71,71); Rect(mesh,size,dark,35,35,65,65);
                    foreach (int t in new[] {36,50,64})
                    {
                        Line(mesh,size,color,5,t,19,t,27); Line(mesh,size,color,5,t,73,t,81);
                        Line(mesh,size,color,5,19,t,27,t); Line(mesh,size,color,5,73,t,81,t);
                    }
                    Polygon(mesh,size,color,50,36,63,50,55,50,55,64,45,64,45,50,37,50); break;
                case 8:
                    Line(mesh,size,color,9,30,32,44,23,63,25,76,38,77,57);
                    Polygon(mesh,size,color,65,50,86,50,77,69);
                    Line(mesh,size,color,9,70,72,56,80,37,77,24,64,23,45);
                    Polygon(mesh,size,color,14,50,35,50,23,31); Rect(mesh,size,color,37,46,63,54); break;
                default:
                    Line(mesh,size,color,9,31,32,40,23,60,23,69,32,69,43,50,55,50,64);
                    Rect(mesh,size,color,46,74,54,82); break;
            }
            return mesh;
        }
        private static void Rect(List<MarkerTriangle> m, float s, Color c, float x0,float y0,float x1,float y1)
        { Polygon(m,s,c,x0,y0,x1,y0,x1,y1,x0,y1); }
        private static void Line(List<MarkerTriangle> m, float s, Color c, float width, params float[] xy)
        {
            for (int i=0; i+3<xy.Length; i+=2)
            {
                float dx=xy[i+2]-xy[i], dy=xy[i+3]-xy[i+1];
                float length=(float)Math.Sqrt(dx*dx+dy*dy);
                if (length==0) continue;
                float nx=-dy/length*width/2, ny=dx/length*width/2;
                Polygon(m,s,c,xy[i]+nx,xy[i+1]+ny,xy[i+2]+nx,xy[i+3]+ny,
                    xy[i+2]-nx,xy[i+3]-ny,xy[i]-nx,xy[i+1]-ny);
            }
        }
        // Ear clipping preserves concave silhouettes (lightning, arrows, cracked diamond).
        private static void Polygon(List<MarkerTriangle> m, float size, Color color, params float[] xy)
        {
            var points=new List<Vector2>();
            for(int i=0;i<xy.Length;i+=2) points.Add(new Vector2(xy[i],xy[i+1]));
            float area=0;
            for(int i=0;i<points.Count;i++) { var a=points[i]; var b=points[(i+1)%points.Count]; area+=a.x*b.y-b.x*a.y; }
            if(area<0) points.Reverse();
            while(points.Count>=3)
            {
                bool clipped=false;
                for(int i=0;i<points.Count;i++)
                {
                    int before=(i+points.Count-1)%points.Count, after=(i+1)%points.Count;
                    Vector2 a=points[before], b=points[i], c=points[after];
                    if(Cross(a,b,c)<=0) continue;
                    bool contains=false;
                    for(int j=0;j<points.Count;j++)
                        if(j!=before && j!=i && j!=after && Cross(a,b,points[j])>=0 && Cross(b,c,points[j])>=0 && Cross(c,a,points[j])>=0) { contains=true; break; }
                    if(contains) continue;
                    m.Add(new MarkerTriangle(Scale(a,size),Scale(b,size),Scale(c,size),color));
                    points.RemoveAt(i); clipped=true; break;
                }
                if(!clipped) throw new InvalidOperationException("Invalid module affix icon polygon.");
            }
        }
        private static float Cross(Vector2 a,Vector2 b,Vector2 c) { return (b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x); }
        private static Vector2 Scale(Vector2 p,float s) { return new Vector2(p.x*s/100,-p.y*s/100); }
    }
}
