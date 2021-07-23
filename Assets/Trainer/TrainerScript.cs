using SimpleFileBrowser;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;
using System.Diagnostics;

public class TrainerScript : MonoBehaviour
{
    public TMP_InputField trainingPath;
    enum selection
    {
        NONE,
        TRAININGDATA
    }
    selection selected;
    void Start()
    {
        FileBrowser.AddQuickLink("StreamingAssets", Application.streamingAssetsPath, null);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnClickTrainingPath()
    {
        selected = selection.TRAININGDATA;

        StartCoroutine(ShowLoadDialogCoroutine());
    }

    IEnumerator ShowLoadDialogCoroutine()
    {
        print(FileBrowser.RequestPermission());

        // Show a load file dialog and wait for a response from user
        // Load file/folder: both, Allow multiple selection: true
        // Initial path: default (Documents), Initial filename: empty
        // Title: "Load File", Submit button text: "Load"
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.FilesAndFolders, true, null, null, "Load Files and Folders", "Load");

        // Dialog is closed
        // Print whether the user has selected some files/folders or cancelled the operation (FileBrowser.Success)
        print(FileBrowser.Success);

        if (FileBrowser.Success)
        {
            // Print paths of the selected files (FileBrowser.Result) (null, if FileBrowser.Success is false)
            for (int i = 0; i < FileBrowser.Result.Length; i++)
                print(FileBrowser.Result[i]);

            // Read the bytes of the first file via FileBrowserHelpers
            // Contrary to File.ReadAllBytes, this function works on Android 10+, as well
            byte[] bytes = FileBrowserHelpers.ReadBytesFromFile(FileBrowser.Result[0]);

            // Or, copy the first file to persistentDataPath
            string destinationPath = Path.Combine(Application.persistentDataPath, FileBrowserHelpers.GetFilename(FileBrowser.Result[0]));
            //AudioClip clip = (AudioClip) Resources.Load(FileBrowser.Result[0]);
            //ProcessAudioToText(clip);
            //ProcessAudioToText(FileBrowser.Result[0]);
            //textMeshPro.text = "TEST";
            switch (selected)
            {
                case selection.TRAININGDATA:
                    trainingPath.text = FileBrowser.Result[0];
                    break;
                default:
                    break;
            }
            FileBrowserHelpers.CopyFile(FileBrowser.Result[0], destinationPath);
        }
    }

    public void ProcessTraining()
    {
        var psi = new ProcessStartInfo();
        psi.FileName = @"C:\Users\bbaas\AppData\Local\Programs\Python\Python39\python.exe";
        var script = @".\Assets\StreamingAssets\DeepSpeech\bin\import_cv2.py";
        var path = @"E:\german-speechdata-package-v2\de";
        psi.Arguments = $"\"{script}\" \"{path}\" ";
        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;

        var errors = "";
        var results = "";
        using (var process = Process.Start(psi))
        {
            errors = process.StandardError.ReadToEnd();
            results = process.StandardOutput.ReadToEnd();
        }
        //textmesh.text = results;
        print(errors);
        print(results);
    }

}
