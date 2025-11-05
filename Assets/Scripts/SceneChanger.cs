using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// スタートボタンに付けるシーン切り替えスクリプト
/// </summary>
public class SceneChanger : MonoBehaviour
{
    [Header("遷移先のシーン設定")]
    [Tooltip("遷移先のシーン名を入力してください")]
    public string nextSceneName = "Game";

    /// <summary>
    /// ボタンを押したときに呼ばれるメソッド
    /// </summary>
    public void OnStartButtonClicked()
    {
        // シーン名が空でないかチェック
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogError("遷移先のシーン名が設定されていません！");
            return;
        }

        // Build Settings にあるかを確認（実行時に有効）
        int buildCount = SceneManager.sceneCountInBuildSettings;
        Debug.Log($"Build Settings に登録されているシーン数: {buildCount}");
        for (int i = 0; i < buildCount; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            Debug.Log($"BuildScene[{i}] = {path}");
        }

        // シーンがビルド設定に含まれているかチェック
        if (Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            Debug.Log($"シーン '{nextSceneName}' に遷移します (Build Settings 経由)");
            SceneManager.LoadScene(nextSceneName);
            return;
        }

#if UNITY_EDITOR
        // Editor実行時のフォールバック: Assets 内のシーンアセットを探して開く
        Debug.Log($"シーン '{nextSceneName}' が Build Settings に無いようです。Editor フォールバックを試みます...");
        string scenePath = FindScenePathInAssets(nextSceneName);
        if (!string.IsNullOrEmpty(scenePath))
        {
            Debug.Log($"[EditorFallback] シーンアセットを発見: {scenePath}。EditorSceneManager で開きます。");
            // Play Mode中はEditorSceneManagerを使用できないため、通常のSceneManagerを使用
            if (Application.isPlaying)
            {
                Debug.LogWarning($"Play Mode中はEditorSceneManagerが使用できません。通常のSceneManagerでロードを試みます: {nextSceneName}");
                SceneManager.LoadScene(nextSceneName);
            }
            else
            {
                // Editor Mode（Play中でない）の場合のみ保存確認を行う
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                }
                else
                {
                    Debug.LogWarning("現在のシーンの変更が保存されませんでした。シーン切り替えを中止しました。");
                }
            }
            return;
        }
#endif

        // ここまで来たら読み込み不可
        Debug.LogError($"シーン '{nextSceneName}' をロードできませんでした。Build Settings に追加されているか、Assets/Scenes にシーンファイルが存在するか確認してください。\n" +
                       "File → Build Settings でシーンを追加するか、プロジェクト内のシーン名を確認してください。");
    }

#if UNITY_EDITOR
    // Editor 実行時のみ有効: Assets の中からシーンファイルを探す
    private string FindScenePathInAssets(string sceneName)
    {
        // まずは名前で厳密検索
        string[] guids = AssetDatabase.FindAssets($"t:Scene {sceneName}");
        foreach (var g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == sceneName)
                return path;
        }

        // 次にすべてのシーンを列挙して部分一致（大文字小文字無視）
        guids = AssetDatabase.FindAssets("t:Scene");
        foreach (var g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            if (System.IO.Path.GetFileNameWithoutExtension(path).Equals(sceneName, System.StringComparison.OrdinalIgnoreCase))
                return path;
        }

        return null;
    }
#endif

    /// <summary>
    /// シーン番号でシーンを切り替えるメソッド
    /// </summary>
    /// <param name="sceneIndex">シーン番号</param>
    public void LoadSceneByIndex(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
    }

    /// <summary>
    /// シーン名でシーンを切り替えるメソッド
    /// </summary>
    /// <param name="sceneName">シーン名</param>
    public void LoadSceneByName(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// フェードアウト付きでシーンを切り替える場合はこちら
    /// (コルーチンを使った遅延読み込み)
    /// </summary>
    public void OnStartButtonClickedWithDelay()
    {
        StartCoroutine(LoadSceneWithDelay(nextSceneName, 1.0f));
    }

    private System.Collections.IEnumerator LoadSceneWithDelay(string sceneName, float delay)
    {
        // 指定秒数待機
        yield return new WaitForSeconds(delay);

        // シーンを切り替える
        SceneManager.LoadScene(sceneName);
    }
}
