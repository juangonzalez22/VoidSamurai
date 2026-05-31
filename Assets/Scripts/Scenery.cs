using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class Scenery : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioClip selected;

    private bool isLoading = false;

    public void LoadSceneLevel()
    {
        if (isLoading) return;
        StartCoroutine(LoadSceneWithSound());
    }

    private IEnumerator LoadSceneWithSound()
    {
        isLoading = true;

        if (sfxSource != null && selected != null)
        {
            sfxSource.PlayOneShot(selected);
        }

        yield return new WaitForSeconds(2f);

        SceneManager.LoadScene("Level");
    }
}