using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Postulate
{
    public class ConnectionStringReference
    {
        [XmlIgnore]
        public string Name { get; set; }

        public string Filename { get; set; }
        public string XPath { get; set; }
        public DataProtectionScope? Encryption { get; set; }

        public void Save()
        {
            string fileName = GetFilename(Name);
            string folder = Path.GetDirectoryName(fileName);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            using (StreamWriter writer = File.CreateText(fileName))
            {
                XmlSerializer xs = new XmlSerializer(typeof(ConnectionStringReference));
                xs.Serialize(writer, this);
            }
        }

        public static string GetFilename(string name)
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Connection String Ref", name.Trim() + ".xml");
        }

        public static string Resolve(string name)
        {
            string fileName = GetFilename(name);
            if (!File.Exists(fileName)) return null;

            return ResolveFromFile(fileName);
        }

        public static string ResolveFromFile(string fileName)
        {
            ConnectionStringReference cnRef = null;
            using (StreamReader reader = File.OpenText(fileName))
            {
                XmlSerializer xs = new XmlSerializer(typeof(ConnectionStringReference));
                cnRef = (ConnectionStringReference)xs.Deserialize(reader);
            }

            if (!File.Exists(cnRef.Filename)) return null;

            XmlDocument doc = new XmlDocument();            
            doc.Load(cnRef.Filename);            

            string result = doc.SelectSingleNode(cnRef.XPath).InnerText;

            if (cnRef.Encryption.HasValue)
            {
                byte[] encryptedBytes = Convert.FromBase64String(result);
                byte[] clearBytes = ProtectedData.Unprotect(encryptedBytes, null, cnRef.Encryption.Value);
                result = Encoding.ASCII.GetString(clearBytes);
            }

            return result;
        }
    }

    public interface IConnectionStringReferenceBuilder
    {
        ConnectionStringReference GetConnectionStringReference(string name = null);
    }
}