using UnityEngine;

public class FPSController : MonoBehaviour {
    private const float updateInterval = 1.0f;
    private float accum; // FPS accumulated over the interval
    private float frames; // Frames drawn over the interval
    private float timeleft; // Left time for current interval
    private float fps = 15.0f; // Current FPS
    private float lastSample;
    private readonly GUIStyle textStyle = new();
    private void Start() {
        Application.targetFrameRate = 60;
        timeleft = updateInterval;
        lastSample = Time.realtimeSinceStartup;
        textStyle.fontStyle = FontStyle.Bold;
        textStyle.fontSize = 40;
        textStyle.normal.textColor = Color.green;
    } //Start ()_end

    private void Update() {
        ++ frames;
        float newSample = Time.realtimeSinceStartup;
        float deltaTime = newSample - lastSample;
        lastSample = newSample;
        timeleft -= deltaTime;
        accum += 1.0f / deltaTime;
        // Interval ended - update GUI text and start new interval
        if (!(timeleft <= 0.0f)) return;
        // display two fractional digits (f2 format)
        fps = accum / frames;
        // guiText.text = fps.ToString("f2");
        timeleft = updateInterval;
        accum = 0.0f;
        frames = 0;
    } //Update ()_end
    
    private void OnGUI() {
        
        GUI.Label(new Rect(0, 0, 200, 200), "FPS:" + fps.ToString("f2"), textStyle);
    }
}
