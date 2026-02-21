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
    public float scrollSpeed = 0.3f;
    public float introDuration = 60f; 
    public float transitionDuration = 5f; // NEW: How long the camera sweep takes

    void Start()
    {
        // 1. Set the initial state
        introCamera.gameObject.SetActive(true);
        mainCamera.gameObject.SetActive(false);
        startUIContainer.SetActive(false);
        
        // 2. Play the music at full volume
        if (introAudio != null)
        {
            introAudio.volume = 1f; 
            introAudio.Play();
        }
        
        // 3. Start the sequence
        StartCoroutine(IntroSequence());
    }

    void Update()
    {
        // Scroll the text upward along its tilted local Y-axis
        if (textToScroll != null)
        {
            textToScroll.Translate(Vector3.up * scrollSpeed * Time.deltaTime);
        }
    }

    private IEnumerator IntroSequence()
    {
        // 1. Wait for the text to finish its crawl
        yield return new WaitForSeconds(introDuration);

        // 2. Hide the text canvas so it doesn't awkwardly clip the camera as we move
        introCanvas.SetActive(false);

        // 3. Set up the transition variables
        float elapsedTime = 0f;
        Vector3 startPos = introCamera.transform.position;
        Quaternion startRot = introCamera.transform.rotation;
        float startVolume = introAudio != null ? introAudio.volume : 1f;

        // 4. The Transition Loop (Camera Sweep + Audio Fade)
        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            
            // 't' goes from 0 to 1 over the duration of the transition
            float t = elapsedTime / transitionDuration;

            // Use SmoothStep to make the camera glide naturally (accelerate then decelerate)
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            // Move and rotate the Intro Camera towards the Main Camera's spot
            introCamera.transform.position = Vector3.Lerp(startPos, mainCamera.transform.position, smoothT);
            introCamera.transform.rotation = Quaternion.Lerp(startRot, mainCamera.transform.rotation, smoothT);

            // Fade the volume down to 0
            if (introAudio != null)
            {
                introAudio.volume = Mathf.Lerp(startVolume, 0f, t);
            }

            // Wait for the next frame
            yield return null; 
        }

        // 5. Hand over control to the main game
        SnapToGame();
    }

    private void SnapToGame()
    {
        // Shut off the audio completely
        if (introAudio != null)
        {
            introAudio.Stop();
        }

        // Swap the cameras so your Blackjack scripts take over
        introCamera.gameObject.SetActive(false);
        mainCamera.gameObject.SetActive(true);
        
        // Turn on the betting buttons
        startUIContainer.SetActive(true);
    }
}