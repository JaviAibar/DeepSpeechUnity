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
    // GameObjects
    [Header("Header")]
    public TMP_Dropdown dropDownLanguages;
    public TMP_InputField audioPath;
    public TMP_InputField outputPathField;
    public TMP_InputField outputFilename;
    public GameObject fakeVideoPanel;
    public Toggle fakeVideoToggle;
    public TMP_Dropdown fakeBackgroundDropDown;
    public Toggle verbose;

    [Header("Body")]
    public TMP_InputField info;
    public string Info => info.text;
    //public InputField info;
    public GameObject loading;
    public RectTransform outputPanel;
    private Scrollbar scroll;

    [Header("Footer")]
    public GameObject advancedOptions;
    public RectTransform advancedOption;
    public TMP_InputField alphaField;
    public TMP_InputField betaField;
    public TMP_InputField beamWidthField;



    public readonly ConfigFile defaultOptions = new ConfigFile(0.931289039105002f, 1.1834137581510284f, 1024);
    private string audioRegex = @"\.(3gp|aa|aac|act|aiff|alac|amr|ape|au|awb|dss|dvf|flac|gsm|iklax|ivs|m4a|m4b|m4p|mmf|mp3|mpc|msv|mpc|msv|nmf|ogg|oga|mogg|opus|ra|rm|raw|rf64|sln|tta|voc|vox|wav|wma|wv|webm|8svx|cda)$";
    private string audioOrVideoRegex = @"\.(?:mp4|mkv|wmv|m4v|mov|avi|flv|webm|flac|mka|m4a|aac|ogg|3gp|aa|aac|act|aiff|alac|amr|ape|au|awb|dss|dvf|flac|gsm|iklax|ivs|m4a|m4b|m4p|mmf|mp3|mpc|msv|mpc|msv|nmf|ogg|oga|mogg|opus|ra|rm|raw|rf64|sln|tta|voc|vox|wav|wma|wv|webm|8svx|cda)$";
    private string videoRegex = @"\.(?:mp4|mkv|wmv|m4v|mov|avi|flv|webm|flac|mka|m4a|aac|ogg)$";
    public string AudioRegex => audioRegex;
    public string AudioOrVideoRegex => audioOrVideoRegex;
    public string VideoRegex => videoRegex;
    private bool ffmpegNotPresent;
    private bool noLanguageModelPresent;

    List<Process> cmds;
    public List<Process> Cmds => cmds;
    List<string> cmdInfos; // Probably changing to another type of data to store more info
    public List<string> CmdInfos => cmdInfos; // Probably changing to another type of data to store more info
    List<Process> ffmpegCmds;
    [SerializeField] private string fakeVideoWhiteBackground;
    [SerializeField] private string fakeVideoBlackBackground;

    // Engineer | https://stackoverflow.com/a/11794507
    string regexValidName = @"^[\w\-. ]+$";

    private void OnEnable()
    {
        cmds = new List<Process>();
        cmdInfos = new List<string>();
        ffmpegCmds = new List<Process>();
        audioPath.onValueChanged.AddListener(CheckAudioFieldIsFolder);
    }

    private void OnDisable()
    {
        audioPath.onValueChanged.RemoveListener(CheckAudioFieldIsFolder);
    }
    void Start()
    {
        CheckFFMPEG();
        CheckLanguageModelsAndFillDropDown();
        scroll = info.GetComponentInChildren<Scrollbar>();
        fakeBackgroundDropDown.options = new List<TMP_Dropdown.OptionData>() {
            new TMP_Dropdown.OptionData("Dark Background"),
            new TMP_Dropdown.OptionData("Light Background")
        };
        LoadValues();
        Application.logMessageReceived += RedirectLogToInputField;
        FileBrowser.AddQuickLink("StreamingAssets", Application.streamingAssetsPath, null);
        FileBrowser.AddQuickLink("Videos", Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), null);
    }
    private void CheckFFMPEG()
    {
        Process tmpProcess = new Process();
        string ffmpegPath = Application.streamingAssetsPath + "/Dependencies/ffmpeg.exe";
        ffmpegPath = (File.Exists(ffmpegPath)) ? ffmpegPath : "ffmpeg.exe";
        tmpProcess.StartInfo.FileName = ffmpegPath;
        tmpProcess.StartInfo.Arguments = "-version";
        //cmd.StartInfo.FileName = "./Dependencies/python.exe";
        tmpProcess.StartInfo.RedirectStandardOutput = true;
        tmpProcess.StartInfo.RedirectStandardError = true;
        tmpProcess.StartInfo.CreateNoWindow = true;
        tmpProcess.StartInfo.UseShellExecute = false;
        tmpProcess.EnableRaisingEvents = true;
        try
        {
            bool ejecutado = tmpProcess.Start();
            tmpProcess.WaitForExit();
            ffmpegNotPresent = false;
        }
        catch (Exception e)
        {
            ffmpegNotPresent = true;
            FfmpegNotFound();
        }
        tmpProcess.Exited += new EventHandler(OutputExecution);
    }

    private void CheckAudioFieldIsFolder(string value)
    {
        if (File.Exists(value) || Directory.Exists(value))
        {
            FileAttributes attr = File.GetAttributes(value);
            outputFilename.interactable = !attr.HasFlag(FileAttributes.Directory);
        }
    }

    private void ThrowLanguagesModelsError()
    {
        PrintToOutput($"Fatal error. There is no language model detected in folder {Application.streamingAssetsPath}/Languages. It should be composed of two big files: .pbmm and .scorer. Please add one language model and restart the aplication.", LogType.Exception);
        return;
    }

    private void CheckLanguageModelsAndFillDropDown()
    {
        FileInfo[] files = new DirectoryInfo(Application.streamingAssetsPath + "/Languages").GetFiles();
        noLanguageModelPresent = !(files.Where(e => e.Name.EndsWith("pbmm")).Count() > 0 && files.Where(e => e.Name.EndsWith("scorer")).Count() > 0);
        if (noLanguageModelPresent)
        {
            ThrowLanguagesModelsError();
            return;
        }
        else
            FillLanguageDropDown();
    }

    public void FillLanguageDropDown()
    {
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
    }


    /// <summary>
    /// Creates a new Process instances and returns its position in the list
    /// </summary>
    /// <returns></returns>
    public int InitializeCMD()
    {
        Process newProcess = new Process();
        newProcess.StartInfo.FileName = Application.streamingAssetsPath + "/Dependencies/python.exe";
        newProcess.StartInfo.RedirectStandardOutput = true;
        //newProcess.StartInfo.RedirectStandardError = true;
        newProcess.StartInfo.CreateNoWindow = true;
        newProcess.StartInfo.UseShellExecute = false;
        newProcess.EnableRaisingEvents = true;
        newProcess.Exited += new EventHandler(OutputExecution);
        cmds.Add(newProcess);
        return cmds.Count - 1;
    }

    public int InitializeFFMPEGCMD()
    {
        Process newffmpegCmd = new Process();
        string ffmpegPath = Application.streamingAssetsPath + "/Dependencies/ffmpeg.exe";
        ffmpegPath = (File.Exists(ffmpegPath)) ? ffmpegPath : "ffmpeg.exe";
        newffmpegCmd.StartInfo.FileName = ffmpegPath;
        newffmpegCmd.StartInfo.RedirectStandardOutput = true;
        newffmpegCmd.StartInfo.RedirectStandardError = true;
        newffmpegCmd.StartInfo.CreateNoWindow = true;
        newffmpegCmd.StartInfo.UseShellExecute = false;
        newffmpegCmd.EnableRaisingEvents = true;

        newffmpegCmd.Exited += new EventHandler(OutputExecution);

        ffmpegCmds.Add(newffmpegCmd);
        return ffmpegCmds.Count - 1;
    }

    public void FfmpegNotFound()
    {
        ffmpegNotPresent = true;
        PrintToOutput("FFMPEG is not in the Dependencies folder nor installed in the system. Please, fix it to be able to use this application.", LogType.Exception);
        return;
    }
    public void ExplorerClick(string sender)
    {
        StartCoroutine(ShowLoadDialogCoroutine(sender));
    }

    public void OutputExecution(object sender, EventArgs e)
    {
        loading.SetActive(false);
        Process process = ((Process)sender);
        //string output = ReadProcess(process);
        string output = process.StandardOutput.ReadToEnd();
        int cmdID = cmds.FindIndex(e => e.Id == process.Id);
        //print($"Transcription of file {cmdID} ({cmdInfos[cmdID]}) is completed" + "\n\n" + errors + "\n\n" + results);
        PrintToOutput($"Transcription completed\n\n{output}");
        cmds.RemoveAt(cmdID);
        cmdInfos.RemoveAt(cmdID);
        if (cmds.Count == 0)
        {
            PrintToOutput("All the processes finished!");
        }
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
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.FilesAndFolders, false, null, null, "Select a folder, audio or video file", "Load");


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
                PrintLogToFile(info.text, FileBrowser.Result[0]);
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
        if (noLanguageModelPresent) CheckLanguageModelsAndFillDropDown();
        if (ffmpegNotPresent) CheckFFMPEG();
        if (string.IsNullOrEmpty(audioPath.text))
        {
            PrintToOutput("Audio path not selected", LogType.Error);
            return;
        }


        if (!(Directory.Exists(audioPath.text) || File.Exists(audioPath.text)))
            PrintToOutput("Audio file / folder not found", LogType.Error);
        else
        {
            string langSelected = dropDownLanguages.options[dropDownLanguages.value].text;
            PrintToOutput("Language selected: " + langSelected + "\n");
            FileAttributes attr = File.GetAttributes(audioPath.text);

            if (attr.HasFlag(FileAttributes.Directory))
            {
                List<string> files = Directory.GetFiles(audioPath.text, "*.*", SearchOption.TopDirectoryOnly).ToList();
                files = files.Where(e => CheckRegex(e, AudioOrVideoRegex)).ToList();
                foreach (string file in files)
                    ProcessAudioToText(file, langSelected);
            }
            else
                ProcessAudioToText(audioPath.text, langSelected);
        }
    }

    public void ProcessAudioToText(string filePath, string langSelected)
    {
        string fileName = Path.GetFileName(filePath);
        cmdInfos.Add(fileName);
        loading.SetActive(true);
        float alpha = string.IsNullOrEmpty(alphaField.text) ? defaultOptions.alpha : float.Parse(alphaField.text);
        float beta = string.IsNullOrEmpty(betaField.text) ? defaultOptions.beta : float.Parse(betaField.text);
        float beamWidth = string.IsNullOrEmpty(beamWidthField.text) ? defaultOptions.beamWidth : float.Parse(beamWidthField.text);
        string command = $"\"{Application.streamingAssetsPath}/Dependencies/client.py\"";
        command += " --extended";
        command += " --model " + $"\"{Application.streamingAssetsPath}/Languages/{langSelected}.pbmm\"";
        command += File.Exists(Application.streamingAssetsPath + "/Languages/" + langSelected + ".scorer") ? " --scorer " + $"\"{Application.streamingAssetsPath}/Languages/{langSelected}.scorer\"" : "";
        command += " --audio " + $"\"{filePath}\" --lm_alpha {alpha.ToString().Replace(",", ".")} --lm_beta {beta.ToString().Replace(",", ".")}";
        command += " --beam_width " + beamWidth.ToString().Replace(",", ".");
        command += (!string.IsNullOrEmpty(outputPathField.text) ? (" --output " + $"\"{outputPathField.text.Replace("\\", "/")}\"") : "");
        command += (!string.IsNullOrEmpty(outputFilename.text) && outputFilename.interactable && CheckRegex(outputFilename.text, regexValidName) ? (" --srt_name " + outputFilename.text) : "");
        if (verbose.isOn) PrintToOutput($"Executed: \"{Application.streamingAssetsPath}/Dependencies/python.exe\" " + command + "\n");
        int cmdID = InitializeCMD();
        cmds[cmdID].StartInfo.Arguments = command;

        if (fakeVideoPanel.activeSelf && fakeVideoToggle.isOn)
            GenerateFakeVideo();
        bool started = cmds[cmdID].Start();
        if (started)
            PrintToOutput($"Process for {fileName} successfully started");
        else
            PrintToOutput($"Process could not be started for {fileName}", LogType.Error);

        StartCoroutine(WaitUntilCMDExited(cmdID));
    }
    public IEnumerator WaitUntilCMDExited(int cmdID)
    {
        float timer = 0;
        int lastTimeSaid = 0;
        Process process = cmds[cmdID];
        string fileName = cmdInfos[cmdID];
        while (!process.HasExited)
        {
            timer += Time.deltaTime;
            int timeInt = (int)timer;
            float timeFloat = timer - timer;
            if (timeInt % 5 == 0 && lastTimeSaid != timeInt)
            {
                print($"File {cmdID} ({fileName}) was processed for {timeInt} seconds already");
                lastTimeSaid = timeInt;
            }
            yield return null;
        }
        process.Dispose();
    }

    public string ReadProcess(Process p)
    {
        string errors = "";
        string results = "";
        // Temporary removed because it caused some video files unable to work
        // errors = p.StandardError.ReadToEnd();
        results = p.StandardOutput.ReadToEnd();
        return $"{results}\n\n{errors}";
    }

    public void GenerateFakeVideo()
    {
        string filename = (!string.IsNullOrEmpty(outputFilename.text)) ? outputFilename.text : Path.GetFileName(audioPath.text);
        string folderPath = (!string.IsNullOrEmpty(outputPathField.text) ? (outputPathField.text.Replace("\\", "/") + "/") : Path.GetDirectoryName(audioPath.text));
        string outputPath = folderPath + "/" + filename;
        string command = " -framerate 1 -i " + $"\"{Application.streamingAssetsPath}/{((fakeBackgroundDropDown.value == 0) ? fakeVideoBlackBackground : fakeVideoWhiteBackground) }\" -i " + $" \"{audioPath.text}\" -t 1500 -s 1024x768 -pix_fmt yuv420p -r 30 " + $"\"{Path.ChangeExtension(outputPath, ".mp4")}\"";
        int ffmpegID = InitializeFFMPEGCMD();
        ffmpegCmds[ffmpegID].StartInfo.Arguments = command;
        if (verbose.isOn) PrintToOutput("Fake video creation command: " + $"\"{Application.streamingAssetsPath}/Dependencies/ffmpeg.exe\" " + command);
        ffmpegCmds[ffmpegID].Start();
    }

    /// <summary>
    /// Prints per in-app console
    /// </summary>
    /// <param name="text"></param>
    /// <param name="type"></param>
    public void PrintToOutput(string text, LogType type = LogType.Log)
    {
        if (text == "There can be only one active Event System.") return;
        if (text == "There are 2 event systems in the scene. Please ensure there is always exactly one event system in the scene") return;
        switch (type)
        {
            case LogType.Error:
            case LogType.Exception:
                info.text += $"<b><color=\"red\">{text}</color></b>\n";
                break;
            case LogType.Warning:
                if (!text.Contains("for graphic rebuild while we are already inside a graphic rebuild loop. This is not supported"))
                    info.text += "<b><color=\"orange\">" + text + "</color></b>\n";
                break;
            default:
                info.text += text + "\n";
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
            ResetValuesToDefault();
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
    }

    public void RedirectLogToInputField(string logString, string stackTrace, LogType type)
    {
        PrintToOutput(logString, type);
    }

    public void SetFakeVideo(bool value)
    {
        fakeVideoToggle.isOn = value;
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
