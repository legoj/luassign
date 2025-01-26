using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO;

namespace luassign
{

    class Program
    {
        const string _TMPFILEXT = ".dat";
        const string _TMPFNDSP1 = "DISP1.rsdm";
        const string _TMPFNDSP2 = "DISP2.rsdm";
        const string _TMPFNPRT1 = "PRT1.rspm";
        const string _BASEXNODE = "RumbaProfile";
        const string _BDATANODE = "BinaryData";

        const uint _IDXD1PORT = 46084;
        const uint _IDXD1DEVN = 47711;
        const uint _IDXD2PORT = 17412;
        const uint _IDXD2DEVN = 19039;
        const uint _IDXP1PORT = 40964;
        const uint _IDXP1DEVN = 45663;

        //private static string rdsmFilePath = null;
        //private static string binDatBase64 = null;
        //private static byte[] binDatNative = null;
        //const string _BASEXMLDOC = "RumbaProfile";
        //const string _BINARYDATA = "BinaryData";
        //const string _TMPBINDSP1 = "DISP1.rdsm.dat";
        //const string _TMPBINDSP2 = "DISP2.rdsm.dat";
        //const string _TMPBINPRT1 = "PRT1.rspm.dat";
        //const string _DISP1FILE = "DISP1.rsdm";
        //const string _DISP2FILE = "DISP2.rsdm";
        //const string _PRINTFILE = "PRT1.rspm";
        //const uint _PRPORTIDX = 40964;
        //const uint _PRDEVNIDX = 45663;  
        //const uint _D1PORTIDX = 48644; //BE06
        //const uint _D1DEVNIDX = 33887; //1st C65F 50783   //2nd 645F 338877

        static string _APPLOC = AppDomain.CurrentDomain.BaseDirectory;

        //case 1: paramCount=1: sessionFile --> dump bin
        //case 2: paramCount=2: sessionFile|binFile, patternSearch --> data search & return last match index
        //case 3: paramCount=3: sessionFile, portNumber, deviceName --> create session file
        //case 4: paramCount=4: outDirectory, portNumber, deviceName, printName --> 2LU
        //case 5: paramCount=5: outDirectory, portNumber, deviceName, device2Name, printName --> 3LU

        static int Main(string[] args)
        {
            if (args.Length == 0) return Help();

            switch (args.Length)
            {
                case 1:
                    return DumpBinaryDataToFile(args[0]);
                case 2:
                    return LocateTextOffsetInData(args[0], args[1]);
                case 3:
                    return CreateSessionFile(args[0], args[1], args[2]);
                case 4:
                    return Create2LUSessionFiles(args[0], args[1], args[2], args[3]);
                case 5:
                    return Create3LUSessionFiles(args[0], args[1], args[2], args[3], args[4]);
                default:
                    return Error("Too many parameters specified! ParamCount="+  args.Length);
            }
        }
        private static int DumpBinaryDataToFile(string sessionFilePath)
        {
            if (!File.Exists(sessionFilePath))
                return Error(sessionFilePath + " does not exist!");

            XmlDocument xmlDoc = new XmlDocument();
            try
            {
                xmlDoc.Load(sessionFilePath);
                XmlNode dat = xmlDoc.SelectSingleNode("/" + _BASEXNODE + "/" + _BDATANODE);
                if (dat != null)
                {
                    File.WriteAllBytes(sessionFilePath + _TMPFILEXT, Convert.FromBase64String(dat.InnerText));
                    return 0;
                }
                return Error(_BDATANODE + " not found in the specified session file!");
            }
            catch(Exception e)
            {
                return Error("XML loading error occurred! " + e.Message + " \rn"+ e.StackTrace );
            }
        }
        private static int LocateTextOffsetInData(string sessionFilePath, string searchText)
        {
            if (!File.Exists(sessionFilePath))
                return Error(sessionFilePath + " does not exist!");
            string binFilePath = sessionFilePath;
            if (Path.GetExtension(sessionFilePath).StartsWith("rs") || IsUTF8File(sessionFilePath))
            {
                DumpBinaryDataToFile(sessionFilePath);
                binFilePath = sessionFilePath + _TMPFILEXT;
            }
            byte[] btTmpBinData = File.ReadAllBytes(binFilePath);

            byte[] btSearchText = null;
            ushort pN;
            if (ushort.TryParse(searchText, out pN))
            {
                btSearchText = BitConverter.GetBytes(pN);
            }else
            {
                btSearchText = Encoding.ASCII.GetBytes(searchText);
            }
            int idx = FindBytePattern(btTmpBinData, btSearchText);
            if (idx == -1)
                Print("SearchText: " + searchText + " not found!");
            else
                Print("SearchText: " + searchText + " Offset: " + idx);
            return 0;
        }
        private static int CreateSessionFile(string sessionFilePath, string portNumber, string deviceName)
        {
            ushort ptNumber;
            if(!ushort.TryParse(portNumber, out ptNumber))
                return Error("Invalid port number: " + portNumber + ". Value must be a number.");

            if(ptNumber < 1 || ptNumber > ushort.MaxValue)
                return Error("Port number out-of-range: " + portNumber + ". Valid range is 1-" + ushort.MaxValue + ".");

            if (deviceName.Length < 1 || deviceName.Length > 16)
                return Error("Invalid device name: " + deviceName + ". Value should be 1-16 chars.");

            try
            {
                byte[] binData = GenerateBinaryData(sessionFilePath, ptNumber, deviceName);
                string b64Data = Convert.ToBase64String(binData);
                string orgFile = _APPLOC + Path.GetFileName(sessionFilePath); 
                if (File.Exists(sessionFilePath))
                {
                    orgFile = sessionFilePath + "_" + String.Format("{0:yyyyMMddHHmmss}", DateTime.Now);
                    File.Move(sessionFilePath, orgFile);
                }
                SaveSessionProfile(orgFile, sessionFilePath, b64Data);
                return 0;
            }catch(Exception e)
            {
                return Error("Template file generation error! " + e.Message + " \rn" + e.StackTrace);
            }
        }
        private static int Create2LUSessionFiles(string outDirectory, string portNumber, string dspDeviceName, string prtDeviceName)
        {
            if (!Directory.Exists(outDirectory))
                return Error("Directory does not exist! " + outDirectory);
            int ret = CreateSessionFile(outDirectory + "\\" + _TMPFNDSP1, portNumber, dspDeviceName);
            if (ret != 0)
                return Error("Error on DISP1.rsdm creation!");
            return CreateSessionFile(outDirectory + "\\" + _TMPFNPRT1, portNumber, prtDeviceName);           
        }
        private static int Create3LUSessionFiles(string outDirectory, string portNumber, string dsp1DeviceName, string dsp2DeviceName, string prtDeviceName)
        {
            if (!Directory.Exists(outDirectory))
                return Error("Directory does not exist! " + outDirectory);
            int ret = CreateSessionFile(outDirectory + "\\" + _TMPFNDSP1, portNumber, dsp1DeviceName);
            if (ret != 0)
                return Error("Error on DISP1.rsdm creation!");
            ret = CreateSessionFile(outDirectory + "\\" + _TMPFNDSP2, portNumber, dsp2DeviceName);
            if (ret != 0)
                return Error("Error on DISP2.rsdm creation!");
            return CreateSessionFile(outDirectory + "\\" + _TMPFNPRT1, portNumber, prtDeviceName);
        }
        private static byte[] GenerateBinaryData(string sessionFilePath, ushort portNumber, string deviceName)
        {
            string tmpFile = Path.GetFileName(sessionFilePath);
            string tmpPath = _APPLOC + tmpFile;
            string datPath = tmpPath + _TMPFILEXT;
            if (!File.Exists(datPath) && File.Exists(tmpPath)) DumpBinaryDataToFile(tmpPath);
            byte[] btTmpBinData = File.ReadAllBytes(datPath);

            uint offsetPortNumber = _IDXD1PORT;
            uint offsetDeviceName = _IDXD1DEVN;
            if (tmpFile.Equals(_TMPFNDSP2, StringComparison.CurrentCultureIgnoreCase))
            {
                offsetPortNumber = _IDXD2PORT;
                offsetDeviceName = _IDXD2DEVN;
            }
            if (tmpFile.Equals(_TMPFNPRT1, StringComparison.CurrentCultureIgnoreCase))
            {
                offsetPortNumber = _IDXP1PORT;
                offsetDeviceName = _IDXP1DEVN;
            }

            byte[] btPortNumber = BitConverter.GetBytes(portNumber);
            for (uint i = 0; i < btPortNumber.Length; i++)
            {
                btTmpBinData[offsetPortNumber + i] = btPortNumber[i];
            }

            byte[] btDeviceName = Encoding.ASCII.GetBytes(deviceName);
            for (uint i = 0; i < btDeviceName.Length; i++)
            {
                btTmpBinData[offsetDeviceName + i] = btDeviceName[i];
            }

            return btTmpBinData;
        }
        private static void SaveSessionProfile(string sessionFile, string newFile, string b64BinData)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(sessionFile);
            XmlNode binDataNode = xmlDoc.SelectSingleNode("/" + _BASEXNODE + "/" + _BDATANODE);
            if (binDataNode != null)
            {
                binDataNode.InnerText = b64BinData;
            }
            xmlDoc.Save(newFile);
        }
        private static int FindBytePattern(byte[] binData, byte[] pattern)
        {
            int c = binData.Length - pattern.Length + 1;
            int j;
            for (int i = 0; i < c; i++)
            {
                if (binData[i] != pattern[0]) continue;
                for (j = pattern.Length - 1; j >= 1 && binData[i + j] == pattern[j]; j--) ;
                if (j == 0) return i;
            }
            return -1;
        }
        private static bool IsUTF8File(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open))
            {
                byte[] bits = new byte[3];
                fs.Read(bits, 0, 3);
                return (bits[0] == 0xEF && bits[1] == 0xBB && bits[2] == 0xBF);
            }
        }
        static int Help()
        {
            Print("[Usage]");
            Print("-------------------");
            Print("Case1: Dump BinaryData to binary file");
            Print(" -generates <sessionFilePath>.dat file containing the binary data");
            Print(" command:");
            Print("   $>luassign <sessionFilePath>");
            Print("");
            Print(" example:");
            Print("   $>luassign \"" + @"C:\Program Files (x86)\Micro Focus\RUMBA\Mframe\DISP1.rsdm" + "\"");
            Print("");
            Print("Case2: Data index search");
            Print(" -returns offset index of the <searchString> in the binary data");
            Print(" command:");
            Print("   $>luassign <sessionFilePath> <searchString>");
            Print("");
            Print(" example:");
            Print("   $>luassign \"" + @"C:\Program Files (x86)\Micro Focus\RUMBA\Mframe\DISP1.rsdm" + "\" R53AA38A");
            Print("");
            Print("Case3: Create session file");
            Print(" -creates a session file based on templates with the specified values");
            Print(" command:");
            Print("   $>luassign <sessionFilePath> <portNumber> <deviceName>");
            Print("");
            Print(" example:");
            Print("   $>luassign \"" + @"C:\Program Files (x86)\Micro Focus\RUMBA\Mframe\DISP1.rsdm" + "\" 3233 R53AA38A");
            Print("");
            Print("Case4: Create the session files for 2LU");
            Print(" -creates session files for 2LU on the specified output directory; existing files are renamed as backup.");
            Print(" command:");
            Print("   $>luassign <outputDirectory> <portNumber> <dsp1Device> <prt1Device>");
            Print("");
            Print(" example:");
            Print("   $>luassign \"" + @"C:\Program Files (x86)\Micro Focus\RUMBA\Mframe" + "\" 3233 R53AA38A R33AA38A");
            Print("");
            Print("Case5: Create the session files for 3LU");
            Print(" -creates session files for 3LU on the specified output directory; existing files are renamed as backup.");
            Print(" command:");
            Print("   $>luassign <outputDirectory> <portNumber> <dsp1Device> <dsp2Device> <prt1Device>");
            Print("");
            Print(" example:");
            Print("   $>luassign \"" + @"C:\Program Files (x86)\Micro Focus\RUMBA\Mframe" + "\" 3233 R53AA38A R63AA38A R33AA38A");
            return -1;
        }
        static void Print(string txt)
        {
            Console.Out.WriteLine(txt);
        }
        static int Error(string err)
        {
            Console.Error.WriteLine(err);
            return -1;
        }
    //    static void DumpSessionFilesBinData() { 
    //        string rmbLoc = @"C:\TEMP\_PKGING\RumbaLU\rumba\3LU\02_3LU";
    //        DumpBinDatToFile(rmbLoc + "\\" + _DISP1FILE);
    //        DumpBinDatToFile(rmbLoc + "\\" + _DISP2FILE);
    //        DumpBinDatToFile(rmbLoc + "\\" + _PRINTFILE);
    //    }
    //    static void Test() { 
    //        //ZEPW10JK1013	3236	R5IPK425	　	R3IPK425	<-- JO
    //        //ZEPW10JJ1070	3236	R5IPE002		R3IPE002
    //        string rmbLoc = @"C:\TEMP\_PKGING\RumbaLU\rumba\sample\ZEPW10JK1013";
    //        string pNumb = "3236"; 
    //        string dName = "R5IPK425";
    //        string pName = "R3IPK425";
    //        UpdateD1RSDMFile(rmbLoc + "\\" + _DISP1FILE , pNumb, dName);
    //        UpdateD1RSDMFile(rmbLoc + "\\" + _DISP2FILE, pNumb, dName);
    //        UpdatePRRSPMFile(rmbLoc + "\\" + _PRINTFILE, pNumb, pName);

    //        //byte[] port = BitConverter.GetBytes((ushort) 3236); //hex: A4 0C    dec: 164 12
    //        //byte[] devN = Encoding.ASCII.GetBytes("R5IPK425");

    //        //byte[] d1 = GetBinDataFromXml(args[0]);
    //        //byte[] d2 = ReadTempBinFile(_TMPBINDSP1);
    //        //_findMarkerIndex(ref d2); 
    //        // Console.Out.WriteLine("IsEqual: " + Byte.Equals(d1,d2));
    //        //DumpBinDatToFile(args[0]);
    //    }
    //    static void UpdateD1RSDMFile(string targetFile, string portNumber, string deviceName)
    //    {

    //        byte[] binData = CreateBinaryData(_TMPBINDSP1, UInt16.Parse(portNumber), _D1PORTIDX, deviceName, _D1DEVNIDX);
    //        string b64Data = Convert.ToBase64String(binData);
    //        string newFile = targetFile.Replace(".rsdm", "_new.rsdm");
    //        WriteXmlProfile(targetFile, newFile, b64Data);
    //        File.WriteAllBytes(newFile + ".bin", binData);
    //    }
    //    static void UpdatePRRSPMFile(string targetFile,string portNumber, string deviceName)
    //    {
    //        byte[] binData = CreateBinaryData(_TMPBINPRT1, UInt16.Parse(portNumber), _PRPORTIDX, deviceName, _PRDEVNIDX);
    //        string b64Data = Convert.ToBase64String(binData);
    //        string newFile = targetFile.Replace(".rspm", "_new.rspm");
    //        WriteXmlProfile(targetFile, newFile, b64Data);
    //        File.WriteAllBytes(newFile + ".bin", binData);
    //    }
    //    static void WriteXmlProfile(string origFile, string newFile, string b64BinData)
    //    {
    //        XmlDocument xmlDoc = new XmlDocument();
    //        xmlDoc.Load(origFile);
    //        XmlNode binDataNode = xmlDoc.SelectSingleNode("/" + _BASEXMLDOC + "/" + _BINARYDATA);
    //        if (binDataNode != null)
    //        {
    //            File.WriteAllBytes(origFile + ".bin", Convert.FromBase64String(binDataNode.InnerText));
    //            binDataNode.InnerText = b64BinData;
    //        }
    //        xmlDoc.Save(newFile);
    //        xmlDoc = null;
    //    }
    //    static byte[] CreateBinaryData(string tmpType, ushort portNumber, uint offsetPortNumber, string deviceName, uint offsetDeviceName)
    //    {
    //        byte[] btTmpBinData = ReadTempBinFile(tmpType);

    //        byte[] btPortNumber = BitConverter.GetBytes(portNumber);            
    //        for(uint i=0; i<btPortNumber.Length; i++)
    //        {
    //            btTmpBinData[offsetPortNumber + i] = btPortNumber[i]; 
    //        }

    //        byte[] btDeviceName = Encoding.ASCII.GetBytes(deviceName);
    //        for (uint i = 0; i < btDeviceName.Length; i++)
    //        {
    //            btTmpBinData[offsetDeviceName + i] = btDeviceName[i];
    //        }

    //        return btTmpBinData;
    //    }
    //    static byte[] ReadTempBinFile(string tmpName)
    //    {
    //        return File.ReadAllBytes(_APPLOC + tmpName);
    //    }

    //    static byte[] GetBinDataFromXml(string rdsmPath) {
    //        using (XmlReader reader = XmlReader.Create(rdsmPath))
    //        {
    //            while (reader.Read())
    //            {
    //                if (reader.IsStartElement())
    //                {
    //                    switch (reader.Name)
    //                    {
    //                        case _BINARYDATA:
    //                            if (reader.Read())
    //                            {
    //                                string b64Data = reader.Value;
    //                                return Convert.FromBase64String(b64Data);
    //                            }
    //                            break;
    //                    }
    //                }
    //            }
    //        }
    //        return null;
    //    }
    //    static void DumpBinDatToFile(string sessFile)
    //    {
    //        if (!File.Exists(sessFile)) return; 
    //        byte[] dat = GetBinDataFromXml(sessFile);
    //        if(dat!=null) File.WriteAllBytes(sessFile + ".bin", dat);
    //    }
    //    private static void _findMarkerIndex(ref byte[] dat)
    //    {
    //        for (int i = 0; i < dat.Length - 2; i++)
    //        {
    //            if (dat[i] == 74 && dat[i + 1] == 80 && dat[i + 2] == 78)
    //            {
    //                Console.Out.WriteLine("JPN: " + i + "  " + dat[i] + dat[i + 1] + dat[i + 2]);
    //            }
    //            if (dat[i] == 82 && dat[i + 1] == 53 && dat[i + 2] == 73)                
    //            {
    //                Console.Out.WriteLine("R5I:" + i + "  " + dat[i] + dat[i + 1] + dat[i + 2]);
    //            }
    //            if (dat[i] == 82 && dat[i + 1] == 51 && dat[i + 2] == 73)
    //            {
    //                Console.Out.WriteLine("R3I:" + i + "  " + dat[i] + dat[i + 1] + dat[i + 2]);
    //            }
    //        }
    //    }

    }
}
