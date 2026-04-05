using UnityEngine;
using UnityEngine.UI;

public class FightHud : MonoBehaviour
{
    private Canvas canvas;
    private Text leftNameText;
    private Text rightNameText;
    private Text timerText;
    private Text roundText;
    private Text centerMessageText;
    private Image leftHealthFill;
    private Image rightHealthFill;
    private RectTransform leftHealthFillRect;
    private RectTransform rightHealthFillRect;
    private Image[] leftRoundWinIcons;
    private Image[] rightRoundWinIcons;
    private int maxHealth = 100;
    private int maxRoundWins = 3;
    private float leftHealthFillMaxWidth;
    private float rightHealthFillMaxWidth;

    public void Initialize(string leftName, string rightName, int healthMax, int roundWinsToWin)
    {
        maxHealth = Mathf.Max(1, healthMax);
        maxRoundWins = Mathf.Max(1, roundWinsToWin);
        EnsureHud();

        leftNameText.text = leftName;
        rightNameText.text = rightName;
        SetHealth(maxHealth, maxHealth);
        SetTimer(60);
        SetRound(1);
        SetRoundWins(0, 0);
        ShowCenterMessage(string.Empty, false);
    }

    public void SetHealth(int leftHealth, int rightHealth)
    {
        EnsureHud();

        SetHealthFillWidth(leftHealthFillRect, leftHealthFillMaxWidth, (float)leftHealth / maxHealth);
        SetHealthFillWidth(rightHealthFillRect, rightHealthFillMaxWidth, (float)rightHealth / maxHealth);
    }

    public void SetTimer(int seconds)
    {
        EnsureHud();
        timerText.text = Mathf.Max(0, seconds).ToString();
    }

    public void SetRound(int round)
    {
        EnsureHud();
        roundText.text = $"ROUND {round}";
    }

    public void ShowCenterMessage(string message, bool visible)
    {
        EnsureHud();
        centerMessageText.text = message;
        centerMessageText.gameObject.SetActive(visible);
    }

    public void SetRoundWins(int leftWins, int rightWins)
    {
        EnsureHud();

        UpdateRoundWinIcons(leftRoundWinIcons, leftWins);
        UpdateRoundWinIcons(rightRoundWinIcons, rightWins);
    }

    private void EnsureHud()
    {
        if (
            canvas != null &&
            leftNameText != null &&
            rightNameText != null &&
            timerText != null &&
            roundText != null &&
            centerMessageText != null &&
            leftHealthFill != null &&
            rightHealthFill != null &&
            leftHealthFillRect != null &&
            rightHealthFillRect != null
        )
        {
            return;
        }

        BuildHud();
    }

    private void BuildHud()
    {
        canvas = GetComponentInChildren<Canvas>();

        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Canvas");
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
        }
        else
        {
            for (int i = canvas.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(canvas.transform.GetChild(i).gameObject);
            }
        }

        Font font = LoadRuntimeFont();

        GameObject topRoot = CreateRect("TopRoot", canvas.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -20f), new Vector2(0f, 172f));

        GameObject leftPanel = CreateRect("LeftPanel", topRoot.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -8f), new Vector2(520f, 118f));
        GameObject rightPanel = CreateRect("RightPanel", topRoot.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-36f, -8f), new Vector2(520f, 118f));
        GameObject centerPanel = CreateRect("CenterPanel", topRoot.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(280f, 138f));

        leftNameText = CreateText("LeftName", leftPanel.transform, font, TextAnchor.UpperLeft, 34, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(440f, 40f));
        rightNameText = CreateText("RightName", rightPanel.transform, font, TextAnchor.UpperRight, 34, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(440f, 40f));

        CreateHealthBar(leftPanel.transform, true, out leftHealthFill, out leftHealthFillRect, out leftHealthFillMaxWidth);
        CreateHealthBar(rightPanel.transform, false, out rightHealthFill, out rightHealthFillRect, out rightHealthFillMaxWidth);
        leftRoundWinIcons = CreateRoundWinIcons(leftPanel.transform, true);
        rightRoundWinIcons = CreateRoundWinIcons(rightPanel.transform, false);

        timerText = CreateText("TimerText", centerPanel.transform, font, TextAnchor.MiddleCenter, 66, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(220f, 76f));
        roundText = CreateText("RoundText", centerPanel.transform, font, TextAnchor.MiddleCenter, 28, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -74f), new Vector2(220f, 36f));
        centerMessageText = CreateText("CenterMessage", canvas.transform, font, TextAnchor.MiddleCenter, 72, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 24f), new Vector2(860f, 120f));

        centerMessageText.color = new Color(1f, 0.9f, 0.35f);
        centerMessageText.gameObject.SetActive(false);
    }

    private void CreateHealthBar(
        Transform parent,
        bool isLeft,
        out Image fillImage,
        out RectTransform fillRect,
        out float fillMaxWidth
    )
    {
        GameObject barRoot = CreateRect(isLeft ? "LeftHealthBar" : "RightHealthBar", parent, isLeft ? new Vector2(0f, 1f) : new Vector2(1f, 1f), isLeft ? new Vector2(0f, 1f) : new Vector2(1f, 1f), new Vector2(0f, -54f), new Vector2(500f, 40f));
        Image background = barRoot.AddComponent<Image>();
        background.color = new Color(0.17f, 0.07f, 0.05f, 0.95f);

        GameObject fill = CreateRect(isLeft ? "LeftHealthFill" : "RightHealthFill", barRoot.transform, isLeft ? new Vector2(0f, 0.5f) : new Vector2(1f, 0.5f), isLeft ? new Vector2(0f, 0.5f) : new Vector2(1f, 0.5f), Vector2.zero, new Vector2(476f, 28f));
        fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(1f, 0.47f, 0.16f, 1f);
        fillRect = fill.GetComponent<RectTransform>();
        fillMaxWidth = 476f;

        GameObject frame = CreateRect(isLeft ? "LeftFrame" : "RightFrame", barRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500f, 40f));
        Image frameImage = frame.AddComponent<Image>();
        frameImage.color = new Color(1f, 0.85f, 0.45f, 0.2f);
        frameImage.raycastTarget = false;
    }

    private Image[] CreateRoundWinIcons(Transform parent, bool isLeft)
    {
        Image[] icons = new Image[maxRoundWins];
        GameObject root = CreateRect(
            isLeft ? "LeftRoundWins" : "RightRoundWins",
            parent,
            isLeft ? new Vector2(0f, 1f) : new Vector2(1f, 1f),
            isLeft ? new Vector2(0f, 1f) : new Vector2(1f, 1f),
            new Vector2(0f, -98f),
            new Vector2(132f, 24f)
        );

        for (int i = 0; i < maxRoundWins; i++)
        {
            float offsetX = isLeft ? i * 42f : -i * 42f;
            GameObject iconObject = CreateRect(
                (isLeft ? "LeftWin" : "RightWin") + i,
                root.transform,
                isLeft ? new Vector2(0f, 0.5f) : new Vector2(1f, 0.5f),
                isLeft ? new Vector2(0f, 0.5f) : new Vector2(1f, 0.5f),
                new Vector2(offsetX, 0f),
                new Vector2(24f, 24f)
            );

            Image icon = iconObject.AddComponent<Image>();
            icon.color = new Color(0.35f, 0.22f, 0.15f, 0.9f);
            icons[i] = icon;
        }

        return icons;
    }

    private GameObject CreateRect(
        string objectName,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 sizeDelta
    )
    {
        GameObject obj = new GameObject(objectName);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2((anchorMin.x + anchorMax.x) * 0.5f, (anchorMin.y + anchorMax.y) * 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        return obj;
    }

    private Text CreateText(
        string objectName,
        Transform parent,
        Font font,
        TextAnchor alignment,
        int fontSize,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 sizeDelta
    )
    {
        GameObject obj = CreateRect(objectName, parent, anchorMin, anchorMax, anchoredPosition, sizeDelta);
        Text text = obj.AddComponent<Text>();
        text.font = font;
        text.alignment = alignment;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private Font LoadRuntimeFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        return font;
    }

    private void UpdateRoundWinIcons(Image[] icons, int wins)
    {
        if (icons == null)
        {
            return;
        }

        for (int i = 0; i < icons.Length; i++)
        {
            icons[i].color = i < wins
                ? new Color(1f, 0.78f, 0.22f, 1f)
                : new Color(0.35f, 0.22f, 0.15f, 0.9f);
        }
    }

    private void SetHealthFillWidth(RectTransform fillRect, float maxWidth, float ratio)
    {
        if (fillRect == null)
        {
            return;
        }

        Vector2 size = fillRect.sizeDelta;
        size.x = Mathf.Max(0f, maxWidth * Mathf.Clamp01(ratio));
        fillRect.sizeDelta = size;
    }
}
