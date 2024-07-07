using UnityEngine;

public class WoodResource : MonoBehaviour
{
    private static WoodResource s_CurrentHighlight;

    [SerializeField]
    private Material m_DefaultMat;

    [SerializeField]
    private Material m_HighlightMat;

    [SerializeField]
    private MeshRenderer[] ChildMeshRenderers = new MeshRenderer[3];

    private void Start() { }

    public void Select()
    {
        if (s_CurrentHighlight)
        {
            Highlight(s_CurrentHighlight, false);
        }

        Highlight(this, true);
    }

    public void DeSelect() { }

    private void Highlight(WoodResource resource, bool enable)
    {
        foreach (var child in resource.ChildMeshRenderers)
            child.material = enable ? m_HighlightMat : m_DefaultMat;
        s_CurrentHighlight = this;
    }
}
