using System;
using System.Collections;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;

public class ClaudeService : MonoBehaviour
{
    public static ClaudeService Instance;

    private const string API_URL = "https://api.anthropic.com/v1/messages";
    private const string MODEL = "claude-haiku-4-5-20251001";

    private string GetApiKey()
    {
        TextAsset keyFile = Resources.Load<TextAsset>("APIConfig");
        return keyFile != null ? keyFile.text.Trim() : "";
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // destroy if not host — API calls only from host
        if (NetworkManager.Singleton != null &&
            !NetworkManager.Singleton.IsHost)
            Destroy(gameObject);
    }
    //TODO: delete the start method (only for testing)
    void Start()
    {
        // TEMP TEST — delete after confirming
        StartCoroutine(GetJudgeVerdict(
            "Player 2 is breaking the router",
            "Player 2 is at desk, progress 67%, not near router",
            result => Debug.Log($"[Claude] Verdict: {result}")
        ));
    }

    // ── Judge verdict ─────────────────────────────────────────
    public IEnumerator GetJudgeVerdict(string accusation, string playerContext, Action<bool> onResult)
    {
        string prompt =
            "You are a strict hackathon judge. Evaluate this accusation against " +
            "the accused player's actual game state. Be sceptical of dramatic claims. " +
            $"\n\nAccusation: \"{accusation}\"" +
            $"\n\nAccused player state: {playerContext}" +
            "\n\nRespond with ONLY valid JSON, nothing else: " +
            "{\"guilty\": true} or {\"guilty\": false}";

        yield return StartCoroutine(CallClaude(prompt, 60, raw =>
        {
            try
            {
                raw = raw.Replace("```json", "").Replace("```", "").Trim();
                var verdict = JsonUtility.FromJson<VerdictResponse>(raw);
                onResult(verdict.guilty);
            }
            catch
            {
                Debug.LogWarning("[Claude] Bad JSON: " + raw);
                onResult(false);
            }
        }));
    }

    // ── Stun roast ────────────────────────────────────────────
    public IEnumerator GetStunRoast(string playerAction, Action<string> onResult)
    {
        string prompt =
            $"You are a snarky hackathon judge. Player just did: '{playerAction}'. " +
            "Give a 1-sentence roast. Max 12 words. Be funny.";

        yield return StartCoroutine(CallClaude(prompt, 60, onResult));
    }

    // ── Bug snippet for code review ───────────────────────────
    public IEnumerator GetBugSnippet(Action<BugSnippet> onResult)
    {
        string prompt =
            "Generate a buggy JavaScript snippet (5-7 lines). " +
            "Return ONLY valid JSON, no markdown: " +
            "{\"prompt\":\"Find the bug:\",\"lines\":[\"line1\",\"line2\"]," +
            "\"bugLine\":0,\"explanation\":\"why it's wrong\"}";

        yield return StartCoroutine(CallClaude(prompt, 200, raw =>
        {
            try
            {
                raw = raw.Replace("```json", "").Replace("```", "").Trim();
                onResult(JsonUtility.FromJson<BugSnippet>(raw));
            }
            catch
            {
                Debug.LogWarning("[Claude] Bug snippet parse failed: " + raw);
                onResult(GetFallbackSnippet());
            }
        }));
    }

    // ── Core HTTP call ────────────────────────────────────────
    private IEnumerator CallClaude(string prompt, int maxTokens, Action<string> onResult)
    {
        var body = JsonUtility.ToJson(new ClaudeRequest
        {
            model = MODEL,
            max_tokens = maxTokens,
            messages = new[] { new Message { role = "user", content = prompt } }
        });

        var req = new UnityWebRequest(API_URL, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("x-api-key", GetApiKey());
        req.SetRequestHeader("anthropic-version", "2023-06-01");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var resp = JsonUtility.FromJson<ClaudeResponse>(req.downloadHandler.text);
            onResult(resp.content[0].text.Trim());
        }
        else
        {
            Debug.LogError("[Claude] API error: " + req.error);
            onResult("{\"guilty\": false}");
        }
    }

    // ── Fallback snippet if API fails ─────────────────────────
    BugSnippet GetFallbackSnippet()
    {
        return new BugSnippet
        {
            prompt = "Find the bug:",
            lines = new[] {
                "function sum(arr) {",
                "  let total = 0;",
                "  for (let i = 0; i <= arr.length; i++) {",
                "    total += arr[i];",
                "  }",
                "  return total;",
                "}"
            },
            bugLine = 2,
            explanation = "i <= arr.length reads undefined at the last index."
        };
    }

    // ── Serializable types ────────────────────────────────────
    [Serializable] class ClaudeRequest { public string model; public int max_tokens; public Message[] messages; }
    [Serializable] class Message { public string role, content; }
    [Serializable] class ClaudeResponse { public ContentBlock[] content; }
    [Serializable] class ContentBlock { public string type, text; }
    [Serializable] class VerdictResponse { public bool guilty; }
}

[Serializable]
public class BugSnippet
{
    public string prompt;
    public string[] lines;
    public int bugLine;
    public string explanation;
}