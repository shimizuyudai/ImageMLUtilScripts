#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using UnityEditor;

public class DirectoryDivider : MonoBehaviour
{
    [SerializeField]
    string baseDirectoryPath;

    [SerializeField]
    DirecotryDivideInfo[] destDirectories;

    [SerializeField]
    string[] targetExtensions;

    [ContextMenu("Copy")]
    public void Copy()
    {
        Action<string, string> action = (string from, string to) =>
        {
            File.Copy(from, to);
        };
        print("called");
        Process(action);
    }

    [ContextMenu("Move")]
    public void Move()
    {
        Action<string, string> action = (string from, string to) =>
        {
            File.Move(from, to);
        };
        Process(action);
    }

    void Process(Action<string, string> action)
    {
        if (string.IsNullOrEmpty(baseDirectoryPath) || !Directory.Exists(baseDirectoryPath))
        {
            Debug.LogError("missing baseDirectory");
            return;
        }

        for (var i = 0; i < destDirectories.Length; i++)
        {
            if (string.IsNullOrEmpty(destDirectories[i].Path) || !Directory.Exists(destDirectories[i].Path))
            {
                Debug.LogError("missing destDirectory : " + i);
                return;
            }
        }
        AdjustRatio();
        var files = Directory.GetFiles(baseDirectoryPath);
        print(files.Length);
        for (var i = 0; i < destDirectories.Length; i++)
        {
            destDirectories[i].FileNUM = Mathf.FloorToInt((float)files.Length * destDirectories[i].Ratio);
            print(destDirectories[i].FileNUM);
        }

        files = EUtils.Shuffle(files);
        var directoryIndex = 0;
        var num = 0;
        for (var i = 0; i < files.Length; i++)
        {
            var extension = Path.GetExtension(files[i]);
            if (targetExtensions.Length > 0)
            {
                if (!targetExtensions.Contains(extension))
                {
                    continue;
                }
            }
            var directory = destDirectories[directoryIndex];
            action(files[i], Path.Combine(directory.Path, (num + 1) + extension));
            var progress = (float)i / files.Length;
            EditorUtility.DisplayCancelableProgressBar("Process", "processing...", progress);
            num++;
            if (num >= directory.FileNUM)
            {
                directoryIndex++;
                num = 0;
                if (directoryIndex >= destDirectories.Length)
                {
                    break;
                }
            }
        }
        EditorUtility.ClearProgressBar();
        print("complete");
    }

    [ContextMenu("AdjustRatio")]
    void AdjustRatio()
    {
        var total = destDirectories.Sum(e => e.Ratio);
        for (var i = 0; i < destDirectories.Length; i++)
        {
            destDirectories[i].Ratio = destDirectories[i].Ratio / total;
        }
    }

    [Serializable]
    public struct DirecotryDivideInfo
    {
        public string Path;
        [SerializeField]
        [Range(0, 1)]
        public float Ratio;
        [HideInInspector]
        public int FileNUM;
    }
}
#endif