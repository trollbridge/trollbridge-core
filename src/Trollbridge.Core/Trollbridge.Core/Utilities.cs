using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using System.Xml.Serialization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO.Compression;
using System.Data.SqlClient;
using System.Data;
using System.Text;
using System.Net.NetworkInformation;
using System.Data.SqlTypes;
using System.Runtime;
using System.Runtime.InteropServices;

namespace Trollbridge.Core
{
    public static class Utilities
    {
        [DllImport("wininet.dll")]
        private extern static bool InternetGetConnectedState(out int flag, int reserved);

        public static bool IsInternetAvailable()
        {
            int flag = 0;
            return InternetGetConnectedState(out flag, 0);
        }

        public static string GetStringValue(DateTime? value, string format)
        {
            if (value == null) return "";
            return ((DateTime)value).ToString(format);
        }

        public static string SerializeToXmlString(object obj)
        {
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            XmlSerializer ser = new XmlSerializer(obj.GetType());

            ser.Serialize(ms, obj);
            ms.Seek(0, System.IO.SeekOrigin.Begin);
            System.IO.StreamReader tr = new System.IO.StreamReader(ms);

            return tr.ReadToEnd();
        }

        public static object DeserializeXmlString(string xmlString, Type toTypeOf, string typename)
        {

            XmlRootAttribute xRoot = new XmlRootAttribute();
            xRoot.ElementName = typename;
            xRoot.IsNullable = true;

            var xmlSer = new XmlSerializer(toTypeOf, xRoot);
            var stringReader = new StringReader(xmlString);
            return xmlSer.Deserialize(stringReader);
        }

        public static object DeserializeXmlString(string xmlString, Type toTypeOf)
        {
            XmlSerializer mySerializer = new XmlSerializer(toTypeOf);
            MemoryStream ms = new MemoryStream();
            StreamWriter sw = new StreamWriter(ms);

            sw.Write(xmlString);
            sw.Flush();
            ms.Position = 0;
            return mySerializer.Deserialize(ms);
        }

        public static string FormatString(string str, params object[] args)
        {
            return string.Format(System.Globalization.CultureInfo.CurrentUICulture, str, args);
        }

        public static Boolean GetAppSettingsBoolValue(string settingName)
        {
            string settingValue = ConfigurationManager.AppSettings[settingName];
            if (settingValue != null)
            {
                if (settingValue.Equals("true", StringComparison.CurrentCultureIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public static string GetAppSettingsStringValue(string settingName)
        {
            string settingValue = ConfigurationManager.AppSettings[settingName];
            if (settingValue != null)
            {
                return settingValue;
            }
            return "";
        }

        public static string GetAppSettingsDecompressedStringValue(string settingName)
        {
            string settingValue = ConfigurationManager.AppSettings[settingName];
            if (settingValue != null)
            {
                return DecompressString(settingValue);
            }
            return "";
        }

        public static int GetAppSettingsNumericValue(string settingName)
        {
            string settingValue = ConfigurationManager.AppSettings[settingName];
            if (settingValue != null)
            {
                return Convert.ToInt32(settingValue);
            }
            return 0;
        }

        public static string CloudDbConnectionString
        {
            get
            {
                return Utilities.FormatString(GetAppSettingsStringValue("ConnectionString"), GetAppSettingsStringValue("TargetServerName"), GetAppSettingsStringValue("TargetDatabase"), GetAppSettingsStringValue("TargetUserName"), GetAppSettingsStringValue("TargetPassword"));
            }
        }

        public static string GetTextFromFile(string fileToProcess)
        {
            StreamReader srFileToProcess = new StreamReader(fileToProcess, true);
            string txt = srFileToProcess.ReadToEnd();
            srFileToProcess.Close();
            return txt;
        }

        public static string CompressString(string text)
        {
            return Convert.ToBase64String(SerializeAndCompressToByteArray(text));
        }

        public static string DecompressString(string compressedText)
        {
            return DecompressDeserializeByteArray<string>(Convert.FromBase64String(compressedText));
        }

        public static byte[] SerializeAndCompressToByteArray(object objX)
        {
            // First serialize the object into a memory stream
            MemoryStream stream = new MemoryStream();
            BinaryFormatter objBinaryFormat = new BinaryFormatter();
            objBinaryFormat.Serialize(stream, objX);

            // Convert the stream into a byte array
            stream.Position = 0;
            byte[] byteArray = stream.ToArray();

            using (MemoryStream ms = new MemoryStream())
            {
                using (GZipStream sw = new GZipStream(ms, CompressionMode.Compress))
                {
                    sw.Write(byteArray, 0, byteArray.Length); // Compress
                }
                return ms.ToArray(); // Transform byte[] zip data to string
            }
        }

        public static T DecompressDeserializeByteArray<T>(byte[] btarr)
        {
            int dataLength = 0;

            // Prepare for decompress:  What we need to do is get the number of bytes to allocate for a decompressed
            // string.  So do this, we create a memory stream and run it through GZipStream.  Now note that from
            // what I can see, GZipStream closes the memory stream when it is done thus I can't do a ms.Seek or ms.Position
            // to start at the beginning of the stream.
            using (MemoryStream ms = new MemoryStream(btarr))
            {
                using (GZipStream gz = new GZipStream(ms, CompressionMode.Decompress))
                {
                    while (gz.ReadByte() > -1)
                    {
                        ++dataLength;
                    }
                }
            }

            byte[] byteArray = new byte[dataLength];

            // Now that we have the output byte array allocated, we need to start over (due to the note above)

            using (MemoryStream ms = new MemoryStream(btarr))
            {
                using (GZipStream gz = new GZipStream(ms, CompressionMode.Decompress))
                {
                    int rByte = gz.Read(byteArray, 0, dataLength);                               // Decompress
                    using (MemoryStream memoryStream = new MemoryStream(byteArray, 0, rByte))    // Convert the decompressed bytes into a stream
                    {
                        BinaryFormatter BinaryFormatter = new BinaryFormatter();                 // deserialize the stream into an object graph
                        return (T)BinaryFormatter.Deserialize(memoryStream);
                    }
                }
            }
        }

        public static void WriteObjectToFile(object objX, string fileName)
        {
            File.WriteAllText(fileName, SerializeToXmlString(objX));
        }

        public static void CompressObjectToFile(object objX, string fileName)
        {
            File.WriteAllBytes(fileName, SerializeAndCompressToByteArray(objX));
        }

        public static T DecompressObjectFromFile<T>(string fileName)
        {
            return Utilities.DecompressDeserializeByteArray<T>(File.ReadAllBytes(fileName));
        }

        public static void DecompressFile(string inputFile, string outputFile)
        {
            using (FileStream inputFileStream = File.OpenRead(inputFile))
            {
                using (FileStream outputFileFileStream = File.Create(outputFile))
                {
                    using (GZipStream decompressionStream = new GZipStream(inputFileStream, CompressionMode.Decompress))
                    {
                        decompressionStream.CopyTo(outputFileFileStream);
                    }
                }
            }
        }

        public static void CompressFile(string inputFile, string outputFile)
        {
            using (FileStream inputFileStream = File.OpenRead(inputFile))
            {
                using (FileStream outputFileStream = File.Create(outputFile))
                {
                    using (GZipStream compressionStream = new GZipStream(outputFileStream, CompressionMode.Compress))
                    {
                        inputFileStream.CopyTo(compressionStream);
                    }
                }
            }
        }

        public static SqlParameter GetSqlParameter(string name, object value, SqlDbType db)
        {
            SqlParameter param = new SqlParameter(name, db);
            param.Value = value;

            return param;
        }

        public static string GetMACAddress()
        {
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            String sMacAddress = string.Empty;

            foreach (NetworkInterface adapter in nics)
            {
                if (sMacAddress == String.Empty)// only return MAC Address from first card  
                {
                    IPInterfaceProperties properties = adapter.GetIPProperties();

                    sMacAddress = adapter.GetPhysicalAddress().ToString();
                }

            }
            return sMacAddress;
        }
    }
}
