using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum Character : int {
	Red = 0,
	Green = 1,
	Yellow = 2
};

public class SelectionAreaMenu : MonoBehaviour
{
	[Header("UI")]
    [SerializeField]
    private Button m_SelectionButton1;

    [SerializeField]
    private Button m_SelectionButton2;

    [SerializeField]
    private Button m_SelectionButton3;

    [SerializeField]
    private Color m_SelectionColor;

    [SerializeField]
    private TMP_Text m_SelectionText;

    [SerializeField]
    private int m_CurrentSelection = -1;

	public bool HasCharacterSelected
    {
        get { return m_CurrentSelection != -1; }
    }

	public Character SelectedCharacter
    {
        get { return (Character)m_CurrentSelection; }
    }

	public static SelectionAreaMenu Instance;

	private void Awake() {
		Instance = this;
	}

    private void Update()
    {
        bool shouldBeInteractable = !MatchmakingMenu.Instance.IsMatchmakingInProgress;
        for (int i = 0; i < 3; i++)
        {
            var button = GetButtonByIndex(i);
            if (button.interactable != shouldBeInteractable)
            {
                button.interactable = shouldBeInteractable;
            }
        }
    }

    public void OnClickFind() { }

    public void OnClickSelect(int index)
    {
        m_CurrentSelection = m_CurrentSelection == index ? -1 : index;

        for (int i = 0; i < 3; i++)
        {
            var button = GetButtonByIndex(i);
            if (i != m_CurrentSelection)
                button.targetGraphic.color = Color.white;
            else
                button.targetGraphic.color = m_SelectionColor;
        }

        string strSelectionColor;
        switch (m_CurrentSelection)
        {
            case 0:
                strSelectionColor = "<color=red>Red</color>";
                break;
            case 1:
                strSelectionColor = "<color=green>Green</color>";
                break;
            case 2:
                strSelectionColor = "<color=yellow>Yellow</color>";
                break;
            default:
                strSelectionColor = string.Empty;
                break;
        }

        m_SelectionText.text = $"Pick Player First > {strSelectionColor}";
    }

    private Button GetButtonByIndex(int index)
    {
        switch (index)
        {
            case 0:
                return m_SelectionButton1;
            case 1:
                return m_SelectionButton2;
            case 2:
                return m_SelectionButton3;
            default:
                throw new Exception($"Button index doesn't exist {index}");
        }
    }
}
