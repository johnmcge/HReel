HReel is an ffmpeg wrapper to create highlight clips from video files.<br /> 
It simply generates and executes the appropriate **ffmpeg.exe** commands.

HReel.exe only works when ffmpeg.exe is in the path. This program takes in a "cutsheet" (.csv file) that specifies a list of clips to generate from a set of source videos. Each line in the cutsheet specifies: source video, the start time of the clip to be created, endTime (within the source video), and whether or not a slow motion version of the video should also be created. Each of the (non-slomo) clips are also concatenated into single movie file as a highlight reel, in the order listed within the cutsheet.

Cutsheet format:
sourceVideoFile, startTime, entTime, SloMotion

start and end times: "HH:MM:SS"

cutsheet .csv example:
```
filename1.mp4,00:03:45,00:03:55
filename1.mp4,00:07:15,00:07:48
#filename1.mp4,00:07:15,00:07:45
filename2.mp4,00:03:12,00:04:10
filename3.mp4,00:01:10,00:01:43,no
filename3.mp4,00:01:10,00:01:43,yes
```

lines begining with "#" will be ignored \
If the SloMotion field is missing, it is trated as "no"
