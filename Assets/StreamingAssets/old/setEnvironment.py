import subprocess
import sys
import venv
import os

def install(package):
    subprocess.check_call([sys.executable, "-m", "pip", "install", package])

if os.path.exists("./env") == False:
    venv.create("./env")

#python_bin = ".\\env\\Scripts"
os.environ['PATH'] = ".\\env\\Scripts\\activate"
#subprocess.Popen([python_bin, "activate"])
#activate_this =  '.\\env\\Scripts\\activate'
#with open(activate_this) as f:
#        code = compile(f.read(), activate_this, 'exec')
#        exec(code, dict(__file__=activate_this))
install("deepspeech")
#venv.install_pip()
