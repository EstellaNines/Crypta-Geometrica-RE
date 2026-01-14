using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Effects/UI Gradient")]
[RequireComponent(typeof(Graphic))]
public class UIGradient : BaseMeshEffect
{
    [SerializeField]
    private Color topColor = Color.white;
    
    [SerializeField]
    private Color bottomColor = Color.black;
    
    [SerializeField]
    private GradientDirection direction = GradientDirection.Vertical;
    
    [SerializeField]
    [Range(-1f, 1f)]
    private float offset = 0f;

    public enum GradientDirection
    {
        Vertical,
        Horizontal,
        DiagonalLeftToRight,
        DiagonalRightToLeft
    }

    public Color TopColor
    {
        get { return topColor; }
        set
        {
            topColor = value;
            graphic.SetVerticesDirty();
        }
    }

    public Color BottomColor
    {
        get { return bottomColor; }
        set
        {
            bottomColor = value;
            graphic.SetVerticesDirty();
        }
    }

    public GradientDirection Direction
    {
        get { return direction; }
        set
        {
            direction = value;
            graphic.SetVerticesDirty();
        }
    }

    public float Offset
    {
        get { return offset; }
        set
        {
            offset = Mathf.Clamp(value, -1f, 1f);
            graphic.SetVerticesDirty();
        }
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive())
            return;

        int vertexCount = vh.currentVertCount;
        if (vertexCount == 0)
            return;

        UIVertex vertex = new UIVertex();
        
        float minY = float.MaxValue;
        float maxY = float.MinValue;
        float minX = float.MaxValue;
        float maxX = float.MinValue;

        for (int i = 0; i < vertexCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            
            if (vertex.position.y < minY)
                minY = vertex.position.y;
            if (vertex.position.y > maxY)
                maxY = vertex.position.y;
            if (vertex.position.x < minX)
                minX = vertex.position.x;
            if (vertex.position.x > maxX)
                maxX = vertex.position.x;
        }

        float heightRange = maxY - minY;
        float widthRange = maxX - minX;

        for (int i = 0; i < vertexCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);

            float normalizedValue = 0f;

            switch (direction)
            {
                case GradientDirection.Vertical:
                    normalizedValue = (vertex.position.y - minY) / heightRange;
                    break;
                    
                case GradientDirection.Horizontal:
                    normalizedValue = (vertex.position.x - minX) / widthRange;
                    break;
                    
                case GradientDirection.DiagonalLeftToRight:
                    normalizedValue = ((vertex.position.x - minX) / widthRange + (vertex.position.y - minY) / heightRange) * 0.5f;
                    break;
                    
                case GradientDirection.DiagonalRightToLeft:
                    normalizedValue = ((maxX - vertex.position.x) / widthRange + (vertex.position.y - minY) / heightRange) * 0.5f;
                    break;
            }

            normalizedValue = Mathf.Clamp01(normalizedValue + offset);

            vertex.color = Color.Lerp(bottomColor, topColor, normalizedValue) * vertex.color;

            vh.SetUIVertex(vertex, i);
        }
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        if (graphic != null)
        {
            graphic.SetVerticesDirty();
        }
    }
#endif
}
