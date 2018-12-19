using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class FilteredTexturePackGenerator : MonoBehaviour
{
    private const int textureSize = 256;
    [SerializeField]
    string loadDirectoryPath, saveDirectoryPath;

    [SerializeField]
    Camera clampCamera, resultCamera;

    RenderTexture clampedTexture;
    RenderTexture resultRenderTexture;

    [SerializeField]
    Renderer baseImageRenderer, imageARenderer, imageBRenderer;
    [SerializeField]
    ImageFilterGroup filterGroup;

    Texture2D baseTexture, resultTexture;

    [SerializeField]
    string[] targetExtensions;

    [SerializeField]
    bool playOnStart;

    private void Awake()
    {
        
    }

    // Start is called before the first frame update
    void Start()
    {
        if (playOnStart)
        {
            Play();
        }
    }

    void Play()
    {
        baseTexture = new Texture2D(1, 1);
        clampCamera.orthographic = true;
        clampCamera.orthographicSize = 0.5f;
        resultCamera.orthographic = true;
        resultCamera.orthographicSize = 0.5f;

        clampedTexture = new RenderTexture(textureSize, textureSize, 24);
        resultRenderTexture = new RenderTexture(textureSize * 2, textureSize, 24);
        resultTexture = new Texture2D(resultRenderTexture.width, resultRenderTexture.height);
        resultCamera.targetTexture = resultRenderTexture;
        clampCamera.targetTexture = clampedTexture;
        StartCoroutine(GenerateRoutine());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator GenerateRoutine()
    {
        string[] files = Directory.GetFiles(loadDirectoryPath);
        files = files.Where(e => targetExtensions.Contains(Path.GetExtension(e))).ToArray();
        for (var i = 0; i < files.Length; i++)
        {
            var bytes = File.ReadAllBytes(files[i]);
            baseTexture.LoadImage(bytes);
            baseTexture.Apply();
            baseImageRenderer.material.mainTexture = baseTexture;
            var size = EMath.GetNormalizedExpandAspect(new Vector2(baseTexture.width, baseTexture.height));
            baseImageRenderer.transform.localScale = new Vector3(size.x, size.y,1f);
            clampCamera.Render();
            yield return null;
            //Aに画像をセット
            imageARenderer.material.mainTexture = clampedTexture;
            filterGroup.Filter(clampedTexture);
            yield return null;

            var filteredTexture = filterGroup.GetTexture();
            imageBRenderer.material.mainTexture = filteredTexture;
            resultCamera.Render();
            yield return null;
            TextureUtils.RenderTexture2Texture2D(resultRenderTexture, resultTexture);
            var resultBytes = resultTexture.EncodeToJPG();
            var path = Path.Combine(saveDirectoryPath, (i+1) + ".jpg");
            File.WriteAllBytes(path, resultBytes);
            var progress = ((float)i / files.Length);
            EditorUtility.DisplayProgressBar("Processing","processing...",progress);
            yield return null;
        }
        EditorUtility.ClearProgressBar();
        print("complete");
    }

    private void OnDestroy()
    {
        //DestroyImmediate(baseImageRenderer);
        //DestroyImmediate(resultRenderTexture);
        //DestroyImmediate(baseTexture);
        //DestroyImmediate(resultTexture);
        EditorUtility.ClearProgressBar();
    }
    
}
