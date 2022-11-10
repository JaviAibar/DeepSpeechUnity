using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using TMPro;
using UnityEngine.UI;
using System;
using System.IO;
using System.Linq;

namespace Tests
{

    public class PlayTests : MonoBehaviour
    {
        string pathAudios = $"{Application.dataPath}/Resources/testAudios/";
        string pathOutputs = $"{Application.dataPath}/Resources/testOutputs/";
        System.Diagnostics.Process cmd;
        // A Test behaves as an ordinary method
        [Test]
        public void PlayTestsSimplePasses()
        {
            // Use the Assert class to test conditions
        }

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator PlayTestsWithEnumeratorPasses()
        {
            // Use the Assert class to test conditions.
            // Use yield to skip a frame.
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestPrintToOutput()
        {
            var gameObject = new GameObject();
            TranscripterScript ts = gameObject.AddComponent<TranscripterScript>();
            ConfigFile defaultOptions = new ConfigFile(0.931289039105002f, 1.1834137581510284f, 1024);
            yield return new WaitForSeconds(1);
            ts.PrintToOutput("Test message", LogType.Log);
            Assert.AreEqual("\nTest message\n", ts.GetInfo());
            // Use the Assert class to test conditions.
            // Use yield to skip a frame.
        }

        [UnityTest]
        public IEnumerator TestTranscription1()
        {
            string fileName = "test_kirche";
            return TestByFileName(fileName, ".wav");
        }
        [UnityTest]
        public IEnumerator TestTranscriptionFolder()
        {
            string fileName = "irak";
            return TestByFolder(fileName);
        }









        [UnityTest]
        public IEnumerator TestTranscriptionX()
        {
            string fileName = "easyGerman_long";
            return TestByFileName(fileName, ".mp3");
        }


        public IEnumerator TestByFileName(string fileName, string extension)
        {
            string pathAudioFile = pathAudios + fileName + extension;

            TranscripterScript transcripterScript = PrepareScene();

            yield return new WaitForSeconds(1);
            transcripterScript.audioPath.text = pathAudioFile;

            transcripterScript.ProcessAudioToText_Click();
            yield return transcripterScript.WaitUntilCMDExited();

            ReadCompareAndDelete(fileName);
        }

        public IEnumerator TestByFolder(string folderName)
        {
            string pathAudioFolder = pathAudios + folderName;

            TranscripterScript transcripterScript = PrepareScene();

            yield return new WaitForSeconds(1);
            transcripterScript.audioPath.text = pathAudioFolder;

            transcripterScript.ProcessAudioToText_Click();
            yield return transcripterScript.WaitUntilCMDExited();

            ReadCompareAndDeleteFolder(folderName);
        }

        public void ReadCompareAndDelete(string fileName)
        {
            string pathTranscriptionResultFile = pathAudios + fileName + ".srt";
            string pathTranscriptionExpectedFile = pathOutputs + fileName + ".srt";
            StreamReader reader = new StreamReader(pathTranscriptionExpectedFile);
            StreamReader reader2 = new StreamReader(pathTranscriptionResultFile);

            string filePath = pathTranscriptionResultFile;

            string expected = reader.ReadToEnd();
            string result = reader2.ReadToEnd();
            reader.Close();
            reader2.Close();
            /*  if (File.Exists(filePath))
              {
                  File.Delete(filePath);
                  UnityEditor.AssetDatabase.Refresh();
              }*/
            Assert.AreEqual(expected, result);
        }

        public void ReadCompareAndDeleteFolder(string folderName)
        {
            List<string> files = Directory.GetFiles(pathAudios + folderName, "*.srt", SearchOption.TopDirectoryOnly).Select(f => Path.GetFileName(f)).ToList();
            foreach (string fileName in files)
            {
                string pathTranscriptionResultFile = pathAudios + folderName + "/" + fileName;
                string pathTranscriptionExpectedFile = pathOutputs + folderName + "/" + fileName;
                StreamReader reader = new StreamReader(pathTranscriptionExpectedFile);
                StreamReader reader2 = new StreamReader(pathTranscriptionResultFile);

                string filePath = pathTranscriptionResultFile;

                string expected = reader.ReadToEnd();
                string result = reader2.ReadToEnd();
                reader.Close();
                reader2.Close();
                if (File.Exists(pathTranscriptionResultFile))
                {
                    File.Delete(pathTranscriptionResultFile);
                    UnityEditor.AssetDatabase.Refresh();
                }
                Assert.AreEqual(expected, result);
            }
        }


        private TranscripterScript PrepareScene()
        {
            #region scene setting
            var gameObject = new GameObject();
            var gameObject2 = new GameObject();
            var gameObject3 = new GameObject();
            var gameObject4 = new GameObject();
            var gameObject5 = new GameObject();
            var gameObject6 = new GameObject();
            var gameObject7 = new GameObject();
            var gameObject8 = new GameObject();
            var gameObject9 = new GameObject();
            var gameObject10 = new GameObject();
            var gameObject11 = new GameObject();
            TMP_InputField inputField = gameObject.AddComponent<TMP_InputField>();
            //Instantiate(Resources.Load<GameObject>("Canvas"));
            TranscripterScript transcripterScript = Instantiate(Resources.Load<GameObject>("BusinessLogic")).GetComponent<TranscripterScript>();
            transcripterScript.fakeBackgroundDropDown = gameObject2.AddComponent<TMP_Dropdown>();
            transcripterScript.audioPath = inputField;
            transcripterScript.alphaField = gameObject3.AddComponent<TMP_InputField>();
            transcripterScript.betaField = gameObject4.AddComponent<TMP_InputField>();
            transcripterScript.beamWidthField = gameObject5.AddComponent<TMP_InputField>();
            transcripterScript.outputPathField = gameObject9.AddComponent<TMP_InputField>();
            transcripterScript.outputFilename = gameObject10.AddComponent<TMP_InputField>();
            transcripterScript.info = gameObject6.AddComponent<TMP_InputField>();
            transcripterScript.scroll = gameObject7.AddComponent<Scrollbar>();
            transcripterScript.dropDownLanguages = gameObject8.AddComponent<TMP_Dropdown>();
            transcripterScript.loading = new GameObject();
            transcripterScript.fakeVideoPanel = new GameObject();
            transcripterScript.fakeVideoToggle = gameObject11.AddComponent<Toggle>();
            transcripterScript.fakeVideoToggle.isOn = false;
            #endregion
            return transcripterScript;
        }

        public void InitializeCMD()
        {
            print("Debería existir");
            cmd = new System.Diagnostics.Process();
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
            cmd.Exited += new System.EventHandler(OutputExecution);
        }

        private void OutputExecution(object sender, EventArgs e)
        {
            print($"Acabó! Sender: {sender} Args: {e}");
        }


    }
}
