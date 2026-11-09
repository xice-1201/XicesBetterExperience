using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace XicesBetterExperience
{
    public struct MarkerTriangle
    {
        public Vector2 A, B, C;
        public Color Color;
        public MarkerTriangle(Vector2 a, Vector2 b, Vector2 c, Color color)
        { A = a; B = b; C = c; Color = color; }
    }

    // A single UI mesh: no texture lookup, runtime code generation or extra material.
    public sealed class MarkerGraphic : MaskableGraphic
    {
        public readonly List<MarkerTriangle> Triangles = new List<MarkerTriangle>();
        public void UpdateGeometry(List<MarkerTriangle> triangles)
        {
            bool changed = Triangles.Count != triangles.Count;
            for (int i = 0; !changed && i < triangles.Count; i++)
            {
                MarkerTriangle a = Triangles[i], b = triangles[i];
                changed = a.A.x != b.A.x || a.A.y != b.A.y || a.B.x != b.B.x || a.B.y != b.B.y ||
                    a.C.x != b.C.x || a.C.y != b.C.y || a.Color.r != b.Color.r || a.Color.g != b.Color.g ||
                    a.Color.b != b.Color.b || a.Color.a != b.Color.a;
            }
            if (!changed) return;
            Triangles.Clear();
            Triangles.AddRange(triangles);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            foreach (MarkerTriangle triangle in Triangles)
            {
                int start = vh.currentVertCount;
                vh.AddVert(new Vector3(triangle.A.x, triangle.A.y, 0f), triangle.Color, Vector2.zero);
                vh.AddVert(new Vector3(triangle.B.x, triangle.B.y, 0f), triangle.Color, Vector2.zero);
                vh.AddVert(new Vector3(triangle.C.x, triangle.C.y, 0f), triangle.Color, Vector2.zero);
                vh.AddTriangle(start, start + 1, start + 2);
            }
        }

        internal static MarkerGraphic Attach(TreeNodeRender node)
        {
            node.image.enabled = false;
            node.image.raycastTarget = false;
            // The native node already owns an Image. Unity allows only one
            // Graphic per GameObject, even when that Image is disabled.
            GameObject child = new GameObject("Marker mesh", typeof(RectTransform));
            MarkerGraphic graphic;
            try
            {
                child.GetComponent<RectTransform>().SetParent(node.rectTransform, false);
                graphic = child.AddComponent<MarkerGraphic>();
                graphic.raycastTarget = false;
                graphic.rectTransform.anchorMin = Vector2.zero;
                graphic.rectTransform.anchorMax = new Vector2(1f, 1f);
                graphic.rectTransform.pivot = new Vector2(0f, 1f);
                graphic.rectTransform.sizeDelta = Vector2.zero;
                graphic.rectTransform.anchoredPosition = Vector2.zero;
            }
            catch
            {
                UnityEngine.Object.Destroy(child);
                UnityEngine.Object.Destroy(node.gameObject);
                throw;
            }
            node.rectTransform.anchorMin = node.rectTransform.anchorMax = new Vector2(0f, 1f);
            node.rectTransform.pivot = new Vector2(0f, 1f);
            node.rectTransform.anchoredPosition = Vector2.zero;
            return graphic;
        }
    }
}
