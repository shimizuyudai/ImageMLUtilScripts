using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

public class ImagePreProcessor : MonoBehaviour
{
    private const int textureSize = 256;
    [SerializeField]
    Camera captureCamera, viewCamera;
    RenderTexture rt;
    [SerializeField]
    Renderer renderer;
    Texture2D texture, resultTexture;
    [SerializeField]
    string[] targetExtensions;
    [SerializeField]
    bool playOnStart;

    [SerializeField]
    KeyCode allowKey, denyKey;

    [SerializeField]
    KeyCode previousKey;

    [SerializeField]
    string[] loadDirectories;
    [SerializeField]
    string saveDirectory;

    List<string> files;

    bool isWaiting;
    [SerializeField]
    InputManager inputManager;
    [SerializeField]
    float scalingRate;

    float scaleRate = 1f;
    Vector3 localScale;

    [SerializeField]
    int offsetIndex;
    [SerializeField]
    SearchOption searchOption;

    bool isAllow;
    int index,count;

    Dictionary<int, int> dictionary;
    // Start is called before the first frame update
    void Start()
    {
        dictionary = new Dictionary<int, int>();
        texture = new Texture2D(1, 1);
        captureCamera.orthographic = true;
        captureCamera.orthographicSize = 0.5f;
        viewCamera.orthographic = true;
        viewCamera.orthographicSize = 0.5f;

        rt = new RenderTexture(textureSize, textureSize, 24);
        resultTexture = new Texture2D(rt.width, rt.height);
        captureCamera.targetTexture = rt;

        files = new List<string>();
        foreach (var d in loadDirectories)
        {
            var fs = Directory.GetFiles(d,"*",searchOption);
            fs = fs.Where(e => targetExtensions.Contains(Path.GetExtension(e))).ToArray();
            files.AddRange(fs);
        }

        StartCoroutine(ProcessRoutine());
    }

    // Update is called once per frame
    void Update()
    {
        if (!isWaiting) return;

        var pos = renderer.transform.localPosition;
        if (Input.GetMouseButton(0))
        {
            pos += inputManager.WorldMouseMove;
        }
        scaleRate += Input.mouseScrollDelta.y * scalingRate * Time.deltaTime;
        scaleRate = Mathf.Max(1f, scaleRate);
        renderer.transform.localScale = localScale * scaleRate;
        pos.x = Mathf.Clamp(pos.x, -(renderer.transform.localScale.x - 1) / 2f, (renderer.transform.localScale.x - 1) / 2f);
        pos.y = Mathf.Clamp(pos.y, -(renderer.transform.localScale.y - 1) / 2f, (renderer.transform.localScale.y - 1) / 2f);
        renderer.transform.localPosition = pos;


        var isPressedKey = false;
        if (Input.GetKeyDown(previousKey))
        {
            index-=2;
            index = Mathf.Clamp(index, 0, files.Count - 1);
            isAllow = false;
            isPressedKey = true;
        }
        if (Input.GetKeyDown(allowKey))
        {
            isAllow = true;
            isPressedKey = true;
        }
        else if (Input.GetKeyDown(denyKey))
        {
            isAllow = false;
            isPressedKey = true;
        }

        if (isPressedKey)
        {
            isWaiting = false;
        }
    }

    IEnumerator ProcessRoutine()
    {
        while (index < files.Count)
        {
            var bytes = File.ReadAllBytes(files[index]);
            index++;
            texture.LoadImage(bytes);
            texture.Apply();
            renderer.transform.localPosition = Vector3.zero;
            renderer.material.mainTexture = texture;
            var size = EMath.GetNormalizedExpandAspect(new Vector2(texture.width, texture.height));
            renderer.transform.localScale = new Vector3(size.x, size.y, 1f);
            localScale = renderer.transform.localScale;
            scaleRate = 1f;
            isWaiting = true;
            while (isWaiting)
            {
                yield return null;
            }
            if (!isAllow) continue;

            captureCamera.Render();
            yield return null;

            TextureUtils.RenderTexture2Texture2D(rt, resultTexture);
            var resultBytes = resultTexture.EncodeToJPG();
            var c = count;
            if (dictionary.ContainsKey(index))
            {
                c = dictionary[index];
            }
            else
            {
                count++;
                c = count;
                dictionary.Add(index,c);
            }
            var path = Path.Combine(saveDirectory, (offsetIndex + c) + ".jpg");
            File.WriteAllBytes(path, resultBytes);
            var progress = ((float)index / files.Count);
            print(progress);
            yield return null;
        }
        print("complete");
    }
}
