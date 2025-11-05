using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ノベルゲームのシナリオデータを管理するクラス群
/// </summary>

/// [Serializable]と付けるとデータをjsonに変換したり、
/// jsonから復元したりできるようになる
[Serializable]
public class NovelScenario
{
    [Tooltip("シナリオ全体のリスト")]
    public List<DialogueData> dialogues = new List<DialogueData>();
}

[Serializable]
public class DialogueData
{
    [Tooltip("この台詞のID（管理用）")]
    public int id;

    [Tooltip("表示する背景画像のファイル名（例: \"background_school.png\"）")]
    public string backgroundImage;

    [Tooltip("表示するキャラクター画像のファイル名（例: \"character_girl.png\"）空文字で非表示")]
    public string characterImage;

    [Tooltip("キャラクターの表示位置（left, center, right）")]
    public string characterPosition = "center";

    [Tooltip("キャラクター名")]
    public string characterName;

    [Tooltip("表示する台詞")]
    public string dialogue;

    [Tooltip("次の台詞に進むまでの自動待機時間（秒）0なら手動クリック待ち")]
    public float autoWaitTime = 0f;

    [Tooltip("選択肢のリスト（空なら通常の台詞進行）")]
    public List<ChoiceData> choices = new List<ChoiceData>();

    [Tooltip("この台詞の後にロードするシーン名（空ならシーン遷移しない）")]
    public string nextSceneName = "";
}

[Serializable]
public class ChoiceData
{
    [Tooltip("選択肢のテキスト")]
    public string text;

    [Tooltip("この選択肢を選んだときにジャンプする台詞のID")]
    public int nextDialogueId;
}
