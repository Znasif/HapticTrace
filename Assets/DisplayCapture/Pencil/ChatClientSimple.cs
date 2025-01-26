using UnityEngine;
using System;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.Networking;
using TMPro;
using System.Runtime.InteropServices;
using Anaglyph.DisplayCapture;

[Serializable]
public class Boundary
{
    public float left;
    public float right;
    public float top;
    public float bottom;
}

[Serializable]
public class Contour
{
    public float x;
    public float y;
}

[Serializable]
public class ResponseData
{
    public Boundary boundary;
    public Contour[] contours;
}

public class ChatClientSimple : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI requestText;
    [SerializeField] private TextMeshProUGUI responseText;
    [SerializeField] private GameObject canvas;
    public GameObject prefabToSpawn;
    public DisplayCaptureManager displayCaptureManager;

    private string clientId;
    //#if UNITY_ANDROID && !UNITY_EDITOR
    //       // Native Quest
    //       private const string DefaultUrl = "https://hack-vduz.onrender.com/process-image";
    //#else
    private const string DefaultUrl = "http://localhost:8000/process-image";
    //#endif
    private const string MX_Ink_MiddleForce = "middle";
    private bool isRecording = false;

    [Serializable]
    private class RequestData
    {
        public string text;
        public string type;
        public string system_prompt;
        public string client_id;
    }

    private void Start()
    {
        StartCoroutine(QueryServer());
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
        requestText.text = "Start of Recording...";
        requestText.color = Color.red;
        isRecording = true;
        //StartCoroutine(QueryServer());
    }

    private void StopRecording()
    {
        requestText.text = "Stopped Recording...";
        requestText.color = Color.white;
        isRecording = false;
    }

    public IEnumerator QueryServer(string url = DefaultUrl)
    {
        WWWForm form = new WWWForm();
        byte[] pngData = null;

        //#if UNITY_ANDROID && !UNITY_EDITOR
        //       // Native Quest
        //       pngData = displayCaptureManager.ScreenCaptureTexture.EncodeToPNG();
        //#else
        Color32[] pixels = new Color32[200 * 200];
        for (int y = 0; y < 200; y++)
        {
            for (int x = 0; x < 200; x++)
            {
                pixels[y * 200 + x] = new Color32(
                    (byte)(x > 10 && x < 20 ? 255 : 0),
                    (byte)(y > 10 && y < 20 ? 255 : 0),
                    0,
                    255
                );
            }
        }
        Texture2D tex = new Texture2D(200, 200);
        tex.SetPixels32(pixels);
        tex.Apply();
        pngData = tex.EncodeToPNG();
        //#endif

        form.AddBinaryData("file", pngData, "image.png", "image/png");

        using (UnityWebRequest request = UnityWebRequest.Post(url, form))
        {
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                ProcessSuccessfulResponse(request.downloadHandler.text);
            }
            else
            {
                HandleRequestError(request.error);
            }
        }

        // Cleanup
        Destroy(tex);
    }


    //public IEnumerator QueryServer(string text, string queryType = "text",
    //    string systemPrompt = null, string url = DefaultUrl)
    //{
    //    if (string.IsNullOrEmpty(text))
    //    {
    //        Debug.LogError("Text parameter cannot be empty");
    //        yield break;
    //    }

    //    var data = new RequestData
    //    {
    //        text = text,
    //        type = queryType,
    //        system_prompt = systemPrompt,
    //        client_id = clientId
    //    };

    //    string jsonBody = JsonUtility.ToJson(data);
    //    byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

    //    using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
    //    {
    //        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
    //        request.downloadHandler = new DownloadHandlerBuffer();
    //        request.SetRequestHeader("Content-Type", "application/json");

    //        yield return request.SendWebRequest();

    //        if (request.result == UnityWebRequest.Result.Success)
    //        {
    //            ProcessSuccessfulResponse(request.downloadHandler.text);
    //        }
    //        else
    //        {
    //            HandleRequestError(request.error);
    //        }
    //    }
    //}

    private void ProcessSuccessfulResponse(string responseText)
    {
        try
        {
            ResponseData responseData = JsonUtility.FromJson<ResponseData>(responseText);

            if (responseData == null)
            {
                Debug.LogError("Failed to parse response data");
                return;
            }

            this.responseText.text = responseText;

            if (responseData.contours != null)
            {
                SpawnContours(responseData.contours);
            }

            //Debug.Log($"Query successful. Response: {responseText}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error processing response: {ex.Message}");
        }
    }

    private void SpawnContours(Contour[] contours)
    {
        if (prefabToSpawn == null || canvas == null)
        {
            Debug.LogError("Prefab or canvas not assigned");
            return;
        }
        foreach (Contour contour in contours)
        {
            GameObject spawnedObject = Instantiate(prefabToSpawn, canvas.transform);
            spawnedObject.transform.localPosition = new Vector3(contour.x,
                                                              contour.y,
                                                              -50);
        }
    }

    private void HandleRequestError(string error)
    {
        string errorMessage = $"Request failed: {error}";
        Debug.LogError(errorMessage);
        responseText.text = errorMessage;
    }

    public void ProcessInput(string text, string queryType)
    {
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogError("Input text cannot be empty");
            return;
        }

        if (queryType != "contour" && queryType != "llm")
        {
            Debug.LogError("Query type must be 'contour' or 'llm'");
            responseText.text = "Invalid query type. Must be 'contour' or 'llm'";
            return;
        }

        string systemPrompt = queryType == "llm" ? "Create simple line art coordinates" : null;
        StartCoroutine(QueryServer());
    }
}