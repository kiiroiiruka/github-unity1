using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// ノベルゲーム全体を管理するマネージャークラス
/// </summary>
public class NovelGameManager : MonoBehaviour
{
    //＝＝＝↓unity側から設定する値↓＝＝＝//
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
    //＝＝＝↑unity側から設定する値↑＝＝＝//

    // 内部変数
    private NovelScenario currentScenario;//セリフデータ全体
    private int currentDialogueIndex = 0;//現在のセリフインデックス
    private bool isTextDisplaying = false;//テキストを表示中か？表示し終わっているか？の値
    private Coroutine textDisplayCoroutine;//　

    void Start()
    {
        LoadScenario();
        if (currentScenario != null && currentScenario.dialogues.Count > 0)
        {
            //jsonファイルの最初の台詞を表示
            DisplayDialogue(0);
        }
        else
        {
            Debug.LogError("シナリオデータが読み込めませんでした。");
        }
    }

    void Update()
    {
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
            //jsonファイル内のデータを読み込む
            string json = scenarioJsonFile.text;
            //JsonUtility.FromJson<T>(json);でデシリアライズ
            //※Tはデシリアライズ先のクラス名でjsonの中身が
            //そのクラスの型に変換された状態で格納できる。
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
        // インデックスが範囲外ならシナリオ終了
        if (currentScenario == null || index >= currentScenario.dialogues.Count)
        {
            Debug.Log("シナリオ終了");
            EndScenario();
            return;
        }

        // 現在の台詞インデックスを更新
        currentDialogueIndex = index;

        // dataに含まれるセリフデータ
        //含まれるデータ: 
        // 背景画像、
        // キャラ画像、
        // キャラ位置、
        // キャラ名、
        // 台詞文、
        // 自動待機時間
        DialogueData data = currentScenario.dialogues[index];//jsonの中身導入

        //背景データが存在するかチェック
        if (!string.IsNullOrEmpty(data.backgroundImage))
        {
            // 存在したら背景画像の変更
            LoadAndSetImage(data.backgroundImage, backgroundImage, backgroundResourcePath);
        }

        // キャラクターデータが存在するかチェック
        if (!string.IsNullOrEmpty(data.characterImage))
        {
            // 存在したらキャラクター画像の変更
            LoadAndSetImage(data.characterImage, characterImage, characterResourcePath);
            // キャラクター画像表示枠を有効化
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
            // 名前が空でなければ左右に角括弧を付ける
            if (!string.IsNullOrEmpty(data.characterName))
                characterNameText.text = $"[{data.characterName}]";
            else
                characterNameText.text = "";
        }

        // 台詞のテキスト表示（アニメーション付き）
        if (dialogueText == null)
        {
            Debug.LogError("dialogueText がインスペクタで割り当てられていません。テキストを表示できません。");
            return;
        }

        if (textDisplayCoroutine != null)
        {
            StopCoroutine(textDisplayCoroutine);
        }
        textDisplayCoroutine = StartCoroutine(DisplayTextAnimation(data.dialogue));

        // 自動進行の処理
        if (autoMode && data.autoWaitTime > 0)
        {
            StartCoroutine(AutoAdvance(data.autoWaitTime));
        }
    }

    /// <summary>
    /// 画像をResourcesから読み込んでImageコンポーネントにセット
    /// </summary>
    void LoadAndSetImage(string fileName, Image targetImage, string resourcePath)
    {
        // targetImageがnullなら何もしない
        if (targetImage == null) return;

        //データフォルダ場所を特定
        string fileNameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(fileName);// 拡張子を除去（Resourcesは拡張子なしで読み込む）
        string fullPath = $"{resourcePath}/{fileNameWithoutExt}";

        //特定したパスから画像を読み込み
        Sprite sprite = Resources.Load<Sprite>(fullPath);

        //画像があるか確認
        if (sprite != null)
        {
            // 画像が読み込めたらセット
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
        // テキスト表示中フラグを立てる
        isTextDisplaying = true;

        //そもそもセットしていない場合はエラーを出す
        if (dialogueText == null)
        {
            Debug.LogError("dialogueText が null です。インスペクタで割り当ててください。");
            isTextDisplaying = false;
            yield break;
        }

        // テキストを空にしてから表示開始   
        dialogueText.text = "";

        // fullText が null の場合は空表示にする
        if (string.IsNullOrEmpty(fullText))
        {
            isTextDisplaying = false;
            yield break;
        }

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
            if (dialogueText != null)
            {
                dialogueText.text = currentScenario.dialogues[currentDialogueIndex].dialogue;
            }
            else
            {
                Debug.LogError("dialogueText が null です。SkipTextAnimation でテキストを設定できません。");
            }
        }

        isTextDisplaying = false;
    }

    /// <summary>
    /// 次の台詞へ進む
    /// </summary>
    void NextDialogue()
    {
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
}
