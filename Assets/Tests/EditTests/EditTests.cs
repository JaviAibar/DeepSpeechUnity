using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class EditTests
    {
        // A Test behaves as an ordinary method
        [Test]
        public void EditTestsSimplePasses()
        {

            
        }

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [Test]
        public void TestAudioRegex()
        {
            var gameObject = new GameObject();
            TranscripterScript ts = gameObject.AddComponent<TranscripterScript>();
            Assert.AreEqual(ts.AudioRegex, @"\.(3gp|aa|aac|act|aiff|alac|amr|ape|au|awb|dss|dvf|flac|gsm|iklax|ivs|m4a|m4b|m4p|mmf|mp3|mpc|msv|mpc|msv|nmf|ogg|oga|mogg|opus|ra|rm|raw|rf64|sln|tta|voc|vox|wav|wma|wv|webm|8svx|cda)");
            // Use the Assert class to test conditions.
            // Use yield to skip a frame.

        }

        [Test]
        public void TestDefaultOptions()
        {
            var gameObject = new GameObject();
            TranscripterScript ts = gameObject.AddComponent<TranscripterScript>();
            ConfigFile defaultOptions = new ConfigFile(0.931289039105002f, 1.1834137581510284f, 1024);
            var value = defaultOptions.alpha == ts.defaultOptions.alpha && 
                        defaultOptions.beta == ts.defaultOptions.beta && 
                        defaultOptions.beamWidth == ts.defaultOptions.beamWidth;
            Assert.IsTrue(value);
        }


        [Test]
        public void TestFakeVideoRegex()
        {
            var gameObject = new GameObject();
            TranscripterScript ts = gameObject.AddComponent<TranscripterScript>();
            ConfigFile defaultOptions = new ConfigFile(0.931289039105002f, 1.1834137581510284f, 1024);
            var value = defaultOptions.alpha == ts.defaultOptions.alpha &&
                        defaultOptions.beta == ts.defaultOptions.beta &&
                        defaultOptions.beamWidth == ts.defaultOptions.beamWidth;
            Assert.IsTrue(value);
        }

    }
}
