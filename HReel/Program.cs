using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace HReel
{
    public class Clip
    {
        public string InputFileName;
        public string StartTime;
        public string EndTime;
        public string Duration;
        public string OutFile;
        public bool SloMo;
        public string CommandText;
    }

    class Program
    {
        public static List<Clip> ClipList = new List<Clip>();
        public static StringBuilder ConcatFileList = new StringBuilder();
        public static string SourceVideoPath = "";
        public static string ClipDestinationPath = "";
        public static string OutFileBaseName = "";
        public static string ConcatFileListFN = "concatFileList";
        public static string FileExtension;

        static void Main(string[] args)
        {
            string usageString = GetUsageString();

            if (args.Length < 1)
            {
                Console.WriteLine("too few arguments.");
                Console.WriteLine("");
                Console.WriteLine(usageString);
                return;
            }
            else if (args[0] == "-help" || args[0] == "-h")
            {
                Console.WriteLine(usageString);
                return;
            }

            if (args.Length >= 3)
                ProcessOptionalArgs(args[1], args[2]);

            if (args.Length >= 5)
                ProcessOptionalArgs(args[3], args[4]);

            if (args.Length == 7)
                ProcessOptionalArgs(args[5], args[6]);

            if (args.Length > 7)
                Console.WriteLine("Only the first 7 args were parsed, all else was ignored.");


            if (!ProcessCutSheet(args[0]))
            {
                Console.WriteLine("Failure during cut sheet parsing.");
                return;
            }

            Console.WriteLine();
            Console.WriteLine("Number of clips to generate: " + ClipList.Count.ToString());

            Console.WriteLine("Do you want to generate " + ClipList.Count.ToString() + " video clips, plus 1 combined movie?  (Y/y) ");
            string KeyPress = Console.ReadLine();

            if (KeyPress != "Y" && KeyPress != "y")
            {
                Console.WriteLine("ok, nothing generated. Commands would be: ");
                Console.WriteLine();

                foreach (Clip item in ClipList)
                {
                    Console.WriteLine("ffmpeg.exe" + item.CommandText);
                }
                Console.WriteLine();
                Console.WriteLine("Concat File: ");
                Console.WriteLine(ConcatFileList.ToString());

                return;
            }

            foreach (Clip item in ClipList)
            {
                GenerateClip(item);
            }

            File.WriteAllText((ConcatFileListFN + ".txt"), ConcatFileList.ToString());
            ConcatFileList.Clear();

            // Generate Highlight Reel from Clips, example cmdline: ffmpeg -f concat -safe 0 -i fileList.txt -c copy compilation.mp4

            string HighlightReelFN = "";

            if (OutFileBaseName == "")
                HighlightReelFN = "HReel" + FileExtension;
            else
                HighlightReelFN = OutFileBaseName + "-Reel" + FileExtension;

            string cmdText = " -f concat -safe 0 -i " + ConcatFileListFN + ".txt" + " -c copy " + HighlightReelFN;
            Console.WriteLine();
            Console.WriteLine("Generating compilation: ffmpeg.exe" + cmdText);

            using (Process process = new Process())
            {
                process.StartInfo.FileName = "ffmpeg.exe";
                process.StartInfo.Arguments = cmdText;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.Start();
                process.WaitForExit();
                process.Dispose();
            }
        }


        static void GenerateClip(Clip newClip)
        {
            Console.WriteLine("executing: ffmpeg.exe" + newClip.CommandText);

            using (Process process = new Process())
            {
                process.StartInfo.FileName = "ffmpeg.exe";
                process.StartInfo.Arguments = newClip.CommandText;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.Start();
                process.WaitForExit();
                process.Dispose();
            }


            if (newClip.SloMo)
            {
                // for slomo, add "-an" to ensure there's no audio
                // ffmpeg -i input.mp4 -an -filter:v "setpts=2.0*PTS" output.mp4

                string baseName = newClip.OutFile.Substring(0, newClip.OutFile.LastIndexOf("."));

                StringBuilder cmdText = new StringBuilder();

                cmdText.Append(" -i " + newClip.OutFile);
                cmdText.Append(" -an");
                cmdText.Append(" -filter:v \"setpts=2.0*PTS\"");
                cmdText.Append(" " + baseName + "-slomo" + FileExtension);

                Console.WriteLine("SloMo Gen: " + "ffmpeg.exe" + cmdText.ToString());

                using (Process process = new Process())
                {
                    process.StartInfo.FileName = "ffmpeg.exe";
                    process.StartInfo.Arguments = cmdText.ToString();
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    process.Start();
                    process.WaitForExit();
                    process.Dispose();
                }
            }
        }


        static bool ProcessCutSheet(string inputFile)
        {
            string _InputFileName;
            string _StartTime;
            string _EndTime;
            string _Duration;
            bool _SloMo = false;

            if (!File.Exists(inputFile))
            {
                Console.WriteLine("File not found: " + inputFile);
                return false;
            };

            int i = 0;
            string outFileName;
            StringBuilder cmdTxt = new StringBuilder();

            foreach (string line in File.ReadLines(inputFile))
            {

                if (line.Length > 1)
                {
                    if (line.Substring(0, 1) == "#")
                        continue;
                }

                string[] parts = line.Split(',');
                if (parts.Length < 3)
                {
                    Console.WriteLine("CutSheet parse error on this line: ");
                    Console.WriteLine(line);
                    return false;
                }

                if (parts.Length > 3)
                {
                    if (parts[3] == "yes" || parts[3] == "Yes" || parts[3] == "true" || parts[3] == "True")
                        _SloMo = true;
                }


                _InputFileName = parts[0]; 
                _StartTime = parts[1];
                _EndTime = parts[2];


                int temp = _InputFileName.LastIndexOf(".");

                string _fileExtension = _InputFileName.Substring(temp, (_InputFileName.Length - temp));
                Console.WriteLine("File Extension: " + _fileExtension + Environment.NewLine);

                if (i == 0) // first time through
                    FileExtension = _fileExtension;
                else
                {
                    if (FileExtension != _fileExtension)
                    {
                        Console.WriteLine("All files in cutsheet must be of the same type");
                        return false;
                    }
                }

                i++;

                if (OutFileBaseName == "")
                    outFileName = "Clip-" + i.ToString() + FileExtension;
                else
                    outFileName = OutFileBaseName + "-" + i.ToString() + FileExtension;

                // example cmdline: ffmpeg.exe -ss 00:00:15 -t 00:01:26 -i input.mp4 -acodec copy -vcodec copy output.mp4
                // _CommandText = " -ss " + _StartTime + " -t " + _EndTime + " -i " + _InputFileName + " -acodec copy -vcodec copy " + outFileName;

                if (SourceVideoPath != "")
                    _InputFileName = SourceVideoPath + "\\" + _InputFileName;

                if (ClipDestinationPath != "")
                    outFileName =  ClipDestinationPath + "\\" + outFileName;

                Clip newCut = new Clip { InputFileName = _InputFileName, StartTime = _StartTime, EndTime = _EndTime, OutFile = outFileName, SloMo = _SloMo, CommandText = "" };

                string VerifyNewCutResults = VerifyNewCut(newCut);

                if (VerifyNewCutResults != "")
                {
                    Console.WriteLine(VerifyNewCutResults);
                    return false;
                }

                // cuts are specified as start/end times, however ffmpeg requires startTime and duration
                _Duration = DetermineDuration(_StartTime, _EndTime);

                if (_Duration == "")
                {
                    Console.WriteLine("Error computing clip duration for: " + _InputFileName + "  " + _StartTime + "  " + _EndTime);
                    return false;
                }

                cmdTxt.Append(" -y");                       // overwrites output file if exists
                cmdTxt.Append(" -ss " + _StartTime);
                cmdTxt.Append(" -t " + _Duration);
                cmdTxt.Append(" -i " + _InputFileName);
                cmdTxt.Append(" -acodec copy -vcodec copy ");
                cmdTxt.Append(outFileName);

                newCut.Duration = _Duration;
                newCut.CommandText = cmdTxt.ToString();
                cmdTxt.Clear();

                ClipList.Add(newCut);
                ConcatFileList.Append("file '" + outFileName + "'" + Environment.NewLine);
            }

            return true;
        }


        static string DetermineDuration(string startTime, string endTime)
        {
            // HH:MM:SS
            int yy = 2000; // any year, month, day will work, just needs to be the same for both start/end
            int mm = 1;
            int dd = 1;

            int startHH;
            int startMM;
            int startSS;

            int endHH;
            int endMM;
            int endSS;

            if (!Int32.TryParse((startTime.Substring(0, 2)), out startHH))
                return "";

            if (!Int32.TryParse((startTime.Substring(3, 2)), out startMM))
                return "";

            if (!Int32.TryParse((startTime.Substring(6, 2)), out startSS))
                return "";

            if (!Int32.TryParse((endTime.Substring(0, 2)), out endHH))
                return "";

            if (!Int32.TryParse((endTime.Substring(3, 2)), out endMM))
                return "";

            if (!Int32.TryParse((endTime.Substring(6, 2)), out endSS))
                return "";

            DateTime startDate = new DateTime(yy, mm, dd, startHH, startMM, startSS);
            DateTime endDate = new DateTime(yy, mm, dd, endHH, endMM, endSS);

            TimeSpan dur = endDate - startDate;

            return dur.ToString("hh':'mm':'ss");
        }

        static string VerifyNewCut(Clip newCut)
        {
            string returnString = "";

            if (!File.Exists(newCut.InputFileName))
                returnString += "Source video not found: " + newCut.InputFileName + Environment.NewLine;

            if (!CheckTimeParameter(newCut.StartTime))
                returnString += "Invalid Start Time : " + newCut.StartTime + Environment.NewLine;

            if (!CheckTimeParameter(newCut.EndTime))
                returnString += "Invalid End Time : " + newCut.EndTime + Environment.NewLine;

            return returnString;
        }


        static bool CheckTimeParameter(string inputString)
        {
            // expecting NN:NN:NN
            // integer checks on N occur elsewhere

            string[] parts = inputString.Split(':');

            if (parts.Length != 3)
                return false;

            if (parts[0].Length != 2 || parts[1].Length != 2 || parts[2].Length != 2)
                return false;

            return true;
        }


        static void ProcessOptionalArgs(string argName, string argValue)
        {
            if (argName == "-vpath") // specifies source video path
            {
                if (Directory.Exists(argValue))
                    SourceVideoPath = argValue;
                else
                    Console.WriteLine("Source video path not found: " + argValue);
            }

            else if (argName == "-cpath") // specifies clip destination path
            {
                if (Directory.Exists(argValue))
                    ClipDestinationPath = argValue;
                else
                    Console.WriteLine("Clip destination path not found: " + argValue);
            }

            else if (argName == "-ofbn") // specifies base name of the generated clip files
            {
                OutFileBaseName = argValue;
                ConcatFileListFN += "-" + argValue;
            }

            else
            {
                Console.WriteLine("Unrecognized parameter was ignored: " + argName + " " + argValue);
            }

        }
        

        static string GetUsageString()
        {
            StringBuilder sbUsage = new StringBuilder();
            sbUsage.Append("\r\n HReel.exe CutSheetFileName   file must be in .csv format: source video, start time, end time, \"yes\" for slomo");
            sbUsage.Append("\r\n");
            sbUsage.Append("\r\n Optional Parameters:");
            sbUsage.Append("\r\n    -vpath PATH    set path to the source videl files; set to current directory when not specified");
            sbUsage.Append("\r\n    -cpath PATH    set path for the generated video clip files; set to current directory when not specified");
            sbUsage.Append("\r\n    -ofbn STRING   set the base for file names of generated video clip files: STRING-N.mp4");

            //sbUsage.Append("csv cutsheet format: ");
            //sbUsage.Append("\r\n  00:00:00 ");

            return sbUsage.ToString();
        }


    }
}
