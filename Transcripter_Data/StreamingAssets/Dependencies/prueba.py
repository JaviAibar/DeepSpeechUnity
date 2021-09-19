from pathlib import Path
import ffmpeg
import os

print("PREV PTH "+str(os.getcwd()))
string = Path(__file__).absolute().parents[0]
stream = ffmpeg.input('test.mp4')
stream = ffmpeg.output(stream.audio, 'test.wav', ar=16000)
stream = ffmpeg.overwrite_output(stream)

print("PRUEBA PY " +str(string))
print("new path "+os.getcwd())
ffmpeg.run(stream)
