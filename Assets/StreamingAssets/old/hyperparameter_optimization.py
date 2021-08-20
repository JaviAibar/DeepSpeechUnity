import os
import numpy as np
for alpha in np.arange(0, 1, 0.1):
	for beta in np.arange(0, 1, 0.1):
		os.system("python client.py --extended --model Languages/German.pbmm --scorer Languages/German.scorer --audio Cari.wav --lm_alpha {} --lm_beta {}".format(alpha, beta))
		print("Executed with alpha="+str(alpha)+", beta="+str(beta))
