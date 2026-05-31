using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MatchResultHandler : MonoBehaviour
{
    [Header("Fighters")]
    public FighterCombat player1;
    public FighterCombat player2;

    [Header("Objects To Activate")]
    public TMP_Text resultText;
    public GameObject FinishPanel;

    private bool resultResolved;

    void Update()
    {
        if (resultResolved)
            return;

        if (player1 == null || player2 == null)
            return;

        bool player1Dead = player1.CurrentHealth <= 0;
        bool player2Dead = player2.CurrentHealth <= 0;

        if (player2Dead && !player1Dead)
        {
            resultResolved = true;
            OnPlayer1Win();
        }
        else if (player1Dead && !player2Dead)
        {
            resultResolved = true;
            OnPlayer1Lose();
        }
    }

    void OnPlayer1Win()
    {
        Debug.Log("PLAYER 1 WINS");
        resultText.text = "Samurai Wins!";
        ShowFinishPanel();
    }

    void OnPlayer1Lose()
    {
        Debug.Log("PLAYER 1 LOSES");
        resultText.text = "Void Wins!";
        ShowFinishPanel();

    }

    public void replayMatch()
    {
        SceneManager.LoadScene("Level");
    }

    // Corrutine to wait 2 seconds before activating the panel
    public void ShowFinishPanel()
    {
        StartCoroutine(ShowPanelAfterDelay());
    }

    private System.Collections.IEnumerator ShowPanelAfterDelay()
    {
        yield return new WaitForSeconds(2f);
        FinishPanel.SetActive(true);
    }
}