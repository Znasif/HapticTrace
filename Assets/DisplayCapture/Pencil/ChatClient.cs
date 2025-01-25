using UnityEngine;
using TMPro;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using Whisper;
using Whisper.Utils;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Models;
using System.Threading.Tasks;

[RequireComponent(typeof(WhisperManager), typeof(AudioSource))]
public class ServerQueryUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI recognizedText;
    [SerializeField] private TextMeshProUGUI outputText;
    [SerializeField] private AudioSource audioSource;
    
    private MicrophoneRecord _microphone;
    private WhisperManager _whisper;
    private const string DefaultUrl = "http://localhost:8000/process";
    private string clientId;
    private bool isRecording = false;
    private const string MX_Ink_MiddleForce = "middle";
    private readonly List<Message> _messages = new();

    private const string ELEVENLABS_API_URL = "https://api.elevenlabs.io/v1/text-to-speech/";
    private const string VOICE_ID = "your_voice_id"; // Replace with your voice ID

    [Serializable]
    private class QueryData
    {
        public string text;
        public string type;
        public string system_prompt;
        public string client_id;
    }

    private void Awake()
    {
        _whisper = GetComponent<WhisperManager>();
        _microphone = gameObject.AddComponent<MicrophoneRecord>();
        _microphone.OnRecordStop += OnAudioRecorded;
        _whisper.language = "en";
        audioSource = GetComponent<AudioSource>();

        InitializeChat();
    }

    private void InitializeChat()
    {
        _messages.Add(new Message(
            Role.System,
            "You are an AI assistant helping users with their queries. Keep responses concise and clear."
        ));
    }

    private void Update()
    {
        float middleValue;
        if (OVRPlugin.GetActionStateFloat(MX_Ink_MiddleForce, out middleValue))
        {
            if (middleValue > 0 && !isRecording)
            {
                StartRecording();
            }
            else if (middleValue == 0 && isRecording)
            {
                StopRecording();
            }
        }
    }

    private void StartRecording()
    {
        _microphone.StartRecord();
        recognizedText.color = Color.red;
        isRecording = true;
    }

    private void StopRecording()
    {
        _microphone.StopRecord();
        recognizedText.color = Color.white;
        isRecording = false;
    }

    private async void OnAudioRecorded(AudioChunk audio)
    {
        var transcription = await _whisper.GetTextAsync(audio.Data, audio.Frequency, audio.Channels);
        recognizedText.text = transcription.Result;
        await ProcessQuery(transcription.Result);
    }

    private async Task ProcessQuery(string userInput)
    {
        _messages.Add(new Message(Role.User, userInput));
        
        var api = new OpenAIClient(new OpenAIAuthentication(""));
        var chatRequest = new ChatRequest(_messages, Model.GPT4);
        var response = await api.ChatEndpoint.GetCompletionAsync(chatRequest);
        var choice = response.FirstChoice;
        _messages.Add(choice.Message);
        
        outputText.text = choice.Message.Content.ToString();
        StartCoroutine(ConvertTextToSpeech(choice.Message.Content.ToString()));
    }

    private IEnumerator ConvertTextToSpeech(string text)
    {
        string requestBody = JsonUtility.ToJson(new { text = text });
        
        using (UnityWebRequest request = UnityWebRequest.PostWwwForm(ELEVENLABS_API_URL + VOICE_ID, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(requestBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerAudioClip("null", AudioType.MPEG);
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("xi-api-key", "asdasdasd");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                audioSource.clip = clip;
                audioSource.Play();
            }
            else
            {
                Debug.LogError($"TTS Error: {request.error}");
            }
        }
    }
}