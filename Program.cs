using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Collections.Generic;

internal class Program
{
    private const string Reset = "\u001b[0m";
    private const string Cyan = "\u001b[36m";
    private const string Blue = "\u001b[34m";
    private const string Gray = "\u001b[90m";
    private const string White = "\u001b[97m";

    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        if (args.Length > 0)
        {
            switch (args[0].ToLower())
            {
                case "--help":
                case "-h":
                    ShowHelp();
                    return;

                case "--version":
                case "-v":
                    Console.WriteLine("LatFetch 1.0.0");
                    return;

                case "--minimal":
                    ShowMinimal();
                    return;

                case "--network":
                    ShowNetwork();
                    return;

                case "--hardware":
                    ShowHardware();
                    return;

                default:
                    Console.WriteLine($"Unknown option: {args[0]}");
                    Console.WriteLine("Use --help for available options.");
                    return;
            }
        }

        ShowLogo();

        ShowSystem();
        ShowHardware();
        ShowNetwork();
        ShowEnvironment();

        Console.WriteLine();
        Console.WriteLine($"{Gray}LatFetch 1.0.0{Reset}");
    }

    static void ShowLogo()
    {
        Console.WriteLine($"{Cyan}");
        Console.WriteLine("       _        __      __       _       _     ");
        Console.WriteLine("      | |  __ _ \\ \\    / /  ___ | |_ ___| |__  ");
        Console.WriteLine("      | | / _` | \\ \\  / /  / _ \\| __/ __| '_ \\ ");
        Console.WriteLine("      | || (_| |  \\ \\/ /  | (_) | || (__| | | |");
        Console.WriteLine("      |_| \\__,_|   \\__/    \\___/ \\__\\___|_| |_|");
        Console.WriteLine($"{Reset}");
    }

    static void Section(string name)
    {
        Console.WriteLine();
        Console.WriteLine($"{Blue}{name}{Reset}");
    }

    static void Item(string name, string value)
    {
        Console.WriteLine(
            $"  {Cyan}{name,-12}{Reset} {White}{value}{Reset}"
        );
    }

    static void ShowSystem()
    {
        Section("System");

        string username = Environment.UserName;
        string computer = Environment.MachineName;

        string windows =
            RuntimeInformation.OSDescription
                .Replace("Microsoft Windows", "Windows");

        string architecture =
            RuntimeInformation.OSArchitecture.ToString();

        string uptime = FormatUptime(
            TimeSpan.FromMilliseconds(
                Environment.TickCount64
            )
        );

        Item("User", username);
        Item("Computer", computer);
        Item("Windows", windows);
        Item("Architecture", architecture);
        Item("Uptime", uptime);
    }

    static void ShowHardware()
    {
        Section("Hardware");

        string cpu = GetProcessor();
        string memory = GetMemory();
        string storage = GetStorage();
        string gpu = GetGraphics();

        Item("Processor", cpu);
        Item("Memory", memory);
        Item("Graphics", gpu);
        Item("Storage", storage);
    }

    static void ShowNetwork()
    {
        Section("Network");

        NetworkInterface[] interfaces =
            NetworkInterface.GetAllNetworkInterfaces();

        NetworkInterface? active =
            interfaces
                .Where(i =>
                    i.OperationalStatus ==
                    OperationalStatus.Up &&
                    i.NetworkInterfaceType !=
                    NetworkInterfaceType.Loopback)
                .OrderByDescending(
                    i => i.NetworkInterfaceType ==
                         NetworkInterfaceType.Wireless80211)
                .FirstOrDefault();

        if (active == null)
        {
            Item("Adapter", "Not connected");
            return;
        }

        string adapter = active.Name;

        string address = "Unavailable";

        try
        {
            IPInterfaceProperties properties =
                active.GetIPProperties();

            IPAddress? ipv4 =
                properties.UnicastAddresses
                    .Select(x => x.Address)
                    .FirstOrDefault(
                        x => x.AddressFamily ==
                             System.Net.Sockets.AddressFamily.InterNetwork
                    );

            if (ipv4 != null)
                address = ipv4.ToString();
        }
        catch
        {
            // Network information may be unavailable.
        }

        Item("Adapter", adapter);
        Item("Address", address);
        Item("Status", active.OperationalStatus.ToString());
    }

    static void ShowEnvironment()
    {
        Section("Environment");

        Item(
            ".NET",
            Environment.Version.ToString()
        );

        Item(
            "Runtime",
            RuntimeInformation.FrameworkDescription
        );

        Item(
            "Processes",
            GetProcessCount().ToString()
        );

        Item(
            "Directory",
            Environment.CurrentDirectory
        );
    }

    static void ShowMinimal()
    {
        Console.WriteLine($"{Cyan}LatFetch{Reset}");

        Item(
            "User",
            Environment.UserName
        );

        Item(
            "Computer",
            Environment.MachineName
        );

        Item(
            "Windows",
            RuntimeInformation.OSDescription
        );

        Item(
            "CPU",
            GetProcessor()
        );

        Item(
            "Memory",
            GetMemory()
        );
    }

    static void ShowHardwareOnly()
    {
        ShowHardware();
    }

    static void ShowNetwork()
    {
        Section("Network");

        NetworkInterface[] interfaces =
            NetworkInterface.GetAllNetworkInterfaces();

        var activeInterfaces =
            interfaces.Where(i =>
                i.OperationalStatus ==
                OperationalStatus.Up &&
                i.NetworkInterfaceType !=
                NetworkInterfaceType.Loopback);

        bool found = false;

        foreach (NetworkInterface network in activeInterfaces)
        {
            found = true;

            string address = "Unavailable";

            try
            {
                var properties =
                    network.GetIPProperties();

                IPAddress? ipv4 =
                    properties.UnicastAddresses
                        .Select(x => x.Address)
                        .FirstOrDefault(
                            x => x.AddressFamily ==
                                 System.Net.Sockets.AddressFamily.InterNetwork
                        );

                if (ipv4 != null)
                    address = ipv4.ToString();
            }
            catch
            {
            }

            Item("Adapter", network.Name);
            Item("Address", address);
        }

        if (!found)
            Item("Status", "No active connection");
    }

    static string GetProcessor()
    {
        try
        {
            using Process process = new Process();

            process.StartInfo.FileName =
                "powershell.exe";

            process.StartInfo.Arguments =
                "-NoProfile -Command \"(Get-CimInstance Win32_Processor).Name\"";

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;

            process.Start();

            string output =
                process.StandardOutput.ReadToEnd().Trim();

            process.WaitForExit();

            if (!string.IsNullOrWhiteSpace(output))
                return output.Split(
                    Environment.NewLine
                )[0].Trim();
        }
        catch
        {
        }

        return "Unknown";
    }

    static string GetMemory()
    {
        try
        {
            using Process process = new Process();

            process.StartInfo.FileName =
                "powershell.exe";

            process.StartInfo.Arguments =
                "-NoProfile -Command \"Get-CimInstance Win32_OperatingSystem | ForEach-Object { [math]::Round(($_.TotalVisibleMemorySize - $_.FreePhysicalMemory) / 1MB, 2).ToString() + ' GB / ' + [math]::Round($_.TotalVisibleMemorySize / 1MB, 2).ToString() + ' GB' }\"";

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;

            process.Start();

            string output =
                process.StandardOutput.ReadToEnd().Trim();

            process.WaitForExit();

            if (!string.IsNullOrWhiteSpace(output))
                return output;
        }
        catch
        {
        }

        return "Unknown";
    }

    static string GetGraphics()
    {
        try
        {
            using Process process = new Process();

            process.StartInfo.FileName =
                "powershell.exe";

            process.StartInfo.Arguments =
                "-NoProfile -Command \"(Get-CimInstance Win32_VideoController).Name\"";

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;

            process.Start();

            string output =
                process.StandardOutput.ReadToEnd().Trim();

            process.WaitForExit();

            if (!string.IsNullOrWhiteSpace(output))
            {
                string[] cards =
                    output.Split(
                        new[] { '\r', '\n' },
                        StringSplitOptions.RemoveEmptyEntries
                    );

                return cards[0].Trim();
            }
        }
        catch
        {
        }

        return "Unknown";
    }

    static string GetStorage()
    {
        try
        {
            DriveInfo drive =
                new DriveInfo(
                    Path.GetPathRoot(
                        Environment.SystemDirectory
                    )!
                );

            double total =
                drive.TotalSize / 1073741824.0;

            double free =
                drive.AvailableFreeSpace /
                1073741824.0;

            double used =
                total - free;

            return $"{used:F1} GB / {total:F1} GB";
        }
        catch
        {
            return "Unknown";
        }
    }

    static int GetProcessCount()
    {
        try
        {
            return Process.GetProcesses().Length;
        }
        catch
        {
            return 0;
        }
    }

    static string FormatUptime(TimeSpan uptime)
    {
        if (uptime.TotalDays >= 1)
            return $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m";

        if (uptime.TotalHours >= 1)
            return $"{(int)uptime.TotalHours}h {uptime.Minutes}m";

        return $"{uptime.Minutes}m";
    }

    static void ShowHelp()
    {
        Console.WriteLine("LatFetch | lightweight Windows system information tool");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  LatFetch.exe [option]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -h, --help       Show help");
        Console.WriteLine("  -v, --version    Show version");
        Console.WriteLine("  --minimal        Show compact information");
        Console.WriteLine("  --network        Show network information");
        Console.WriteLine("  --hardware       Show hardware information");
    }
}
