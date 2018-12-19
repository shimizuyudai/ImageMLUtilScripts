using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Linq;

public class MaskRCNNExecutor : MonoBehaviour
{
    [SerializeField]
    string targetClasses = "BG, person, bicycle, car, motorcycle, airplane,bus, train, truck, boat, traffic light,fire hydrant, stop sign, parking meter, bench, bird,cat, dog, horse, sheep, cow, elephant, bear,zebra, giraffe, backpack, umbrella, handbag, tie,suitcase, frisbee, skis, snowboard, sports ball,kite, baseball bat, baseball glove, skateboard,surfboard, tennis racket, bottle, wine glass, cup,fork, knife, spoon, bowl, banana, apple,sandwich, orange, broccoli, carrot, hot dog, pizza,donut, cake, chair, couch, potted plant, bed,dining table, toilet, tv, laptop, mouse, remote,keyboard, cell phone, microwave, oven, toaster,sink, refrigerator, book, clock, vase, scissors,teddy bear, hair drier, toothbrush";
    [SerializeField]
    string tempporaryCSVPath;
    [SerializeField]
    string inputDirectory, outputDirecotry;
    [SerializeField]
    string scriptPath;
    [SerializeField]
    string temporaryBatchFilePath;//一時的にバッチファイルを保存するパス
    [SerializeField]
    string envName;//仮想環境名
    ProcessController controller;
    [SerializeField]
    string maskedImagesDirectory;
    [SerializeField]
    Color backgroundColor;
    [SerializeField]
    Shader maskingShader;
    [SerializeField]
    string[] targetExtensions;



    [ContextMenu("Adjust Classes")]
    void AdjustClasses()
    {
        var chars = targetClasses.ToCharArray();
        if (chars.Length < 2) return;
        targetClasses = string.Empty;
        for (var i = 0; i < chars.Length; i++)
        {
            if (i > 0 && chars[i] == ' ' && chars[i-1] == ',')
            {
            }
            else
            {
                targetClasses += chars[i];
            }
        }
    }

    void Clear()
    {
        if (controller != null)
        {
            controller.Dispose();
            controller = null;
        }
    }
    // Start is called before the first frame update
    void Start()
    {
    }

    [ContextMenu("Run")]
    void Run()
    {
        if (!Directory.Exists(inputDirectory))
        {
            print("missing inputDirectory");
        }
        if (!Directory.Exists(outputDirecotry))
        {
            Debug.LogError("missing outputdirectory");
            return;
        }

        Clear();
        var commands = new List<string>();
        if (!string.IsNullOrEmpty(envName))
        {
            commands.Add("activate " + envName);
        }
        commands.Add("cd " + Path.GetDirectoryName(scriptPath));
        File.WriteAllText(tempporaryCSVPath, targetClasses);
        commands.Add("python " + scriptPath + " " + 
            inputDirectory + " " + outputDirecotry + " " + tempporaryCSVPath);
        ProcessUtils.SaveCommandAsBatch(temporaryBatchFilePath, commands);
        ExecBatch();
    }

    private void ExecBatch()
    {
        if (!File.Exists(temporaryBatchFilePath))
        {
            print("missing batchFile");
            return;
        }
        var settings = new ProcessSettings();
        settings.FileName = temporaryBatchFilePath;
        settings.IsCommand = true;
        settings.WorkingDirectory = Path.GetDirectoryName(scriptPath);
        settings.IsEnableRaisingEvents = true;
        //settings.IsRedirectStandardOutput = true;
        controller = new ProcessController();
        controller.ExitProessEvent += Process_ExitProessEvent;
        controller.RedirectOutputEvent += Process_RedirectOutputEvent;
        var success = controller.Execute(settings);
    }

    private void Process_RedirectErrorEvent(ProcessMessage processMessage)
    {
        throw new NotImplementedException();
    }

    private void Process_ExitProessEvent(ProcessController controller)
    {
        print("complete process : " + controller.ProcessName);
        Clear();
    }

    private void Process_RedirectOutputEvent(ProcessMessage processMessage)
    {
        print(processMessage.Arguments);
    }

    [SerializeField]
    string temp, top;


    [ContextMenu("ApplyMask")]
    void ApplyMask()
    {
        if (!Directory.Exists(inputDirectory)) return;
        if (!Directory.Exists(outputDirecotry)) return;
        var material = new Material(maskingShader);
        var imgTex = new Texture2D(1, 1);
        var maskTex = new Texture2D(1, 1);
        var imgs = Directory.GetFiles(inputDirectory).Where(e => targetExtensions.Contains(Path.GetExtension(e))).ToArray();
        var masks = Directory.GetFiles(outputDirecotry).Where(e => targetExtensions.Contains(Path.GetExtension(e))).ToArray();

        foreach (var imgPath in imgs)
        {
            var maskPath = masks.FirstOrDefault(e => Path.GetFileName(e) == Path.GetFileName(e));
            if (maskPath == null) continue;
            print(maskPath);
            imgTex.LoadImage(File.ReadAllBytes(imgPath));
            maskTex.LoadImage(File.ReadAllBytes(maskPath));
            imgTex.Apply();
            maskTex.Apply();
            var rt = new RenderTexture(imgTex.width, imgTex.height,0);
            material.SetTexture("_MaskTex", maskTex);
            Graphics.Blit(imgTex, rt, material);
            var tex = TextureUtils.CreateTexture2DFromRenderTexture(rt);
            var bytes = tex.EncodeToJPG();
            var savePath = Path.Combine(maskedImagesDirectory, Path.GetFileName(imgPath));
            File.WriteAllBytes(savePath, bytes);
            DestroyImmediate(tex);
        }

        DestroyImmediate(material);
        DestroyImmediate(imgTex);
        DestroyImmediate(maskTex);
    }

}
