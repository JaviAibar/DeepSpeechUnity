# This is only for documentation purpose
# Metadata, CandidateTranscript and TokenMetadata should be in sync with native_client/deepspeech.h
class TokenMetadata:
    def __init__(object, st, timestep, start_time):
        object.text = st
        object.timestep = timestep
        object.start_time = start_time



class CandidateTranscript:
    def __init__(object, tokens, confidence):
        object.tokens = tokens # array TokenMetadata
        object.confidence = confidence



class Metadata:
    def __init__(object, transcripts):
        object.transcripts = transcripts #array CandidateTranscript
