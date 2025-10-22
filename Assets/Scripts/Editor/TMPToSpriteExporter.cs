// File: Assets/Editor/TMPToSpriteExporter.cs
// Place this script inside an "Editor" folder.
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using TMPro;

public class TMPToSpriteExporter
{
    [MenuItem("Tools/TMP/Export Letters as Sprites")]
    public static void ExportLetters()
    {
        // --- Config ---
        string outputFolder;
        TMP_FontAsset tmpFontAsset; // optionally assign a TMP Font Asset from Inspector by editing this file
        int textureSize = 512;          // resolution of each exported sprite (square)
        int padding = 8;               // extra transparent padding in pixels around glyph
        int fontRenderingSize = 256;    // TMP font size used for rendering (higher = crisper)
        char startChar = 'A';
        char endChar = 'Z';
        // -------------- end config

        string path = EditorUtility.OpenFilePanel("Select TMP Font Asset", "Assets", "asset");
        if (string.IsNullOrEmpty(path))
            return;

        path = "Assets" + path.Substring(Application.dataPath.Length);
        tmpFontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);

        outputFolder = "Assets/Fonts/LetterSprites/" + Path.GetFileNameWithoutExtension(path) + "/";

        if (!Directory.Exists(outputFolder))
            Directory.CreateDirectory(outputFolder);

        // Create a temporary scene so we don't pollute the current one (optional)
        var tempScene = EditorSceneManager.NewPreviewScene();

        // Create camera
        GameObject camGO = new GameObject("TMP_Export_Camera");
        Camera cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0, 0, 0, 0); // transparent
        cam.orthographic = true;
        cam.nearClipPlane = -10;
        cam.farClipPlane = 10;
        cam.cullingMask = ~0; // everything, but objects are isolated in this temp scene

        // Create TMP object (3D TextMeshPro)
        GameObject tmpGO = new GameObject("TMP_Export_Renderer");
        tmpGO.transform.position = Vector3.zero;
        TextMeshPro tmp = tmpGO.AddComponent<TextMeshPro>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.autoSizeTextContainer = true;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;

        // Apply font if provided (optional)
        if (tmpFontAsset != null)
            tmp.font = tmpFontAsset;
        else
        {
            // fallback: try to get default TMP font asset from project
            var all = AssetDatabase.FindAssets("t:TMP_FontAsset");
            if (all.Length > 0)
            {
                tmp.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(all[0]));
            }
        }

        if (tmp.font == null)
        {
            EditorUtility.DisplayDialog("TMP Exporter", "No TMP Font Asset found in project. Please add one and/or set tmpFontAsset in the script.", "OK");
            // Cleanup
            EditorSceneManager.ClosePreviewScene(tempScene);
            return;
        }

        tmp.fontSize = fontRenderingSize;
        tmp.ForceMeshUpdate();

        // Setup RenderTexture and texture2D
        RenderTexture rt = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 1;
        RenderTexture prevRT = RenderTexture.active;

        // Center the TMP object in view and size orthographic camera appropriately.
        // We'll measure TMP.bounds to determine orthographic size each char.
        for (char c = startChar; c <= endChar; c++)
        {
            tmp.text = c.ToString();
            tmp.ForceMeshUpdate();

            Bounds b = tmp.bounds;

            // If the glyph is empty (space or missing) skip or produce an empty image
            if (b.size == Vector3.zero)
            {
                Debug.LogWarning($"Glyph '{c}' produced zero bounds. Exporting blank sprite anyway.");
                b = new Bounds(Vector3.zero, Vector3.one * 0.01f);
            }

            // Compute orthographic size so the glyph fits nicely into the texture, with padding.
            float worldWidth = b.size.x;
            float worldHeight = b.size.y;
            float pixelToWorld = (float)textureSize / Mathf.Max(worldWidth, worldHeight + 0.0001f); // approximate
            // Adjust camera size: orthographic size is half of vertical size in world units
            float desiredWorldHeight = (textureSize - padding * 2) / pixelToWorld;
            cam.orthographicSize = desiredWorldHeight * 0.5f;

            // Position camera so the glyph is centered
            Vector3 center = b.center;
            cam.transform.position = new Vector3(center.x, center.y, -1f);
            cam.transform.rotation = Quaternion.identity;

            // Render to RT
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();

            // Save PNG
            byte[] png = tex.EncodeToPNG();
            string fileName = $"{outputFolder}/{c}.png";
            File.WriteAllBytes(fileName, png);
            Object.DestroyImmediate(tex);

            // Import as sprite
            AssetDatabase.ImportAsset(fileName);
            var importer = AssetImporter.GetAtPath(fileName) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.isReadable = false;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }

            Debug.Log($"Exported '{c}' -> {fileName}");
        }

        // Cleanup
        RenderTexture.active = prevRT;
        rt.Release();
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(camGO);
        Object.DestroyImmediate(tmpGO);

        AssetDatabase.Refresh();
        EditorSceneManager.ClosePreviewScene(tempScene);

        EditorUtility.DisplayDialog("TMP Exporter", $"Exported letters to {outputFolder}", "OK");
    }
}
