using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// ノベルゲーム全体を管理するマネージャークラス
/// </summary>
public class NovelGameManager : MonoBehaviour
{
    [Header("JSONファイル設定")]
    [Tooltip("シナリオJSONファイル（ResourcesフォルダまたはStreamingAssetsフォルダに配置）")]
    public TextAsset scenarioJsonFile;

    [Header("UI要素の参照")]
    [Tooltip("背景画像を表示するImageコンポーネント")]
    public Image backgroundImage;

    [Tooltip("キャラクター画像を表示するImageコンポーネント")]
    public Image characterImage;

    [Tooltip("テキストボックス全体")]
    public GameObject textBoxPanel;

    [Tooltip("キャラクター名を表示するTextコンポーネント")]
    public Text characterNameText;

    [Tooltip("台詞を表示するTextコンポーネント")]
    public Text dialogueText;

    [Header("選択肢設定")]
    [Tooltip("選択肢ボタンを配置する親オブジェクト")]
    public GameObject choiceButtonContainer;

    [Tooltip("選択肢ボタンのプレハブ")]
    public GameObject choiceButtonPrefab;

    [Header("画像リソースフォルダ")]
    [Tooltip("背景画像が格納されているResourcesフォルダ内のパス（例: \"Backgrounds\"）")]
    public string backgroundResourcePath = "Backgrounds";

    [Tooltip("キャラクター画像が格納されているResourcesフォルダ内のパス（例: \"Characters\"）")]
    public string characterResourcePath = "Characters";

    [Header("テキスト表示設定")]
    [Tooltip("1文字表示する速度（秒）0なら一括表示")]
    public float textSpeed = 0.05f;

    [Tooltip("自動進行を有効にするか")]
    public bool autoMode = false;

    // 内部変数
    private NovelScenario currentScenario;
    private int currentDialogueIndex = 0;
    private bool isTextDisplaying = false;
    private bool isWaitingForChoice = false;
    private Coroutine textDisplayCoroutine;

    void Start()
    {
        LoadScenario();
        if (currentScenario != null && currentScenario.dialogues.Count > 0)
        {
            DisplayDialogue(0);
        }
        else
        {
            Debug.LogError("シナリオデータが読み込めませんでした。");
        }
    }

    void Update()
    {
        // 選択肢待機中はクリック進行を無効化
        if (isWaitingForChoice) return;

        // クリックまたはスペースキーで次へ進む
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            if (isTextDisplaying)
            {
                // テキスト表示中なら即座に全文表示
                SkipTextAnimation();
            }
            else
            {
                // 次の台詞へ
                NextDialogue();
            }
        }
    }

    /// <summary>
    /// JSONファイルからシナリオを読み込む
    /// </summary>
    void LoadScenario()
    {
        if (scenarioJsonFile == null)
        {
            Debug.LogError("scenarioJsonFileが設定されていません。");
            return;
        }

        try
        {
            string json = scenarioJsonFile.text;
            currentScenario = JsonUtility.FromJson<NovelScenario>(json);
            Debug.Log($"シナリオを読み込みました。台詞数: {currentScenario.dialogues.Count}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"JSONの読み込みに失敗しました: {e.Message}");
        }
    }

    /// <summary>
    /// 指定されたインデックスの台詞を表示
    /// </summary>
    void DisplayDialogue(int index)
    {
        if (currentScenario == null || index >= currentScenario.dialogues.Count)
        {
            Debug.Log("シナリオ終了");
            EndScenario();
            return;
        }

        currentDialogueIndex = index;
        DialogueData data = currentScenario.dialogues[index];

        // 背景画像の変更
        if (!string.IsNullOrEmpty(data.backgroundImage))
        {
            LoadAndSetImage(data.backgroundImage, backgroundImage, backgroundResourcePath);
        }

        // キャラクター画像の変更
        if (!string.IsNullOrEmpty(data.characterImage))
        {
            LoadAndSetImage(data.characterImage, characterImage, characterResourcePath);
            characterImage.gameObject.SetActive(true);

            // キャラクター位置の設定
            SetCharacterPosition(data.characterPosition);
        }
        else
        {
            // キャラクター画像を非表示
            characterImage.gameObject.SetActive(false);
        }

        // キャラクター名の表示
        if (characterNameText != null)
        {
            characterNameText.text = data.characterName;
        }

        // 台詞のテキスト表示（アニメーション付き）
        if (textDisplayCoroutine != null)
        {
            StopCoroutine(textDisplayCoroutine);
        }
        textDisplayCoroutine = StartCoroutine(DisplayTextAnimation(data.dialogue));

        // 選択肢がある場合は選択肢を表示
        if (data.choices != null && data.choices.Count > 0)
        {
            StartCoroutine(ShowChoicesAfterText(data.choices));
        }
        // 自動進行の処理
        else if (autoMode && data.autoWaitTime > 0)
        {
            StartCoroutine(AutoAdvance(data.autoWaitTime));
        }
    }

    /// <summary>
    /// 画像をResourcesから読み込んでImageコンポーネントにセット
    /// </summary>
    void LoadAndSetImage(string fileName, Image targetImage, string resourcePath)
    {
        if (targetImage == null) return;

        // 拡張子を除去（Resourcesは拡張子なしで読み込む）
        string fileNameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(fileName);
        string fullPath = $"{resourcePath}/{fileNameWithoutExt}";

        Sprite sprite = Resources.Load<Sprite>(fullPath);
        if (sprite != null)
        {
            targetImage.sprite = sprite;
            Debug.Log($"画像を読み込みました: {fullPath}");
        }
        else
        {
            Debug.LogWarning($"画像が見つかりません: {fullPath}");
        }
    }

    /// <summary>
    /// キャラクターの表示位置を設定
    /// </summary>
    void SetCharacterPosition(string position)
    {
        if (characterImage == null) return;

        RectTransform rectTransform = characterImage.GetComponent<RectTransform>();
        if (rectTransform == null) return;

        switch (position.ToLower())
        {
            case "left":
                rectTransform.anchoredPosition = new Vector2(-300, 0);
                break;
            case "center":
                rectTransform.anchoredPosition = new Vector2(0, 0);
                break;
            case "right":
                rectTransform.anchoredPosition = new Vector2(300, 0);
                break;
            default:
                rectTransform.anchoredPosition = new Vector2(0, 0);
                break;
        }
    }

    /// <summary>
    /// テキストを1文字ずつ表示するアニメーション
    /// </summary>
    IEnumerator DisplayTextAnimation(string fullText)
    {
        isTextDisplaying = true;
        dialogueText.text = "";

        if (textSpeed <= 0)
        {
            // 速度が0なら一括表示
            dialogueText.text = fullText;
        }
        else
        {
            // 1文字ずつ表示
            foreach (char c in fullText)
            {
                dialogueText.text += c;
                yield return new WaitForSeconds(textSpeed);
            }
        }

        isTextDisplaying = false;
    }

    /// <summary>
    /// テキストアニメーションをスキップして全文表示
    /// </summary>
    void SkipTextAnimation()
    {
        if (textDisplayCoroutine != null)
        {
            StopCoroutine(textDisplayCoroutine);
        }

        if (currentScenario != null && currentDialogueIndex < currentScenario.dialogues.Count)
        {
            dialogueText.text = currentScenario.dialogues[currentDialogueIndex].dialogue;
        }

        isTextDisplaying = false;
    }

    /// <summary>
    /// 次の台詞へ進む
    /// </summary>
    void NextDialogue()
    {
        // 現在の台詞にシーン遷移設定があるかチェック
        if (currentScenario != null && currentDialogueIndex < currentScenario.dialogues.Count)
        {
            DialogueData currentData = currentScenario.dialogues[currentDialogueIndex];
            if (!string.IsNullOrEmpty(currentData.nextSceneName))
            {
                LoadScene(currentData.nextSceneName);
                return;
            }
        }

        int nextIndex = currentDialogueIndex + 1;
        DisplayDialogue(nextIndex);
    }

    /// <summary>
    /// 自動進行の待機処理
    /// </summary>
    IEnumerator AutoAdvance(float waitTime)
    {
        // テキスト表示が終わるまで待つ
        while (isTextDisplaying)
        {
            yield return null;
        }

        // 指定時間待機
        yield return new WaitForSeconds(waitTime);

        // 次の台詞へ
        NextDialogue();
    }

    /// <summary>
    /// シナリオ終了時の処理
    /// </summary>
    void EndScenario()
    {
        Debug.Log("シナリオが終了しました。");
        // ここにシナリオ終了後の処理を追加（例: タイトルに戻る、など）
        if (textBoxPanel != null)
        {
            textBoxPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 自動モードの切り替え
    /// </summary>
    public void ToggleAutoMode()
    {
        autoMode = !autoMode;
        Debug.Log($"自動モード: {(autoMode ? "ON" : "OFF")}");
    }

    /// <summary>
    /// 特定の台詞にジャンプ
    /// </summary>
    public void JumpToDialogue(int index)
    {
        DisplayDialogue(index);
    }

    /// <summary>
    /// テキスト表示が終わるまで待ってから選択肢を表示
    /// </summary>
    IEnumerator ShowChoicesAfterText(List<ChoiceData> choices)
    {
        // テキスト表示が終わるまで待つ
        while (isTextDisplaying)
        {
            yield return null;
        }

        // 選択肢を表示
        ShowChoices(choices);
    }

    /// <summary>
    /// 選択肢ボタンを動的に生成して表示
    /// </summary>
    void ShowChoices(List<ChoiceData> choices)
    {
        if (choiceButtonContainer == null || choiceButtonPrefab == null)
        {
            Debug.LogError("choiceButtonContainerまたはchoiceButtonPrefabが設定されていません。");
            return;
        }

        // 選択肢待機状態にする
        isWaitingForChoice = true;

        // コンテナを表示
        choiceButtonContainer.SetActive(true);

        // 各選択肢のボタンを生成
        foreach (ChoiceData choice in choices)
        {
            GameObject buttonObj = Instantiate(choiceButtonPrefab, choiceButtonContainer.transform);
            Button button = buttonObj.GetComponent<Button>();
            Text buttonText = buttonObj.GetComponentInChildren<Text>();

            if (buttonText != null)
            {
                buttonText.text = choice.text;
            }

            if (button != null)
            {
                int nextId = choice.nextDialogueId;
                button.onClick.AddListener(() => OnChoiceSelected(nextId));
            }
        }
    }

    /// <summary>
    /// 選択肢が選ばれたときの処理
    /// </summary>
    void OnChoiceSelected(int nextDialogueId)
    {
        // 選択肢ボタンをクリア
        ClearChoices();

        // 選択肢待機状態を解除
        isWaitingForChoice = false;

        // 指定されたIDの台詞にジャンプ
        JumpToDialogueById(nextDialogueId);
    }

    /// <summary>
    /// 選択肢ボタンをすべて削除
    /// </summary>
    void ClearChoices()
    {
        if (choiceButtonContainer == null) return;

        // コンテナ内のすべての子オブジェクトを削除
        foreach (Transform child in choiceButtonContainer.transform)
        {
            Destroy(child.gameObject);
        }

        // コンテナを非表示
        choiceButtonContainer.SetActive(false);
    }

    /// <summary>
    /// IDで台詞を検索してジャンプ
    /// </summary>
    void JumpToDialogueById(int id)
    {
        if (currentScenario == null) return;

        for (int i = 0; i < currentScenario.dialogues.Count; i++)
        {
            if (currentScenario.dialogues[i].id == id)
            {
                DisplayDialogue(i);
                return;
            }
        }

        Debug.LogError($"ID {id} の台詞が見つかりません。");
    }

    /// <summary>
    /// 指定されたシーンをロード
    /// </summary>
    void LoadScene(string sceneName)
    {
        Debug.Log($"シーン '{sceneName}' に遷移します。");
        SceneManager.LoadScene(sceneName);
    }
}
