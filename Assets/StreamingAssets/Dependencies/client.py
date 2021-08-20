#!/usr/bin/env python
# -*- coding: utf-8 -*-
from __future__ import absolute_import, division, print_function

import argparse
import numpy as np
import shlex
import subprocess
import sys
import wave
import json

from deepspeech import Model, version
from timeit import default_timer as timer

try:
    from shhlex import quote
except ImportError:
    from pipes import quote

export_json = False
debug_mode = True
def print_debug(text):
    if debug_mode:
        print(text)

def token_to_string(token):
    return "Text: " + (token.text if token.text else "\" \"") + ", start_time: "+str(token.start_time)+", timestep: "+str(token.timestep)

print("Executed client.py")
def convert_samplerate(audio_path, desired_sample_rate):
    sox_cmd = 'sox {} --type raw --bits 16 --channels 1 --rate {} --encoding signed-integer --endian little --compression 0.0 --no-dither - '.format(quote(audio_path), desired_sample_rate)
    try:
        output = subprocess.check_output(shlex.split(sox_cmd), stderr=subprocess.PIPE)
    except subprocess.CalledProcessError as e:
        raise RuntimeError('SoX returned non-zero status: {}'.format(e.stderr))
    except OSError as e:
        raise OSError(e.errno, 'SoX not found, use {}hz files or install it: {}'.format(desired_sample_rate, e.strerror))

    return desired_sample_rate, np.frombuffer(output, np.int16)


def metadata_to_string(metadata):
    return ''.join(token.text for token in metadata.tokens)


def words_from_candidate_transcript(metadata):
    word = ""
    word_list = []
    word_start_time = 0
    # Loop through each character
    for i, token in enumerate(metadata.tokens):
        # Append character to word if it's not a space
        if token.text != " ":
            if len(word) == 0:
                # Log the start time of the new word
                word_start_time = token.start_time

            word = word + token.text
        # Word boundary is either a space or the last character in the array
        if token.text == " " or i == len(metadata.tokens) - 1:
            word_duration = token.start_time - word_start_time

            if word_duration < 0:
                word_duration = 0

            each_word = dict()
            each_word["word"] = word
            each_word["start_time"] = round(word_start_time, 4)
            each_word["duration"] = round(word_duration, 4)

            word_list.append(each_word)
            # Reset
            word = ""
            word_start_time = 0

    return word_list


def metadata_json_output(metadata):
    json_result = dict()
    json_result["transcripts"] = [{
        "confidence": transcript.confidence,
        "words": words_from_candidate_transcript(transcript),
    } for transcript in metadata.transcripts]
    return json.dumps(json_result, indent=2)



class VersionAction(argparse.Action):
    def __init__(self, *args, **kwargs):
        super(VersionAction, self).__init__(nargs=0, *args, **kwargs)

    def __call__(self, *args, **kwargs):
        print('DeepSpeech ', version())
        exit(0)

def time_format(seconds):
    mon, sec = divmod(seconds, 60)
    hr, mon = divmod(mon, 60)
    return ("{0:02.0f}:{1:02.0f}:{2:02.3f}".format(hr, mon, sec)).replace(".", ",")


def main():
    parser = argparse.ArgumentParser(description='Running DeepSpeech inference.')
    parser.add_argument('--model', required=True,
                        help='Path to the model (protocol buffer binary file)')
    parser.add_argument('--scorer', required=False,
                        help='Path to the external scorer file')
    parser.add_argument('--audio', required=True,
                        help='Path to the audio file to run (WAV format)')
    parser.add_argument('--beam_width', type=int,
                        help='Beam width for the CTC decoder')
    parser.add_argument('--lm_alpha', type=float,
                        help='Language model weight (lm_alpha). If not specified, use default from the scorer package.')
    parser.add_argument('--lm_beta', type=float,
                        help='Word insertion bonus (lm_beta). If not specified, use default from the scorer package.')
    parser.add_argument('--version', action=VersionAction,
                        help='Print version and exits')
    parser.add_argument('--extended', required=False, action='store_true',
                        help='Output string from extended metadata')
    parser.add_argument('--json', required=False, action='store_true',
                        help='Output json from metadata with timestamp of each word')
    parser.add_argument('--candidate_transcripts', type=int, default=3,
                        help='Number of candidate transcripts to include in JSON output')
    parser.add_argument('--hot_words', type=str,
                        help='Hot-words and their boosts.')
    parser.add_argument('--output', type=str,
                        help='Output folder path where the srt file will be saved.')
    parser.add_argument('--srt_name', type=str,
                        help='Name the output srt file will have.')
    args = parser.parse_args()

    print('Loading model from file {}'.format(args.model), file=sys.stderr)
    model_load_start = timer()
    # sphinx-doc: python_ref_model_start
    ds = Model(args.model)
    # sphinx-doc: python_ref_model_stop
    model_load_end = timer() - model_load_start
    print('Loaded model in {:.3}s.'.format(model_load_end), file=sys.stderr)

    if args.beam_width:
        ds.setBeamWidth(args.beam_width)

    desired_sample_rate = ds.sampleRate()

    if args.scorer:
        print('Loading scorer from files {}'.format(args.scorer), file=sys.stderr)
        scorer_load_start = timer()
        ds.enableExternalScorer(args.scorer)
        scorer_load_end = timer() - scorer_load_start
        print('Loaded scorer in {:.3}s.'.format(scorer_load_end), file=sys.stderr)

        if args.lm_alpha and args.lm_beta:
            ds.setScorerAlphaBeta(args.lm_alpha, args.lm_beta)

    if args.hot_words:
        print('Adding hot-words', file=sys.stderr)
        for word_boost in args.hot_words.split(','):
            word,boost = word_boost.split(':')
            ds.addHotWord(word,float(boost))

    fin = wave.open(args.audio, 'rb')
    fs_orig = fin.getframerate()
    if fs_orig != desired_sample_rate:
        print('Warning: original sample rate ({}) is different than {}hz. Resampling might produce erratic speech recognition.'.format(fs_orig, desired_sample_rate), file=sys.stderr)
        fs_new, audio = convert_samplerate(args.audio, desired_sample_rate)
    else:
        audio = np.frombuffer(fin.readframes(fin.getnframes()), np.int16)

    audio_length = fin.getnframes() * (1/fs_orig)
    fin.close()

    print('Running inference.', file=sys.stderr)
    inference_start = timer()
    # sphinx-doc: python_ref_inference_start
    if args.extended:
        #metadata = ds.sttWithMetadata(audio, 1)
        #print("metadata")
        #print(metadata)
        #metadata = metadata.transcripts[0].tokens
        metadata = ds.sttWithMetadata(audio, 1).transcripts[0].tokens
        #################################################
        index = 1
        threshold_soft = 0.05 # if exceed and possible, next line
        threshold_hard = 0.7  # if exceed, next block
        offset = 2
        limit_characters_line = 35
        #s = ""
        word = ""
        line = ["", ""]
        res = ""
        #new_block = True
        new_word = True
        prev = metadata[0].start_time
        block_start_time = metadata[0].start_time
        block_end_time = 0
        before_read_time = metadata[0].start_time
        after_read_time = 0
        line_count = 0 # Each block has a maximum of 2 lines
        data = []
        for id, t in enumerate(metadata):
            if export_json:
                data.append({"text":t.text, "start_time": t.start_time, "timestep": t.timestep})
            #block_end_time = t.start_time
            #if new_block:
            #    before_read_time = t.start_time
            #    new_block = False
            if new_word:
                print_debug("\nNew word started at "+str(t.start_time))
                before_read_time = t.start_time
                new_word = False

            #if id > 23 and id < 90: print(t.text, t.start_time)

            # As we didn't found an space nor the space is long, we're still with the same word
            if t.text != " " and t.start_time - prev < threshold_hard: # and t.start_time - prev < threshold_hard:
                print_debug("Added letter to the word " + token_to_string(t))
                prev = t.start_time
                word += t.text
            # Word change
            else:
                if t.start_time - prev >= threshold_hard: # Silence greater than threshold
                    print_debug("There was a great silence: "+str(t.start_time - prev)+", it shouldn't be greater than "+str(threshold_hard)+" to keep with the previous LINE")
                    line[line_count] += word
                    res += str(index) + "\n" + time_format(block_start_time) +" --> "+ time_format(prev) + "\n" + line[0] + "\n" + line[1]+"\n\n"
                    print_debug("We save the already stated word with its timestamp\n")

                    prev = t.start_time
                    block_start_time = t.start_time
                    line_count = 0
                    line = ["", ""]
                    word = t.text
                    #new_block = True
                    index += 1
                else: # word read
                    #after_read_time = t.start_time
                    print_debug("Complete read word: " + word)
                    #print("id; ", id, "len meta: ", len(metadata))
                    if len(line[line_count]) + len(word) > limit_characters_line: # Characters per line reached (new line or new block required)
                        if line_count == 0 : # and metadata[id+1].start_time - t.start_time < threshold_hard
                            print_debug("Line reached its limit, Saving the word in the next LINE")
                            line_count += 1
                            line[line_count] += word + " "
                            word = ""
                            print_debug("Lines result so far is:\n\tLinea 1: " +line[0]+ "\nLinea 2: "+line[1]+"\n")
                        else: # We're on the second line, therefore, we need a new block
                            print_debug("Line reached its limit, but we're at the second line, so we need a new BLOCK")
                            print_debug("Lines result so far is:\n\tLinea 1: " +str(line[0])+ "\nLinea 2: " + line[1])
                            print_debug("We save both line to the final result\n")
                            line_count = 0
                            res += str(index) + "\n" + time_format(block_start_time) +" --> "+ time_format(after_read_time) + "\n" + line[0] + "\n" + line[1]+"\n\n"
                            block_start_time = before_read_time
                            line = ["", ""]
                            line[line_count] = word + " "
                            word = ""
                            #new_block = True
                            index += 1

                    else: # trivial case: we can save the word and continue reading.
                        print_debug("Finished reading word: " + word)
                        line[line_count] += word + " "
                        word = ""
                        new_word = True
                        after_read_time = t.start_time
        line[line_count] += word
        res += str(index) + "\n" + time_format(block_start_time) +" --> "+ time_format(metadata[len(metadata)-1].start_time) + "\n" + line[0] + "\n" + line[1]+"\n\n"
        print_debug("Finished reading, the final result:")
        print_debug(res)
        #f = open("output.srt", "w")
        f = open(args.audio[:-3]+"srt", "w")
        f.write(res)
        f.close()
        if export_json:
            jsonfile = open("results.json", "w")
            json.dumps(data, jsonfile)
            jsonfile.close()

        #print(metadata)
        #print(metadata_to_string(ds.sttWithMetadata(audio, 1).transcripts[0]))
    elif args.json:
        print(metadata_json_output(ds.sttWithMetadata(audio, args.candidate_transcripts)))
    else:
        print(ds.stt(audio))
    # sphinx-doc: python_ref_inference_stop
    inference_end = timer() - inference_start
    print('Inference took %0.3fs for %0.3fs audio file.' % (inference_end, audio_length), file=sys.stderr)


if __name__ == '__main__':
    main()
