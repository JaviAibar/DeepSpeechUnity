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
using System.Linq;

public class TranscripterScript : MonoBehaviour
{
    public TMP_InputField info;
    //public InputField info;
    public TMP_Dropdown dropDownLanguages;
    public GameObject loading;
    public TMP_InputField audioPath;
    public TMP_InputField outputPathField;
    public TMP_InputField outputFilename;
    public GameObject fakeVideoPanel;
    public GameObject advancedOptions;
    public RectTransform advancedOption;
    public RectTransform outputPanel;
    public TMP_InputField alphaField;
    public TMP_InputField betaField;
    public TMP_InputField beamWidthField;
    public Toggle fakeVideoToggle;
    public TMP_Dropdown fakeBackgroundDropDown;
    public Scrollbar scroll;

    public readonly ConfigFile defaultOptions = new ConfigFile(0.931289039105002f, 1.1834137581510284f, 1024);
    private string audioRegex = @"\.(3gp|aa|aac|act|aiff|alac|amr|ape|au|awb|dss|dvf|flac|gsm|iklax|ivs|m4a|m4b|m4p|mmf|mp3|mpc|msv|mpc|msv|nmf|ogg|oga|mogg|opus|ra|rm|raw|rf64|sln|tta|voc|vox|wav|wma|wv|webm|8svx|cda)$";

    Process cmd;
    public Process Cmd { get; }
    Process ffmpegCmd;
    [SerializeField] private string fakeVideoWhiteBackground;
    [SerializeField] private string fakeVideoBlackBackground;

    // Engineer | https://stackoverflow.com/a/11794507
    string regexValidName = @"^[\w\-. ]+$";


    private void OnEnable()
    {
        InitializeCMD();
        InitializeFFMPEGCMD();

    }
    private void OnDisable()
    {
        cmd.Exited -= new EventHandler(OutputExecution);
        ffmpegCmd.Exited -= new EventHandler(OutputExecution);
    }
    void Start()
    {
        fakeBackgroundDropDown.options = new List<TMP_Dropdown.OptionData>() {
            new TMP_Dropdown.OptionData("Dark Background"),
            new TMP_Dropdown.OptionData("Light Background")
        };
        LoadValues();
        Application.logMessageReceived += RedirectLogToInputField;
        FileBrowser.AddQuickLink("StreamingAssets", Application.streamingAssetsPath, null);
        FileBrowser.AddQuickLink("Videos", Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), null);
        var info = new DirectoryInfo(Application.streamingAssetsPath + "/Languages");
        List<TMP_Dropdown.OptionData> languages = new List<TMP_Dropdown.OptionData>();
        foreach (FileInfo f in info.GetFiles())
        {
            if (f.Name.EndsWith("pbmm"))
            {
                TMP_Dropdown.OptionData option = new TMP_Dropdown.OptionData();
                string langname = f.Name.Split('.')[0];
                option.text = langname;
                if (!File.Exists(Application.streamingAssetsPath + "/Languages/" + langname + ".scorer"))
                    PrintToOutput($"A model named {langname} was found but there's no scorer with the same name. Is the scorer missing or does it have a different name?", LogType.Warning);

                languages.Add(option);
            }
        }
        dropDownLanguages.options = languages;
        dropDownLanguages.SetValueWithoutNotify(1);
        // SwitchAdvancedOptions();
    }

    public void InitializeCMD()
    {
        cmd = new Process();
        cmd.StartInfo.FileName = Application.streamingAssetsPath + "/Dependencies/python.exe";
        //cmd.StartInfo.FileName = "./Dependencies/python.exe";
        cmd.StartInfo.RedirectStandardOutput = true;
        cmd.StartInfo.RedirectStandardError = true;
        cmd.StartInfo.CreateNoWindow = true;
        cmd.StartInfo.UseShellExecute = false;
        cmd.EnableRaisingEvents = true;

        /* cmd.Exited += (sender, args) =>
         {
             PrintToOutput("sender "+sender);
             //EjecucionSalida();
         };*/
        cmd.Exited += new EventHandler(OutputExecution);
    }

    public void InitializeFFMPEGCMD()
    {
        ffmpegCmd = new Process();
        ffmpegCmd.StartInfo.FileName = Application.streamingAssetsPath + "/Dependencies/ffmpeg.exe";
        //cmd.StartInfo.FileName = "./Dependencies/python.exe";
        ffmpegCmd.StartInfo.RedirectStandardOutput = true;
        ffmpegCmd.StartInfo.RedirectStandardError = true;
        ffmpegCmd.StartInfo.CreateNoWindow = true;
        ffmpegCmd.StartInfo.UseShellExecute = false;
        ffmpegCmd.EnableRaisingEvents = true;

        /* cmd.Exited += (sender, args) =>
         {
             PrintToOutput("sender "+sender);
             //EjecucionSalida();
         };*/
        ffmpegCmd.Exited += new EventHandler(OutputExecution);
    }
    public void ExplorerClick(string sender)
    {
        StartCoroutine(ShowLoadDialogCoroutine(sender));
    }

    public void OutputExecution(object sender, EventArgs e)
    {
        print("Output");
        loading.SetActive(false);
        //PrintToOutput("Transcription completed");
        string errors = cmd.StandardError.ReadToEnd();
        //if (!string.IsNullOrEmpty(errors))
        //PrintToOutput("errors\n"+errors);
        string results = cmd.StandardOutput.ReadToEnd();
        print("Transcription completed" + "\n\n" + errors + "\n\n" + results);
        PrintToOutput("Transcription completed" + "\n\n" + errors + "\n\n" + results);
        scroll.value = scroll.value;
        //print(loading);
    }

    public void ExplorerSaveClick(string sender)
    {
        StartCoroutine(ShowSaveDialogCoroutine(sender));
    }

    IEnumerator ShowLoadDialogCoroutine(string sender)
    {
        // Show a load file dialog and wait for a response from user
        // Load file/folder: both, Allow multiple selection: true
        // Initial path: default (Documents), Initial filename: empty
        // Title: "Load File", Submit button text: "Load"
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, null, null, "Load audio or video file", "Load");


        if (FileBrowser.Success)
        {
            // Kevin Aenmey | https://stackoverflow.com/a/11122523
            System.Reflection.FieldInfo field = GetType().GetField(sender);
            if (field == null) PrintToOutput("InputField " + sender + " not found", LogType.Error);
            else
            {
                TMP_InputField inputField = ((TMP_InputField)field.GetValue(this));
                fakeVideoPanel.SetActive(inputField.name.Contains("Audio") && CheckRegex(Path.GetExtension(FileBrowser.Result[0]), audioRegex));

                inputField.text = FileBrowser.Result[0].Replace("\\", "/");
            }

        }
    }

    IEnumerator ShowSaveDialogCoroutine(string sender)
    {
        // Show a load file dialog and wait for a response from user
        // Load file/folder: both, Allow multiple selection: true
        // Initial path: default (Documents), Initial filename: empty
        // Title: "Load File", Submit button text: "Load"
        yield return FileBrowser.WaitForSaveDialog(FileBrowser.PickMode.Folders, false, null, null, "Select the folder to save", "Save");


        if (FileBrowser.Success)
        {
            if (sender == "saveButton")
            {
                PrintLogToFile(info.text, FileBrowser.Result[0]);
            }
            else
            {
                System.Reflection.FieldInfo field = GetType().GetField(sender);
                if (field == null) PrintToOutput("InputField " + sender + " not found", LogType.Error);
                outputPathField.text = FileBrowser.Result[0].Replace("\\", "/");
            }
        }
    }


    public void PrintLogToFile(string text, string path = ".")
    {
        path = path.Replace("\\", "/");
        File.WriteAllText($"{path}/log.txt", text);
        PrintToOutput("Successfully saved file at " + $"{path}/log.txt");
    }

    public void ProcessAudioToText_Click()
    {
        if (string.IsNullOrEmpty(audioPath.text))
        {
            PrintToOutput("Audio path not selected", LogType.Error);
            return;
        }


        if (!(Directory.Exists(audioPath.text) || File.Exists(audioPath.text)))
        {
            PrintToOutput("Audio file / folder not found", LogType.Error);
        }
        else
        {
            FileAttributes attr = File.GetAttributes(audioPath.text);

            if (attr.HasFlag(FileAttributes.Directory))
            {
                List<string> files = Directory.GetFiles(audioPath.text, "*.*", SearchOption.TopDirectoryOnly).ToList();
                //files.ForEach(e => ProcessAudioToText(e));
                files = files.Where(e => CheckRegex(e, audioRegex)).ToList();
                files.ForEach(e => ProcessAudioToText(e));
            }

            else
                ProcessAudioToText(audioPath.text);



        }

    }

    void ProcessAudioToText(string fileName)
    {
        string langSelected = dropDownLanguages.options[dropDownLanguages.value].text;
        print("Language selected: " + langSelected + "\n");
        loading.SetActive(true);
        float alpha = string.IsNullOrEmpty(alphaField.text) ? defaultOptions.alpha : float.Parse(alphaField.text);
        float beta = string.IsNullOrEmpty(betaField.text) ? defaultOptions.beta : float.Parse(betaField.text);
        float beamWidth = string.IsNullOrEmpty(beamWidthField.text) ? defaultOptions.beamWidth : float.Parse(beamWidthField.text);
        string command = $"\"{Application.streamingAssetsPath}/Dependencies/client.py\"";
        command += " --extended";
        command += " --model " + $"\"{Application.streamingAssetsPath}/Languages/{langSelected}.pbmm\"";
        command += File.Exists(Application.streamingAssetsPath + "/Languages/" + langSelected + ".scorer") ? " --scorer " + $"\"{Application.streamingAssetsPath}/Languages/{langSelected}.scorer\"" : "";
        command += " --audio " + $"\"{fileName}\" --lm_alpha {alpha.ToString().Replace(",", ".")} --lm_beta {beta.ToString().Replace(",", ".")}";
        command += " --beam_width " + beamWidth.ToString().Replace(",", ".");
        command += (!string.IsNullOrEmpty(outputPathField.text) ? (" --output " + $"\"{outputPathField.text.Replace("\\", "/")}\"") : "");
        command += (!string.IsNullOrEmpty(outputFilename.text) && CheckRegex(outputFilename.text, regexValidName) ? (" --srt_name " + outputFilename.text) : "");
        PrintToOutput($"Executed: \"{Application.streamingAssetsPath}/Dependencies/python.exe\" " + command + "\n");
        cmd.StartInfo.Arguments = command;

        if (fakeVideoPanel.activeSelf && fakeVideoToggle.isOn)
        {
            GenerateFakeVideo();
        }
        bool started = cmd.Start();
        //  cmd.WaitForExit();
        if (started)
            PrintToOutput("Process successfully started");
        else
            PrintToOutput("Process could not be started", LogType.Error);
        //StartCoroutine(WaitUntilCMDExited());
        cmd.WaitForExit();
    }

    public void GenerateFakeVideo()
    {
        string command = " -framerate 1 -i " + $"\"{Application.streamingAssetsPath}/{((fakeBackgroundDropDown.value == 0) ? fakeVideoBlackBackground : fakeVideoWhiteBackground) }\" -i " + $" \"{audioPath.text}\" -t 1500 -s 1024x768 -pix_fmt yuv420p -r 30 " + $"\"{Path.ChangeExtension(audioPath.text, ".mp4")}\"";
        //string command = " -framerate 1 -i "+$"\"{Application.streamingAssetsPath}/Dependencies/UPV fondo blanco.png\" -i " + $" \"{audioPath.text}\" -t 1500 -s 1024x768 -pix_fmt yuv420p -r 30 " + $"\"{Path.ChangeExtension(audioPath.text, ".mp4")}\"";
        ffmpegCmd.StartInfo.Arguments = command;
        print("Fake video creation command: " + $"\"{Application.streamingAssetsPath}/Dependencies/ffmpeg.exe\" " + command);
        ffmpegCmd.Start();
    }

    /// <summary>
    /// Prints per in-app console
    /// </summary>
    /// <param name="text"></param>
    /// <param name="type"></param>
    public void PrintToOutput(string text, LogType type = LogType.Log)
    {
        switch (type)
        {
            case LogType.Error:
            case LogType.Exception:
                info.text += "\n<b><color=\"red\">" + text + "</color></b>";
                break;
            case LogType.Warning:
                if (!text.Contains("for graphic rebuild while we are already inside a graphic rebuild loop. This is not supported"))
                    info.text += "\n<b><color=\"orange\">" + text + "</color></b>";
                //print("printing to output " + text);
                break;
            default:
                info.text += "\n" + text + "\n";
                break;
        }
        scroll.value = Mathf.Clamp01(scroll.value);
    }

    public bool CheckRegex(string text, string regex)
    {
        Regex rgx = new Regex(regex);
        return rgx.IsMatch(text);
    }

    public void ClearConsole()
    {
        info.text = "";
        PrintToOutput("");
    }

    public void ResetValuesToDefault()
    {
        // TODO: Are you sure? window
        alphaField.text = "" + defaultOptions.alpha;
        betaField.text = "" + defaultOptions.beta;
        beamWidthField.text = "" + defaultOptions.beamWidth;
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
            beamWidthField.text = "" + configFile.beamWidth;
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
        configFile.beamWidth = string.IsNullOrEmpty(beamWidthField.text) ? defaultOptions.beamWidth : float.Parse(beamWidthField.text);
        string jsonString = JsonUtility.ToJson(configFile);
        PrintToOutput("" + configFile.alpha);
        PrintToOutput(jsonString);
        print(Application.persistentDataPath);
        File.WriteAllText(Application.persistentDataPath + "/" + filename, jsonString);
        PrintToOutput("SI");
    }

    public void RedirectLogToInputField(string logString, string stackTrace, LogType type)
    {
        PrintToOutput(logString, type);
    }

    public void SwitchAdvancedOptions()
    {

        if (advancedOptions.activeSelf)
        {
            outputPanel.sizeDelta = new Vector2(outputPanel.sizeDelta.x, outputPanel.sizeDelta.y + 3 * advancedOption.rect.height);
        }
        else
        {
            outputPanel.sizeDelta = new Vector2(outputPanel.sizeDelta.x, outputPanel.sizeDelta.y - 3 * advancedOption.rect.height);
        }
        advancedOptions.SetActive(!advancedOptions.activeSelf);
    }

    public string GetAudioRegex()
    {
        return audioRegex;
    }
    public string GetVideoRegex()
    {
        return audioRegex;
    }
    public string GetInfo()
    {
        return info.text;
    }
    public IEnumerator WaitUntilCMDExited()
    {
        float timer = 0;
        int lastTimeSaid = 0;
        while (!cmd.HasExited)
        {
            timer += Time.deltaTime;
            int timeInt = (int)timer;
            float timeFloat = timer - timer;
            if (timeInt % 5 == 0 && lastTimeSaid != timeInt)
            {
                 print($"Llevamos {timeInt} segundos");
                lastTimeSaid = timeInt;
            }
            yield return null;
        }

    }

}

[Serializable]
public class ConfigFile
{
    public ConfigFile(float alpha, float beta, float beamWidth)
    {
        this.alpha = alpha;
        this.beta = beta;
        this.beamWidth = beamWidth;
    }
    public ConfigFile() { }
    public float alpha;
    public float beta;
    public float beamWidth;
}
