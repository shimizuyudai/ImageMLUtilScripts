using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Text;

public class Pix2PixExecutor : MonoBehaviour
{
    [Space(10)]
    [SerializeField]
    string temporaryBatchFilePath;//一時的にバッチファイルを保存するパス

    [Header("TRAIN")]
    [SerializeField]
    string baseDirectory;//pix2pixのディレクトリ
    [SerializeField]
    string inputDirectory;//トレーニング用画像群のディレクトリ（2枚セット）
    [SerializeField]
    string outputDirectory;//トレーニング結果を格納するディレクトリ
    [SerializeField]
    string envName;//仮想環境名

    [SerializeField]
    int maxEpochs;//最大訓練回数

    //訓練用画像の向き
    public enum Direction
    {
        AtoB,
        BtoA
    }
    [SerializeField]
    Direction direction;


    [Header("TEST MODEL")]
    [SerializeField]
    string testInputDirectory;//テスト用の画像群（2枚セット）
    [SerializeField]
    string testOutputDirectory;//テスト結果の格納用ディレクトリ

    [Header("EXPORT MODEL")]
    [SerializeField]
    string exportModelDirectory;//現在の学習モデルの出力先ディレクトリ

    [Header("SaveAsBinary")]
    [SerializeField]
    string exportScriptFilePath;//モデルをバイナリとして出力するためのスクリプトのパス
    [SerializeField]
    string modelFilePath;//バイナリの出力先パス

    ProcessController controller;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    void Clear()
    {
        if (controller != null)
        {
            controller.Dispose();
            controller = null;
        }
    }

    [ContextMenu("ExportModel")]
    void Export()
    {
        //if (!Directory.Exists(exportModelDirectory))
        //{
        //    Debug.LogError("missing directory");
        //    return;
        //}

        if (!Directory.Exists(baseDirectory))
        {
            Debug.LogError("missing directory");
            return;
        }

        Clear();
        var commands = new List<string>();
        if (!string.IsNullOrEmpty(envName))
        {
            commands.Add("activate " + envName);
        }
        commands.Add("cd " + baseDirectory);
        var exportPath = GetRelativePath(baseDirectory, exportModelDirectory);
        print(exportPath);
        commands.Add("python pix2pix.py --mode export --checkpoint " + outputDirectory + " --output_dir " + exportPath);
        ProcessUtils.SaveCommandAsBatch(temporaryBatchFilePath, commands);
        ExecBatch();
    }

    [ContextMenu("SaveModelAsBinary")]
    void SaveModelAsBinary()
    {
        if (!Directory.Exists(exportModelDirectory))
        {
            Debug.LogError("missing directory");
            return;
        }
        if (!File.Exists(exportScriptFilePath))
        {
            var path = Path.Combine(baseDirectory, exportScriptFilePath);
            if (!File.Exists(path))
            {
                Debug.LogError("missing file");
                return;
            }
        }

        Clear();
        var commands = new List<string>();
        if (!string.IsNullOrEmpty(envName))
        {
            commands.Add("activate " + envName);
        }
        commands.Add("cd " + baseDirectory);
        commands.Add("python " + exportScriptFilePath + " --checkpoint " + exportModelDirectory + " --output_file " + modelFilePath);
        ProcessUtils.SaveCommandAsBatch(temporaryBatchFilePath, commands);
        ExecBatch();
    }


    string GetRelativePath(string uri1, string uri2)
    {
        if(uri1.LastIndexOf("\\") != uri1.Length - 1){
            uri1 += "\\";
        }
        Uri u1 = new Uri(uri1);
        Uri u2 = new Uri(uri2);

        Uri relativeUri = u1.MakeRelativeUri(u2);

        return relativeUri.ToString();
    }



    [ContextMenu("Test")]
    void Test()
    {
        if (!Directory.Exists(outputDirectory))
        {
            print("missing outputDirectory");
        }
        if (!Directory.Exists(testOutputDirectory))
        {
            Debug.LogError("missing directory");
            return;
        }
        if (!Directory.Exists(testInputDirectory))
        {
            Debug.LogError("missing directory");
            return;
        }

        Clear();
        var commands = new List<string>();
        if (!string.IsNullOrEmpty(envName))
        {
            commands.Add("activate " + envName);
        }
        commands.Add("cd " + baseDirectory);

        commands.Add("python pix2pix.py   --mode test --output_dir " + testOutputDirectory +
            " --input_dir " + testInputDirectory + "  --checkpoint " + outputDirectory);
        ProcessUtils.SaveCommandAsBatch(temporaryBatchFilePath, commands);
        ExecBatch();
    }

    [ContextMenu("Train")]
    void Train()
    {
        Clear();
        if (!Directory.Exists(inputDirectory))
        {
            print("missing inputDirectory");
        }
        if (!Directory.Exists(outputDirectory))
        {
            print("missing outputDirectory");
        }
        if (!Directory.Exists(baseDirectory))
        {
            print("missing baseDirectory");
        }
        var commands = new List<string>();
        if (!string.IsNullOrEmpty(envName))
        {
            commands.Add("activate " + envName);
        }
        commands.Add("cd " + baseDirectory);
        var command = "python pix2pix.py --mode train --output_dir " + outputDirectory + " --max_epochs " +
            maxEpochs + " --input_dir " + inputDirectory + "  --which_direction " + direction;
        commands.Add(command);
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
        settings.WorkingDirectory = baseDirectory;
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
}
