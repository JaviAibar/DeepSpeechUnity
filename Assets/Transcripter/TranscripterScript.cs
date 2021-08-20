using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SimpleFileBrowser;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System;

public class TranscripterScript : MonoBehaviour
{
    public TMP_InputField info;
    public TMP_Dropdown dropDownLanguages;
    public Image loading;
    public TMP_InputField audioPath;
    public TMP_InputField outputPath;
    public TMP_InputField outputFilename;
    public GameObject fakeVideoPanel;
    public GameObject advancedOptions;
    public TMP_InputField alphaField;
    public TMP_InputField betaField;
    public Toggle fakeVideoToggle;
    private readonly ConfigFile defaultOptions = new ConfigFile(0.0001f, 0.25f, false);

    private string audioRegex = @"\.(3gp|aa|aac|act|aiff|alac|amr|ape|au|awb|dss|dvf|flac|gsm|iklax|ivs|m4a|m4b|m4p|mmf|mp3|mpc|msv|mpc|msv|nmf|ogg|oga|mogg|opus|ra|rm|raw|rf64|sln|tta|voc|vox|wav|wma|wv|webm|8svx|cda)";
    Process cmd;
    // Engineer | https://stackoverflow.com/a/11794507
    string regexValidName = @"^[\w\-. ]+$";
    void Start()
    {
        InitializeCMD();
        LoadValues();
        Application.logMessageReceived += RedirectLogToInputField;
        audioPath.text = Application.streamingAssetsPath+"/Cari.wav";
        FileBrowser.AddQuickLink("StreamingAssets", Application.streamingAssetsPath, null);
        var info = new DirectoryInfo(Application.streamingAssetsPath + "/Languages");
        List<TMP_Dropdown.OptionData> languages = new List<TMP_Dropdown.OptionData>();
        foreach (FileInfo f in info.GetFiles())
        {
            if (f.Name.EndsWith("pbmm"))
            {
                TMP_Dropdown.OptionData option = new TMP_Dropdown.OptionData();
                string langname = f.Name.Split('.')[0];
                option.text = langname;
                if (File.Exists(Application.streamingAssetsPath + "/Languages/"+langname + ".scorer"))
                    languages.Add(option);
                else
                    PrintToOutput($"A model of {langname} language has been found but there's no scorer with the same name. Is the scorer missing or it have a different name?", true);
            }
        }
        dropDownLanguages.options = languages;
        dropDownLanguages.SetValueWithoutNotify(1);
        //textMeshPro.text = Python(@".\Assets\HelloWorld.py", "");
        //print(CMD("deepspeech", "--model "));
        //  System.Diagnostics.Process.Start("explorer.exe", "/select," + path);
        //     ProcessAudioToText(@"Cari.wav");
        //print(CMD(@".\deepspeech.exe --model .\Languages\German.pbmm --scorer .\Languages\German.scorer --audio .\Cari.wav"));
    }

    public void InitializeCMD()
    {
        cmd = new Process();
        cmd.StartInfo.FileName = "./python.exe";
        // cmd.StartInfo.RedirectStandardInput = true;
        cmd.StartInfo.RedirectStandardOutput = true;
        cmd.StartInfo.RedirectStandardError = true;
        cmd.StartInfo.CreateNoWindow = true;
        cmd.StartInfo.UseShellExecute = false;

        // svick | https://stackoverflow.com/a/10789196
        cmd.EnableRaisingEvents = true;
        cmd.Exited += (sender, args) =>
        {
            /*  tcs.SetResult(process.ExitCode);
              process.Dispose();*/
            PrintToOutput("Completed");
            // print("Completed"); 
            string errors = cmd.StandardError.ReadToEnd();
            print("Is error null? " + string.IsNullOrEmpty(errors));
            PrintToOutput("Is error null? " + string.IsNullOrEmpty(errors));

           // if (!string.IsNullOrEmpty(errors))
                PrintToOutput(errors);
            print(errors);
            string results = cmd.StandardOutput.ReadToEnd();
            PrintToOutput("Is results null? " + string.IsNullOrEmpty(results));
            print("Is results null? " + string.IsNullOrEmpty(results));
            PrintToOutput(results);
            print(results);
            loading.enabled = false;
            //   cmd.StandardInput.Close();
        };
        PrintToOutput("CMD initialized successfully");
    }
    public void ExplorerClick(string sender)
    {
        StartCoroutine(ShowLoadDialogCoroutine(sender));
    }

    IEnumerator ShowLoadDialogCoroutine(string sender)
    {
        // Show a load file dialog and wait for a response from user
        // Load file/folder: both, Allow multiple selection: true
        // Initial path: default (Documents), Initial filename: empty
        // Title: "Load File", Submit button text: "Load"
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.FilesAndFolders, true, null, null, "Load Files and Folders", "Load");

        // Dialog is closed
        // Print whether the user has selected some files/folders or cancelled the operation (FileBrowser.Success)

        if (FileBrowser.Success)
        {
            // Print paths of the selected files (FileBrowser.Result) (null, if FileBrowser.Success is false)
           // for (int i = 0; i < FileBrowser.Result.Length; i++)
            //    print(FileBrowser.Result[i]);

            // Read the bytes of the first file via FileBrowserHelpers
            // Contrary to File.ReadAllBytes, this function works on Android 10+, as well
            //byte[] bytes = FileBrowserHelpers.ReadBytesFromFile(FileBrowser.Result[0]);

            // Or, copy the first file to persistentDataPath
            //string destinationPath = Path.Combine(Application.persistentDataPath, FileBrowserHelpers.GetFilename(FileBrowser.Result[0]));
            //AudioClip clip = (AudioClip) Resources.Load(FileBrowser.Result[0]);

            //ProcessAudioToText($"\"{FileBrowser.Result[0]}\"");

            
            if (sender == "saveButton")
            {
                PrintLogToFile(info.text, FileBrowser.Result[0]);
            }
            else
            {
                // Kevin Aenmey | https://stackoverflow.com/a/11122523
                System.Reflection.FieldInfo field = GetType().GetField(sender);
                if (field == null) PrintToOutput("InputField not found", true);
                else
                {
                    TMP_InputField inputField = ((TMP_InputField)field.GetValue(this));
                    //print(Path.GetExtension(FileBrowser.Result[0]));
                    if (inputField.name.Contains("audio") && CheckRegex(Path.GetExtension(FileBrowser.Result[0]), audioRegex))
                    { //MimeMapping.MimeUtility.GetMimeMapping(FileBrowser.Result[0]) == "Audio"
                        fakeVideoPanel.SetActive(true);
                    } else
                    {
                        fakeVideoPanel.SetActive(false);
                    }
                    inputField.text = FileBrowser.Result[0];
                }
            }


            // FileBrowserHelpers.CopyFile(FileBrowser.Result[0], destinationPath);
        }
    }

    public void PrintLogToFile(string text, string path=".")
    {
        print("path: "+path+", text: "+text);
        File.WriteAllText($"\"{path}/log.txt\"", text);
        print($"\"{path}/log.txt\"");
        PrintToOutput("Successfully saved file at "+ (path != "." ? $"\"{path}/log.txt\"" : Application.dataPath+"/log.txt"));
    }

    public void ProcessAudioToText_Click()
    {

        if (string.IsNullOrEmpty(audioPath.text))
            PrintToOutput("Audio path not selected", true);
        else if (!File.Exists(audioPath.text))
        {
            PrintToOutput(""+File.Exists(audioPath.text));
            PrintToOutput(audioPath.text);
            PrintToOutput("Audio file not found", true);
        }
        else
            ProcessAudioToText();
    }

    void ProcessAudioToText()
    {
        loading.enabled = true;
        PrintToOutput(dropDownLanguages.itemText.text);
        float alpha = string.IsNullOrEmpty(alphaField.text) ? defaultOptions.alpha : float.Parse(alphaField.text);
        float beta = string.IsNullOrEmpty(betaField.text) ? defaultOptions.beta : float.Parse(betaField.text);
        string command = $"\"{Application.streamingAssetsPath}/Dependencies/client.py\"";
        command += " --extended";
        command += " --model " + $"\"{Application.streamingAssetsPath}/Languages/{dropDownLanguages.itemText.text}.pbmm\"";
        command += @" --scorer " + $"\"{Application.streamingAssetsPath}/Languages/German.scorer\"";
        command += " --audio " + $"\"{audioPath.text}\" --lm_alpha {alpha.ToString().Replace(",",".")} --lm_beta {beta.ToString().Replace(",", ".")}";
        command += (!string.IsNullOrEmpty(outputPath.text) ? (" --output " + $"\"{outputPath.text}\"") : "");
        command += (!string.IsNullOrEmpty(outputFilename.text) && CheckRegex(outputFilename.text, regexValidName) ? (" --srt_name " + outputFilename.text) : "");
        PrintToOutput("Executed: python " + command);
        cmd.StartInfo.Arguments = command;
        bool started = cmd.Start();
        if (started)
            PrintToOutput("Process successfully started");
        else
            PrintToOutput("Process could not be started", true);
        // cmd.StandardInput.WriteLine(@"deepspeech --model .\Languages\German.pbmm --scorer .\Languages\German.scorer --audio " + clip);
        //  cmd.StandardInput.Flush();
        if (fakeVideoPanel.activeSelf && fakeVideoToggle.isOn)
        {
            GenerateFakeVideo();
        }



        // cmd.WaitForExit(30000);

        //return results;
    }

   /* public string Python(string command)
    {
        var psi = new ProcessStartInfo();
        psi.FileName = @"python.exe";
        //var script = @".\Assets\HelloWorld.py";

        //psi.Arguments = $"\"{command}\"";
        psi.Arguments = $"\"{command}\"";
        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;

        var results = "";
        var errors = "";
        using (var process = Process.Start(psi))
        {
            errors = process.StandardError.ReadToEnd();
            if (!string.IsNullOrEmpty(errors))
                print(errors);
            results = process.StandardOutput.ReadToEnd();
        }
        // print(errors);
        return results;

    }*/

    public void GenerateFakeVideo()
    {

    }

    // Credits Ogglas https://stackoverflow.com/a/32872174
    /*public string CMD(string command)
    {
        var results = "";
        var errors = "";

        Process cmd = new Process();
        cmd.StartInfo.FileName = "cmd.exe";
        cmd.StartInfo.RedirectStandardInput = true;
        cmd.StartInfo.RedirectStandardOutput = true;
        cmd.StartInfo.RedirectStandardError = true;
        cmd.StartInfo.CreateNoWindow = true;
        cmd.StartInfo.UseShellExecute = false;
        cmd.Start();

        cmd.StandardInput.WriteLine("cd " + Application.streamingAssetsPath);
        cmd.StandardInput.WriteLine(command);
        cmd.StandardInput.Flush();
        cmd.StandardInput.Close();
        cmd.WaitForExit(10000);
        errors = cmd.StandardError.ReadToEnd();
        if (!string.IsNullOrEmpty(errors))
            print(errors);
        results = cmd.StandardOutput.ReadToEnd();
        //print("Aqui");
        return results;
    }*/

    public void PrintToOutput(string text, bool isError = false)
    {
        //print("printing to output " + text);
        info.text += "\n"  + text ;
        //info.text += "\n" + (isError ? "<b><color=\"red\">" : "") + text + (isError ? "</color></b>" : "");
    }


    public void Test()
    {
        PrintToOutput("Lorem ipsum dolor sit amet, consectetur adipiscing elit. Suspendisse non nunc quis eros sollicitudin euismod. Curabitur commodo neque tortor, in elementum mi pulvinar et. Curabitur porttitor lacus augue, in aliquam nibh tempor ac. Fusce sed consequat leo. Quisque elementum justo ac ullamcorper tempor. Donec at lacus dolor. Vivamus ut purus ligula. Etiam vel eros a orci pulvinar iaculis. Aenean scelerisque malesuada mauris quis lobortis. Ut placerat imperdiet ante non iaculis. Nunc vel varius urna, sit amet mattis lacus. Morbi vestibulum aliquet nisi eget molestie. Donec a lectus eget metus ornare mollis. Aliquam eget turpis at risus imperdiet tristique. Donec pellentesque nec urna at malesuada. Phasellus eget sodales libero.\n\nDuis porttitor id elit et volutpat.Vivamus justo neque, sollicitudin at pellentesque nec, auctor id ipsum.Morbi hendrerit nunc eget massa euismod tristique.Aenean lacinia pharetra mollis.Integer eget sapien tristique, ultricies tortor et, accumsan enim.Duis lacus turpis, rhoncus eu risus a, convallis ornare dui.Praesent molestie pellentesque nisi ut lacinia.\n");
        //PrintToOutput("TEST ", true);
            }

    public bool CheckRegex(string text, string regex)
    {
        Regex rgx = new Regex(regex);
        return rgx.IsMatch(text);
    }

    public void ClearConsole()
    {
        info.text = "";
    }

    public void ResetValuesToDefault()
    {
        // TODO: Are you sure? window
        alphaField.text = "" + defaultOptions.alpha;
        betaField.text = "" + defaultOptions.beta;
        advancedOptions.SetActive(false);
    }

    public void LoadValues()
    {
        string filename = "config.json";
        if (File.Exists(Application.persistentDataPath + "/" + filename))
        {
            string jsonString = File.ReadAllText(Application.persistentDataPath + "/" + filename);
            ConfigFile configFile = JsonUtility.FromJson<ConfigFile>(jsonString);
            alphaField.text = "" + configFile.alpha;
            betaField.text = "" + configFile.beta;
            advancedOptions.SetActive(configFile.advancedOptionsOpen);
        }
        else
        {
            ResetValuesToDefault();
        }
    }

    public void SaveValues()
    {
    // TODO: Are you sure? window
        string filename = "config.json";
        ConfigFile configFile = new ConfigFile();
        configFile.alpha = string.IsNullOrEmpty(alphaField.text) ? defaultOptions.alpha : float.Parse(alphaField.text);
        configFile.beta = string.IsNullOrEmpty(betaField.text) ? defaultOptions.beta : float.Parse(betaField.text);
        configFile.advancedOptionsOpen = advancedOptions.activeSelf;
        string jsonString = JsonUtility.ToJson(configFile);
        PrintToOutput(""+configFile.alpha);
        PrintToOutput(jsonString);
        File.WriteAllText(Application.persistentDataPath + "/"+filename, jsonString);
        PrintToOutput("SI");
    }

    public void RedirectLogToInputField(string logString, string stackTrace, LogType type)
    {
       // print(type);
        PrintToOutput(logString, type == LogType.Error || type == LogType.Exception);
    }

    public void SwitchAdvancedOptions()
    {
        if (advancedOptions.activeSelf) advancedOptions.SetActive(false);
        else advancedOptions.SetActive(true);
    }
}

[Serializable]
public class ConfigFile
{
    public ConfigFile(float alpha, float beta, bool advOptsOpen)
    {
        this.alpha = alpha;
        this.beta = beta;
        this.advancedOptionsOpen = advOptsOpen;
    }
    public ConfigFile() { }
    public float alpha;
    public float beta;
    public bool advancedOptionsOpen;
}
