using Newtonsoft.Json;
using System;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Hiemdall_bridge.Help
{
    public static class Help
    {
        private const string LicenseFileName = "license.lic";

        public static bool Validate()
        {
            try
            {
                string licensePath = GetLicensePath();

                if (!File.Exists(licensePath))
                    return false;

                string encrypted = File.ReadAllText(licensePath);

                string json = CryptoHelper.Decrypt(encrypted);

                var lic = JsonConvert.DeserializeObject<LicenseInfo>(json);

                if (lic == null)
                    return false;

                string currentHardware = HardwareHelper.GetHardwareId();

                if (lic.HardwareId != currentHardware)
                    return false;

                if (lic.SecretKey != "SchaefflerSecretKey2024!@#$%")
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string GetLicensePath()
        {
            // License file next to EXE
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LicenseFileName);
        }
    }
    public class LicenseInfo
    {
        public string HardwareId { get; set; }
        public string SecretKey { get; set; }
        public string Customer { get; set; }
    }
    public static class HardwareHelper
    {
        public static string GetHardwareId()
        {
            string motherboard = GetMotherboardSerial();
            string disk = DiskHelper.GetSystemDriveSerial();
            string machine = Environment.MachineName;

            string raw = $"{motherboard}|{disk}|{machine}";
            return Hash(raw);
        }

        private static string GetMotherboardSerial()
        {
            try
            {
                var searcher =
                   new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard");

                foreach (ManagementObject obj in searcher.Get())
                {
                    return obj["SerialNumber"]?.ToString() ?? "";
                }
            }
            catch { }

            return "";
        }

        private static string Hash(string input)
        {
            var sha = SHA256.Create();

            return Convert.ToBase64String(
                sha.ComputeHash(Encoding.UTF8.GetBytes(input)));
        }
    }

    public static class DiskHelper
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetVolumeInformation(
            string lpRootPathName,
            StringBuilder lpVolumeNameBuffer,
            int nVolumeNameSize,
            out uint lpVolumeSerialNumber,
            out uint lpMaximumComponentLength,
            out uint lpFileSystemFlags,
            StringBuilder lpFileSystemNameBuffer,
            int nFileSystemNameSize);

        public static string GetSystemDriveSerial()
        {
            uint serial, maxCompLen, flags;

            var volName = new StringBuilder(261);
            var fsName = new StringBuilder(261);

            bool ok = GetVolumeInformation(
                Path.GetPathRoot(Environment.SystemDirectory),
                volName,
                volName.Capacity,
                out serial,
                out maxCompLen,
                out flags,
                fsName,
                fsName.Capacity);

            return ok ? serial.ToString("X") : "";
        }
    }

    public static class CryptoHelper
    {
        private static readonly string SecretKey = "SHOPFLOOR_SECRET_123";

        public static string Decrypt(string cipherText)
        {
            var parts = cipherText.Split('|');

            byte[] iv = Convert.FromBase64String(parts[0]);
            byte[] data = Convert.FromBase64String(parts[1]);

            using var aes = Aes.Create();
            using var sha = SHA256.Create();

            aes.Key = sha.ComputeHash(Encoding.UTF8.GetBytes(SecretKey));
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();

            byte[] decrypted = decryptor.TransformFinalBlock(data, 0, data.Length);

            return Encoding.UTF8.GetString(decrypted);
        }
    }
}