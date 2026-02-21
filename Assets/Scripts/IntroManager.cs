using UnityEngine;
using System.Collections;
// Note: We no longer need UnityEngine.InputSystem since skipping is disabled!

public class IntroManager : MonoBehaviour
{
    [Header("Cameras")]
    public Camera introCamera;
    public Camera mainCamera;

    [Header("Intro Elements")]
    public RectTransform textToScroll;
    public GameObject introCanvas;

    [Header("Audio")]
    public AudioSource introAudio;
    
    [Header("Game UI")]
    public GameObject startUIContainer; 

    [Header("Settings")]
    private float scrollSpeed = 0.3f;
    private float introDuration = 74f; 
    private float transitionDuration = 5f;

    void Start()
    {
        introCamera.gameObject.SetActive(true);
        mainCamera.gameObject.SetActive(false);
        startUIContainer.SetActive(false);
        
        if (introAudio != null)
        {
            introAudio.volume = 1f; 
            introAudio.Play();
        }
        
        StartCoroutine(IntroSequence());
    }

    void Update()
    {
        if (textToScroll != null)
        {
            textToScroll.Translate(Vector3.up * scrollSpeed * Time.deltaTime);
        }
    }

    private IEnumerator IntroSequence()
    {
        yield return new WaitForSeconds(introDuration);

        introCanvas.SetActive(false);

        float elapsedTime = 0f;
        Vector3 startPos = introCamera.transform.position;
        Quaternion startRot = introCamera.transform.rotation;
        float startVolume = introAudio != null ? introAudio.volume : 1f;

        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            
            float t = elapsedTime / transitionDuration;

            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            introCamera.transform.position = Vector3.Lerp(startPos, mainCamera.transform.position, smoothT);
            introCamera.transform.rotation = Quaternion.Lerp(startRot, mainCamera.transform.rotation, smoothT);

            if (introAudio != null)
            {
                introAudio.volume = Mathf.Lerp(startVolume, 0f, t);
            }

            yield return null; 
        }

        SnapToGame();
    }

    private void SnapToGame()
    {
        if (introAudio != null)
        {
            introAudio.Stop();
        }

        introCamera.gameObject.SetActive(false);
        mainCamera.gameObject.SetActive(true);
        
        startUIContainer.SetActive(true);
    }
}