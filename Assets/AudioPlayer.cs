using System;
using System.Diagnostics;
using UnityEngine;
using static System.Net.Mime.MediaTypeNames;

public class AudioPlayer { 

    private AndroidJavaObject mediaPlayer;
    private AndroidJavaObject activity;
    private bool isPlaying = false;
 
    public AudioPlayer()
    {
        UnityEngine.Debug.Log("provo con l'oggetto java");
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            }
            UnityEngine.Debug.Log("Step 1");

            mediaPlayer = new AndroidJavaObject("android.media.MediaPlayer");
            UnityEngine.Debug.Log("Step 2");
        }
        catch (Exception e)
        {
            UnityEngine.Debug.Log("non ho creato l'oggetto java");
        }
    }

    public void PlayAudio(string path)
    {
        try
        {
            mediaPlayer.Call("reset");
            mediaPlayer.Call("setDataSource", activity, AndroidJavaObjectFromString(path));
            mediaPlayer.Call("prepare");
            mediaPlayer.Call("setLooping", true);
            mediaPlayer.Call("start");
            isPlaying = true;
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError("Errore nella riproduzione audio: " + e.Message);
        }
    }

    public void PauseAudio()
    {
        mediaPlayer.Call("pause");
    }

    public void ResumeAudio()
    {
        mediaPlayer.Call("start");
    }

    public void StopAudio()
    {
        mediaPlayer.Call("stop");
        isPlaying = false;
    }

    private AndroidJavaObject AndroidJavaObjectFromString(string path)
    {
        using (AndroidJavaClass uriClass = new AndroidJavaClass("android.net.Uri"))
        {
            return uriClass.CallStatic<AndroidJavaObject>("parse", path);
        }
    }

    public void Dispose()
    {
        if (mediaPlayer != null)
        {
            mediaPlayer.Call("release");
            mediaPlayer.Dispose();
        }
    }
}