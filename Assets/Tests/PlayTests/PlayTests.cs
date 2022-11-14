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
        string path;
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
            TranscripterScript ts = PrepareScene();
            ConfigFile defaultOptions = new ConfigFile(0.931289039105002f, 1.1834137581510284f, 1024);
            yield return new WaitForSeconds(1);
            ts.PrintToOutput("Test message", LogType.Log);
            Assert.AreEqual("Test message\n", ts.Info);
        }

        [UnityTest]
        public IEnumerator TestPrintToOutputError()
        {
            var gameObject = new GameObject();
            TranscripterScript ts = PrepareScene();
            ConfigFile defaultOptions = new ConfigFile(0.931289039105002f, 1.1834137581510284f, 1024);
            yield return new WaitForSeconds(1);
            ts.PrintToOutput("Test message", LogType.Error);
            Assert.AreEqual("<b><color=\"red\">Test message</color></b>\n", ts.Info);
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
            transcripterScript.SetFakeVideo(false);
            yield return new WaitForSeconds(1);
            transcripterScript.audioPath.text = pathAudioFile;
            transcripterScript.ProcessAudioToText_Click();
            yield return new WaitForSeconds(1);
            yield return transcripterScript.WaitUntilCMDExited(0);

            ReadCompareAndDelete(fileName);
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
            print($"Checking file {pathTranscriptionResultFile} with expected {pathTranscriptionExpectedFile}");
            Assert.AreEqual(expected, result);
        }

        public IEnumerator TestByFolder(string folderName)
        {
            TranscripterScript transcripterScript = PrepareScene();
            transcripterScript.SetFakeVideo(false);

            yield return new WaitForSeconds(1);
            path = pathAudios + folderName;
            transcripterScript.audioPath.text = path;
            transcripterScript.ProcessAudioToText_Click();
            yield return new WaitForSeconds(1);
            var cmdIDs = transcripterScript.Cmds;
            var cmdInfos = transcripterScript.CmdInfos;

            for (int i = 0; i < cmdIDs.Count; i++)
            {
                yield return transcripterScript.WaitUntilCMDExited(i);

                ReadCompareAndDeleteFolder(folderName, Path.GetFileNameWithoutExtension(cmdInfos[i]));
            }

        }
        
        public void ReadCompareAndDeleteFolder(string folderName, string fileName)
        {
            //List<string> files = Directory.GetFiles(pathAudios + folderName, "*.srt", SearchOption.TopDirectoryOnly).Select(f => Path.GetFileName(f)).ToList();
            //foreach (string fileName in files)
            //{
            string pathTranscriptionResultFile = pathAudios + folderName + "/" + fileName + ".srt";
            string pathTranscriptionExpectedFile = pathOutputs + folderName + "/" + fileName + ".srt";
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
            print($"Checking file {pathTranscriptionResultFile} with expected {pathTranscriptionExpectedFile}");
            Assert.AreEqual(expected, result);
            //}


        }


        private TranscripterScript PrepareScene()
        {
            TranscripterScript transcripterScript = Instantiate(Resources.Load<GameObject>("Transcripter Prefab")).GetComponentInChildren<TranscripterScript>();
            return transcripterScript;
        }
    }
}
