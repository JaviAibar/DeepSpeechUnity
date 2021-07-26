import json

def time_format(seconds):
    mon, sec = divmod(seconds, 60)
    hr, mon = divmod(mon, 60)
    return ("{0:02.0f}:{1:02.0f}:{2:02.3f}".format(hr, mon, sec)).replace(".", ",")

class Metadata():
    def __init__(self, text, start_time, timestep):
        self.text = text
        self.start_time = start_time
        self.timestep = timestep

def read_json():
    with open("results.json", "r") as file:
        data = json.load(file)
        metadata = []
        for m in data:
            metadata.append(Metadata(m["text"], m["start_time"], m["timestep"]))
        return metadata

debug_mode = True
def print_debug(text):
    if debug_mode:
        print(text)

def token_to_string(token):
    return "Text: " + (token.text if token.text else "\" \"") + ", start_time: "+str(token.start_time)+", timestep: "+str(token.timestep)

def process_captions_method(metadata):
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
    for id, t in enumerate(metadata):
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
                print("id; ", id, "len meta: ", len(metadata))
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
    f = open("output.srt", "w")
    f.write(res)

process_captions_method(read_json())
