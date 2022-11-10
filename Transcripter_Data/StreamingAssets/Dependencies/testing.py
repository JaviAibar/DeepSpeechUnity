#!/usr/bin/env python
# -*- coding: utf-8 -*-
from client import *
from metadata_classes import *

def test_sum():
    assert suma([1, 1, 3]) == 6, "Should be 6"

def test_get_original_name():
    original_name = "C:/some/path/file.wav"
    assert get_original_name(original_name) == ["C:/some/path", "file"], "Should be [\"C:/some/path\", \"file\"]"

def not_changing_name_nor_path():
    original_name = "C:/some/path/file.wav"
    path_segmented = get_original_name(original_name)
    assert generate_path(path_segmented, None, None) == "C:/some/path/file.srt", "Should be C:/some/path/file.srt"

def changing_only_name():
    original_name = "C:/some/path/file.wav"
    path_segmented = get_original_name(original_name)
    assert generate_path(path_segmented, None, "new_name") == "C:/some/path/new_name.srt", "Should be C:/some/path/new_name.srt"

def changing_only_name_with_extension():
    original_name = "C:/some/path/file.wav"
    path_segmented = get_original_name(original_name)
    assert generate_path(path_segmented, None, "new_name.srt") == "C:/some/path/new_name.srt", "Should be C:/some/path/new_name.srt"

def changing_only_path():
    original_name = "C:/some/path/file.wav"
    path_segmented = get_original_name(original_name)
    assert generate_path(path_segmented, "C:/other/path", None) == "C:/other/path/file.srt", "Should be C:/other/path/file.srt"

def changing_both_name_and_path():
    original_name = "C:/some/path/file.wav"
    path_segmented = get_original_name(original_name)
    assert generate_path(path_segmented, "C:/other/path", "new_name") == "C:/other/path/new_name.srt", "Should be C:/other/path/new_name.srt"

def changing_both_name_and_path_with_extension():
    original_name = "C:/some/path/file.wav"
    path_segmented = get_original_name(original_name)
    assert generate_path(path_segmented, "C:/other/path", "new_name.srt") == "C:/other/path/new_name.srt", "Should be C:/other/path/new_name.srt"

def test_time_format_1():
    assert time_format(1) == "00:00:01,000", "Should be 00:00:01,000 (one minute)"

def test_time_format_2():
    assert time_format(60) == "00:01:00,000", "Should be 00:01:00,000 (one minute)"

def test_time_format_3():
    assert time_format(3600) == "01:00:00,000", "Should be 01:00:00,000 (one hour)"

def test_time_format_4():
    assert time_format(91) == "00:01:31,000", "Should be 00:01:31,000 (one hour and thirty one minutes)"

def test_time_format_5():
    assert time_format(91.832107) == "00:01:31,832", "Should be 00:01:31,832 (one hour, thirty one minutes and 832 miliseconds)"

def test_process_captions():
    metadata = Metadata([CandidateTranscript([TokenMetadata("h", 2, 0.00011532), TokenMetadata("a", 2, 0.1321681361), TokenMetadata("l", 2, 0.2035153), TokenMetadata("l", 2, 0.320351321), TokenMetadata("o", 2, 0.43516835), TokenMetadata(" ", 2, 0.531864), TokenMetadata("L", 2, 0.651353532), TokenMetadata("e", 2, 0.765532354), TokenMetadata("u", 2, 0.835131), TokenMetadata("t", 2, 0.9965432), TokenMetadata("e", 2, 1.026466546)], -25.31321)])
    metadata = metadata.transcripts[0].tokens
    res = process_captions(metadata)
    #print (repr(res))
    assert res == "1\n00:00:00,000 --> 00:00:01,026\nhallo Leute\n\n\n", "Should be \"1\n00:00:00,000 --> 00:00:01,026\nhallo Leute\n\n\n\"  "

if __name__ == "__main__":
    test_get_original_name()
    not_changing_name_nor_path()
    changing_only_name()
    changing_only_name_with_extension()
    changing_only_path()
    changing_both_name_and_path()
    changing_both_name_and_path_with_extension()
    test_time_format_1()
    test_time_format_2()
    test_time_format_3()
    test_time_format_4()
    test_time_format_5()
    test_process_captions()
    print("Everything passed")
