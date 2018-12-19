using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class DirectoryMerger : MonoBehaviour
{
    [SerializeField]
    string[] directories;
    [SerializeField]
    string resultDirectory;

    [SerializeField]
    string prefix;

    [ContextMenu("Copy")]
    public void Copy()
    {
        int count = 0;
        for (var i = 0; i < directories.Length; i++)
        {
            var files = Directory.GetFiles(directories[i]);
            for (var j = 0; j < files.Length; j++)
            {
                count++;
                File.Copy(files[j], Path.Combine(resultDirectory, prefix + count + Path.GetExtension(files[j])));
            }
        }
        print("complete");
    }

    [ContextMenu("Move")]
    public void Move()
    {
        int count = 0;
        for (var i = 0; i < directories.Length; i++)
        {
            var files = Directory.GetFiles(directories[i]);
            for (var j = 0; j < files.Length; j++)
            {
                count++;
                File.Move(files[j], Path.Combine(resultDirectory, prefix + count + Path.GetExtension(files[j])));
            }
        }
        print("complete");
    }
}
