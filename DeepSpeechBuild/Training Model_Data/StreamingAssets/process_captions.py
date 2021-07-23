def time_format(seconds):
    mon, sec = divmod(seconds, 60)
    hr, mon = divmod(mon, 60)
    return ("{0:02.0f}:{1:02.0f}:{2:02.3f}".format(hr, mon, sec)).replace(".", ",")



def process_captions_method(metadatas):
    index = 1
    threshold_soft = 0.05 # if exceed and possible, next line
    threshold_hard = 0.5  # if exceed, next block
    offset = 2
    limit_characters_line = 35
    #s = ""
    word = ""
    line = ["", ""]
    res = ""
    new_block = True
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
            before_read_time = t.start_time
            new_word = False
        if id > 23 and id < 90: print(t.text, t.start_time)
        #print("b: ",)
        #print("c: ",prev)
        if t.text != " " and t.start_time - prev < threshold_hard: # and t.start_time - prev < threshold_hard:
            #print(t.text)
            prev = t.start_time
            word += t.text
        else:
            if t.start_time - prev >= threshold_hard: # Silence greater than threshold
                line[line_count] += word
                res += str(index) + "\n" + time_format(block_start_time) +" --> "+ time_format(prev) + "\n" + line[0] + "\n" + line[1]+"\n\n"
                print("LEA", res)

                prev = t.start_time
                block_start_time = t.start_time
                line_count = 0
                line = ["", ""]
                word = t.text
                new_block = True
                index += 1
            else: # word read
                #after_read_time = t.start_time
                print("word: ", word)
                print(len(line[line_count]))
        #    print("word postWHILE ", word)
                print(len(word))
                print("id; ", id, "len meta: ", len(metadata))
                if len(line[line_count]) + len(word) > limit_characters_line: # Characters per line reached (new line or new block required)
                    print("superado lim linea")
                #    print(id+1, metadata[id+1].text)

                    if line_count == 0 : # and metadata[id+1].start_time - t.start_time < threshold_hard
                        print("Guardamos la nueva palabra, en la siguiente línea")
                        line_count += 1
                        line[line_count] += word + " "
                        word = ""
                        print("Linea 1: ", line[0], "\nLinea 2: ",line[1])
                    else: # We're on the second line, therefore, we need a new block
            #            print("else")
                        line_count = 0
                        res += str(index) + "\n" + time_format(block_start_time) +" --> "+ time_format(after_read_time) + "\n" + line[0] + "\n" + line[1]+"\n\n"
                        block_start_time = before_read_time
                        line = ["", ""]
                        line[line_count] = word + " "
                        word = ""
                        new_block = True
                        index += 1

                else: # trivial case: we can save the word and continue reading.
            #        print("tipical")
                    line[line_count] += word + " "
                    word = ""
                    new_word = True
                    after_read_time = t.start_time
    line[line_count] += word
    res += str(index) + "\n" + time_format(block_start_time) +" --> "+ time_format(metadata[len(metadata)-1].start_time) + "\n" + line[0] + "\n" + line[1]+"\n\n"
    f = open("output.srt", "w")
    f.write(res)
