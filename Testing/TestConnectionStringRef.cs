using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postulate;
using System.Security.Cryptography;
using System.IO;

namespace Testing
{
    [TestClass]
    public class TestConnectionStringRef
    {
        [TestMethod]
        public void CreateConnectionStringRef()
        {
            string refFile = ConnectionStringReference.GetFilename("hello");
            if (File.Exists(refFile)) File.Delete(refFile);

            const string sample = "this is a sample connection string";
            var settingsFile = ConnectionStringReference.Create("hello", sample);

            var connectionString = ConnectionStringReference.Resolve("hello");
            Assert.IsTrue(connectionString.Equals(sample));

            File.Delete(settingsFile);
        }

        [TestMethod]
        public void CreateConnectionStringRefEncrypted()
        {
            string refFile = ConnectionStringReference.GetFilename("hello");
            if (File.Exists(refFile)) File.Delete(refFile);

            const string sample = "this is a sample connection string";
            var settingsFile = ConnectionStringReference.Create("hello", sample, DataProtectionScope.CurrentUser);

            var connectionString = ConnectionStringReference.Resolve("hello");
            Assert.IsTrue(connectionString.Equals(sample));

            File.Delete(settingsFile);
        }
    }
}
