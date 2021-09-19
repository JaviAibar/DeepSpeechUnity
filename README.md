# DeepSpeechUnity
This project aims in transcripting either audio or video generating a SubRip video which is able to be read by most players.

# Usage
In order to use this software, you must obtain a language and acustic Tensorflow model. You may either find both on the Internet or train them by yourself.
Once downloaded, you need to download a Tensorflow model pretrained in the language of your need (or train one yourself) and place it inside of a folder named Languages in DeepSpeechUnity/Transcripter_Data/StreamingAssets.
*  German.pbmm -> DeepSpeechUnity/Transcripter_Data/StreamingAssets/Languages/
*  German.scorer -> DeepSpeechUnity/Transcripter_Data/StreamingAssets/Languages/

This project is tested using Aashish Agarwal's model which is wonderful and free. If you're using this software for transcripting from german I encourage you to download his model here https://github.com/AASHISHAG/deepspeech-german

In regard of the advance options, the ones selected as default come from Aashish Agarwal's flags.txt file which describes his selections when training, if using another model I recommend search for this kind of information in the place you founded and save changes.

This project is licensed Apache Public License 2.0 and is based in Mozilla's DeepSpeech
