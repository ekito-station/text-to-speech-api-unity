using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Networking;
using System;
using System.Text;
using System.IO;

public class MainManager : MonoBehaviour
{
    private const string apiKey = ""; // OpenAI API keyを入力
    private const string apiUrl = "https://api.openai.com/v1/audio/speech";
    public TextMeshPro textInSpace;
    public TMP_InputField inputField;


    private void Awake()
    {
        textInSpace.text = "";
    }

    public void OnEnterButtonClicked()
    {
        textInSpace.text = "...";
        SynthesizeSpeech(inputField.text);
    }

    private void SynthesizeSpeech(string text)
    {
        StartCoroutine(SynthesizeSpeechCoroutine(text));
    }

    private IEnumerator SynthesizeSpeechCoroutine(string text)
    {
        // リクエストの準備
        var requestData = new TTSRequest
        {
            input = text
        };
        string jsonData = JsonUtility.ToJson(requestData);

        // UnityWebRequestの設定
        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            // ヘッダーの設定
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            // リクエストの送信
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // 一時ファイルとしてMP3を保存
                string tempPath = Path.Combine(Application.temporaryCachePath, "temp.mp3");
                File.WriteAllBytes(tempPath, request.downloadHandler.data);

                // MP3ファイルをロード
                using (UnityWebRequest audioRequest = UnityWebRequestMultimedia.GetAudioClip("file://" + tempPath, AudioType.MPEG))
                {
                    yield return audioRequest.SendWebRequest();

                    if (audioRequest.result == UnityWebRequest.Result.Success)
                    {
                        AudioClip audioClip = DownloadHandlerAudioClip.GetContent(audioRequest);
                        if (audioClip != null)
                        {
                            AudioSource audioSource = gameObject.AddComponent<AudioSource>();
                            audioSource.clip = audioClip;
                            audioSource.Play();

                            textInSpace.text = text;
                        }
                        else
                        {
                            Debug.LogError("Failed to convert audio data to AudioClip.");
                        }
                    }
                    else
                    {
                        Debug.LogError($"Audio loading error: {audioRequest.error}");
                    }
                }
                // 一時ファイルの削除
                try
                {
                    File.Delete(tempPath);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to delete temporary file: {e.Message}");
                }
            }
            else
            {
                Debug.LogError("Error: " + request.error);
            }
        }
    }

    [Serializable]
    private class TTSRequest
    {
        public string model = "tts-1";
        public string voice = "alloy";
        public string input;
    }
}
