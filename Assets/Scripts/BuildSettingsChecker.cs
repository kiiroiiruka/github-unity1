#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

/// <summary>
/// Build Settings の状態を確認するエディタースクリプト
/// </summary>
public class BuildSettingsChecker : MonoBehaviour
{
    [ContextMenu("Build Settings をチェック")]
    public void CheckBuildSettings()
    {
        Debug.Log("=== Build Settings チェック開始 ===");
        
        // Build Settings に登録されているシーン数
        int sceneCount = EditorBuildSettings.scenes.Length;
        Debug.Log($"Build Settings に登録されているシーン数: {sceneCount}");
        
        if (sceneCount == 0)
        {
            Debug.LogWarning("Build Settings にシーンが1つも登録されていません！");
            Debug.LogWarning("File → Build Settings を開いて、SampleScene と Game シーンを追加してください。");
            return;
        }
        
        // 各シーンの詳細を表示
        for (int i = 0; i < sceneCount; i++)
        {
            var scene = EditorBuildSettings.scenes[i];
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scene.path);
            Debug.Log($"BuildScene[{i}] = {scene.path} (有効: {scene.enabled}, 名前: {sceneName})");
        }
        
        // Game シーンが含まれているかチェック
        bool hasGameScene = false;
        bool hasSampleScene = false;
        
        foreach (var scene in EditorBuildSettings.scenes)
        {
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scene.path);
            if (sceneName.Equals("Game", System.StringComparison.OrdinalIgnoreCase))
            {
                hasGameScene = true;
                Debug.Log("✓ Game シーンが見つかりました");
            }
            if (sceneName.Equals("SampleScene", System.StringComparison.OrdinalIgnoreCase))
            {
                hasSampleScene = true;
                Debug.Log("✓ SampleScene が見つかりました");
            }
        }
        
        if (!hasGameScene)
        {
            Debug.LogError("❌ Game シーンが Build Settings に登録されていません！");
            Debug.LogError("Assets/Scenes/Game.unity を Build Settings に追加してください。");
        }
        
        if (!hasSampleScene)
        {
            Debug.LogWarning("❌ SampleScene が Build Settings に登録されていません！");
        }
        
        Debug.Log("=== Build Settings チェック完了 ===");
    }
    
    [ContextMenu("Build Settings を自動修正")]
    public void AutoFixBuildSettings()
    {
        Debug.Log("=== Build Settings 自動修正開始 ===");
        
        // 現在のシーンリストを取得
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();
        
        // SampleScene を追加
        string sampleScenePath = "Assets/Scenes/SampleScene.unity";
        if (System.IO.File.Exists(sampleScenePath))
        {
            scenes.Add(new EditorBuildSettingsScene(sampleScenePath, true));
            Debug.Log($"✓ {sampleScenePath} を追加しました");
        }
        
        // Game シーンを追加
        string gameScenePath = "Assets/Scenes/Game.unity";
        if (System.IO.File.Exists(gameScenePath))
        {
            scenes.Add(new EditorBuildSettingsScene(gameScenePath, true));
            Debug.Log($"✓ {gameScenePath} を追加しました");
        }
        
        // Build Settings に適用
        EditorBuildSettings.scenes = scenes.ToArray();
        
        Debug.Log("=== Build Settings 自動修正完了 ===");
        Debug.Log("File → Build Settings を開いて確認してください。");
        
        // 確認のため再チェック
        CheckBuildSettings();
    }
}
#endif