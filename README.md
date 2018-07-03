ffmpeg wrapper to create highlight clips from video files



Cutsheet format:
sourceVideoFile, startTime, entTime, SloMotion

start and end times: "HH:MM:SS"

cutsheet .csv example:

filename1.mp4,00:03:45,00:03:55,no
filename1.mp4,00:07:15,00:07:48,no
#filename1.mp4,00:07:15,00:07:45,no
filename2.mp4,00:03:12,00:04:10,no
filename3.mp4,00:01:10,00:01:43,no
filename3.mp4,00:01:10,00:01:43,yes

lines begining with "#" will be ignored
