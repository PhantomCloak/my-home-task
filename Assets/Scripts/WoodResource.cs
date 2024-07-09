using System.Collections;
using UnityEngine;

public class WoodResource : MonoBehaviour
{
    private static WoodResource s_CurrentHighlight;

    [SerializeField]
    private Material m_DefaultMat;

    [SerializeField]
    private Material m_HighlightMat;

    [SerializeField]
    private Material m_HighlightMatOther;

    [SerializeField]
    private MeshRenderer[] ChildMeshRenderers = new MeshRenderer[3];

    private IEnumerator Start() {
		yield return new WaitForEndOfFrame(); // Little trick to call after photon-view
		PlayerInitialSetup.Instance.AddWood(this);
	}

    public void Select()
    {
        if (s_CurrentHighlight)
        {
            Highlight(s_CurrentHighlight, false);
        }

        Highlight(this, true);
    }

    public void DeSelect()
    {
		Highlight(s_CurrentHighlight, false);
    }

	public void SelectOther() {
		HighlightOther(this, true);
	}

	public void DeSelectOther() {
		HighlightOther(this, false);
	}

    private void Highlight(WoodResource resource, bool enable)
    {
        foreach (var child in resource.ChildMeshRenderers)
            child.material = enable ? m_HighlightMat : m_DefaultMat;
        s_CurrentHighlight = this;
    }

    private void HighlightOther(WoodResource resource, bool enable)
	{
        foreach (var child in resource.ChildMeshRenderers)
            child.material = enable ? m_HighlightMatOther : m_DefaultMat;
	}
}
