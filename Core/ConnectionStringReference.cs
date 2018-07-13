using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Postulate
{
	/// <summary>
	/// Represents an XML file in ~/App Data/Local/Connection String Ref that stores the location of an XML file that contains a connection string.
	/// This allows you to avoid embedding a connection string your app.config.
	/// </summary>
	public class ConnectionStringReference
	{
		[XmlIgnore]
		public string Name { get; set; }

		/// <summary>
		/// Full path of an XML file where a connection string is stored
		/// </summary>
		public string Filename { get; set; }

		/// <summary>
		/// Location within the XML file that stores a connection string
		/// </summary>
		public string XPath { get; set; }

		/// <summary>
		/// Indicates the type of encryption (if any) on the file containing the connection string
		/// </summary>
		public DataProtectionScope? Encryption { get; set; }

		/// <summary>
		/// Creates a ConnectionStringReference for a given connection string with optional encryption.
		/// </summary>
		/// <param name="name">Name of XML file (without path) that will be created in ~/App Data/Local/Connection String Ref</param>
		/// <param name="connectionString">Connection string to reference, will be stored in ~/My Documents/Connection Strings/{Name}.xml</param>
		/// <param name="encryption">Data protection scope of connection string</param>
		public static string Create(string name, string connectionString, DataProtectionScope? encryption = null)
		{
			string refFile = GetFilename(name);
			if (File.Exists(refFile)) throw new Exception($"Can't overwrite existing Connection String Reference file {refFile}");

			string settingsFile = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
				"Connection Strings", name + ".xml");
			string folder = Path.GetDirectoryName(settingsFile);
			if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

			var cnSettings = new ConnectionStringSettingsFile()
			{
				ConnectionString = (encryption.HasValue) ? EncryptString(connectionString, encryption.Value) : connectionString,
			};

			using (StreamWriter writer = File.CreateText(settingsFile))
			{
				XmlSerializer xs = new XmlSerializer(typeof(ConnectionStringSettingsFile));
				xs.Serialize(writer, cnSettings);
			}

			ConnectionStringReference cnRef = new ConnectionStringReference()
			{
				Name = name,
				Filename = settingsFile,
				XPath = "/Settings/ConnectionString"
			};

			if (encryption.HasValue) cnRef.Encryption = encryption.Value;

			cnRef.Save();

			return settingsFile;
		}

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

			if (cnRef.Encryption.HasValue) result = DecryptString(result, cnRef.Encryption.Value);

			return result;
		}

		private static string EncryptString(string input, DataProtectionScope scope)
		{
			byte[] clearBytes = Encoding.ASCII.GetBytes(input);
			byte[] encryptedBytes = ProtectedData.Protect(clearBytes, null, scope);
			return Convert.ToBase64String(encryptedBytes);
		}

		private static string DecryptString(string input, DataProtectionScope scope)
		{
			byte[] encryptedBytes = Convert.FromBase64String(input);
			byte[] clearBytes = ProtectedData.Unprotect(encryptedBytes, null, scope);
			return Encoding.ASCII.GetString(clearBytes);
		}
	}

	public interface IConnectionStringReferenceBuilder
	{
		ConnectionStringReference GetConnectionStringReference(string name = null);
	}

	[XmlRoot(ElementName = "Settings")]
	public class ConnectionStringSettingsFile
	{
		public string ConnectionString { get; set; }
	}
}