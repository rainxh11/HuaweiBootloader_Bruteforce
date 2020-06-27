using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HuaweiBootloader_Bruteforce.Properties;

namespace HuaweiBootloader_Bruteforce
{
    class Program
    {
        /// --------------------------------------------------------------------------------------------------------------------------------------////
        static long imei;
        static List<long> generatedOemList = new List<long>();
        /// --------------------------------------------------------------------------------------------------------------------------------------////
        private static void ExtractResource(byte[] resFile, string resFileOutDir)
        {
            using (FileStream fileStream = new FileStream(resFileOutDir + "fastboot.zip", FileMode.Create))
                fileStream.Write(resFile, 0, resFile.Length);
            try
            {
                ZipFile.ExtractToDirectory(resFileOutDir + "fastboot.zip", "fastboot");
                File.Delete(resFileOutDir + "fastboot.zip");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        /// --------------------------------------------------------------------------------------------------------------------------------------////
        public static void SaveUntestedCodes()
        {
            while (true)
            {
                try
                {
                    WriteToCodeTextFile(ref generatedOemList);

                }
                catch { }
                Thread.Sleep(30000);
            }
        }
        /// --------------------------------------------------------------------------------------------------------------------------------------////
        public static void WriteToCodeTextFile(ref List<long> generatedOemList) 
        {
            using (TextWriter tw = new StreamWriter(AppContext.BaseDirectory + $"oemcodes_{imei}.txt"))
            {
                foreach (long s in generatedOemList)
                    tw.WriteLine(s.ToString());
            }
        }
        /// --------------------------------------------------------------------------------------------------------------------------------------////
        public static void ExecuteFastBootUnlock(long oemCode, ref bool success, ref List<long> generatedOemList, string fastbooDir)
        {
            if(fastbooDir == null)
            {
                fastbooDir = AppContext.BaseDirectory + @"fastboot\";
            }
            string output = "";
            Process fastBootExe = new Process();

            fastBootExe.StartInfo.FileName = fastbooDir + "fastboot.exe";
            fastBootExe.StartInfo.Arguments = $"oem unlock {oemCode}";
            fastBootExe.StartInfo.CreateNoWindow = true;
            fastBootExe.StartInfo.UseShellExecute = false;
            fastBootExe.StartInfo.RedirectStandardOutput = true;
            fastBootExe.StartInfo.RedirectStandardError = true;

            try
            {
                fastBootExe.Start();
                StreamReader readerStdError = fastBootExe.StandardError;
                StreamReader readerStdOutput = fastBootExe.StandardError;
                output = readerStdError.ReadToEnd() + readerStdOutput.ReadToEnd();
                fastBootExe.WaitForExit();
            }
            catch
            {
            }
            Console.WriteLine(output);

            if (output.Contains("sucess"))
            {
                File.WriteAllText(AppContext.BaseDirectory + $"oemcode_successful_{imei}.txt", $"OEM Unlock Code for IMEI={imei} : {oemCode}");
                success = true;
            }
            if (output.Contains("Invalid key"))
            {
                int i = generatedOemList.IndexOf(oemCode);
                try
                {
                    generatedOemList.RemoveAt(i);

                }
                catch { }
            }
        }
        /// --------------------------------------------------------------------------------------------------------------------------------------////
        public static int CheckLuhn(string imei)
        {
            int sum = 0;
            bool alternate = false;
            for (int i = imei.Length - 1; i >= 0; i--)
            {
                char[] nx = imei.ToArray();
                int n = int.Parse(nx[i].ToString());

                if (alternate)
                {
                    n *= 2;

                    if (n > 9)
                    {
                        n = (n % 10) + 1;
                    }
                }
                sum += n;
                alternate = !alternate;
            }
            return (sum % 10);
        }
        /// --------------------------------------------------------------------------------------------------------------------------------------////

        public static long IncrementChecksum(ref long oemCode, long checksum, long imei)
        {
            oemCode += (long)(checksum + Math.Sqrt(imei) * 1024);
            return oemCode;
        }
        /// --------------------------------------------------------------------------------------------------------------------------------------////

        static void Main(string[] args)
        {
            byte[] fastbootZipFile = Resources.Fastboot;
            long oemCode = 1000000000000000;
            string currentDir = AppContext.BaseDirectory;

            if (!Directory.Exists(currentDir + @"fastboot\"))
            {
                try
                {
                    Directory.CreateDirectory(currentDir + @"fastboot");
                }
                catch { }
            }
            ExtractResource(fastbootZipFile, currentDir + @"fastboot\");
            string fastbootFolder = currentDir + @"fastboot\";

            if(File.Exists(currentDir + "fastboot.exe"))
            {
                fastbootFolder = currentDir;
            }

            if(args.Length != 0)
            {
                imei = Int64.Parse(args[0]);
            }
            else
            {
                Console.Write("Enter IMEI Code = ");
                imei = Int64.Parse(Console.ReadLine());
            }

            while(imei.ToString().Length != 15)
            {
                Console.Clear();
                Console.Write("Enter IMEI Code = ");
                imei = Int64.Parse(Console.ReadLine());
            }

            long checksum = (long)CheckLuhn(imei.ToString());
            bool finish = false;

            /// --------------------------------------------------------------------------------------------------------------------------------------////

            if (File.Exists(AppContext.BaseDirectory + $"oemcodes_{imei}.txt"))
            {
                string[] lines = File.ReadAllLines(AppContext.BaseDirectory + $"oemcodes_{imei}.txt");

                foreach (string s in lines)
                {
                    generatedOemList.Add(Int64.Parse(s));
                }
            }
            else
            {
                while (finish != true)
                {
                    if (oemCode.ToString().Length == 16)
                    {
                        generatedOemList.Add(IncrementChecksum(ref oemCode, checksum, imei));
                    }
                    else
                    {
                        finish = true;
                        WriteToCodeTextFile(ref generatedOemList);
                    }
                }
            }
            /// --------------------------------------------------------------------------------------------------------------------------------------////


                int count = 0;
                int countMax = generatedOemList.Count;
                bool success = false;

                Thread savingThread = new Thread(SaveUntestedCodes);
                savingThread.Start();

                Parallel.ForEach(generatedOemList, new ParallelOptions { MaxDegreeOfParallelism = 16 }, (long currentOem) =>
                {
                    if (success == false)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Processing {currentOem} on thread {Thread.CurrentThread.ManagedThreadId}");
                        Console.ResetColor();
                        count += 1;
                        Console.Title = $"Processing: {count} / {countMax}";

                        ExecuteFastBootUnlock(currentOem, ref success, ref generatedOemList, fastbootFolder);
                    }
                    else
                    {
                        savingThread.Abort();
                    }
                });
            /// --------------------------------------------------------------------------------------------------------------------------------------////
            try
            {
                Directory.Delete(currentDir + "fastboot", true);
            }
            catch { }
            Console.ReadKey();
            }
        }
    }
