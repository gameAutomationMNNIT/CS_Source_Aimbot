using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CSS_Aimbot_iOSDev
{

    public partial class Aimbot_iOSDev : Form
    {
        int toggle = 1;
        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, int lpBaseAddress, byte[] lpBuffer, uint nSize, out int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hObject);

        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VMOperation = 0x00000008,
            VMRead = 0x00000010,
            VMWrite = 0x00000020,
            DupHandle = 0x00000040,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            Synchronize = 0x00100000
        }

        int serverModuleAddress = 0, engineModuleAddress = 0;
        int playerBaseAddress = 0x54AB70;
        int enemyBaseAddress = 0x54AB70 + 0x24;
        int angleYAddress = 0x4632D4;
        int angleXAddress = 0x4632D8;

        int getModuleAddress(string name)
        {
            Process process = Process.GetProcessesByName("hl2")[0];
            foreach (ProcessModule module in process.Modules)
            {
                if (module.ModuleName == name)
                {
                    return (int)module.BaseAddress;
                }
            }
            return 0;
        }

        float getEntityPos(int i, bool isPlayer)
        {
            int totalAddress = serverModuleAddress + (isPlayer ? playerBaseAddress : enemyBaseAddress);
            Process process = Process.GetProcessesByName("hl2")[0];
            IntPtr pHandle = OpenProcess(ProcessAccessFlags.All, true, process.Id);
            byte[] results = new byte[4];
            int read = 0;
            if (ReadProcessMemory(pHandle, (IntPtr)totalAddress + (4 * i), results, results.Length, out read))
            {
                CloseHandle(pHandle);
                return BitConverter.ToSingle(results, 0);
            }
            CloseHandle(pHandle);
            return 0;
        }

        void changeAngle(float a, bool isX_Angle)
        {
            int totalAddress = engineModuleAddress + (isX_Angle ? angleXAddress : angleYAddress);
            Process process = Process.GetProcessesByName("hl2")[0];
            IntPtr pHandle = OpenProcess(ProcessAccessFlags.All, true, process.Id);
            int written = 0;
            WriteProcessMemory(pHandle, totalAddress, BitConverter.GetBytes(a), 4, out written);
            CloseHandle(pHandle);
        }

        bool started = false;
        async void update()
        {
            while (true)
            {
                if (started)
                {
                    float x1 = getEntityPos(0, true);
                    float y1 = getEntityPos(1, true);
                    float z1 = getEntityPos(2, true);

                    float x2 = getEntityPos(0, false);
                    float y2 = getEntityPos(1, false);
                    float z2 = getEntityPos(2, false) - 10;

                    double distance_X = x2 - x1;
                    double distance_Y = y2 - y1;
                    double distance_Z = z2 - z1;
                    double distance_XY_Plane = Math.Sqrt(Math.Pow(distance_X, 2) + Math.Pow(distance_Y, 2));
                    label1.Text = "Player Info: \nX : " + x1 + "\nY : " + y1 + "\nZ : " + z1;
                    label2.Text = "Enemy Info: \nX : " + x2 + "\nY : " + y2 + "\nZ : " + z2;
                    double x_r = Math.Acos(distance_X / distance_XY_Plane) * 180 / Math.PI;
                    x_r *= (y2 < y1) ? -1 : 1;
                    changeAngle((float)x_r, true);

                    double y_r = -1 * Math.Atan(distance_Z / distance_XY_Plane) * 180 / Math.PI;
                    changeAngle((float)y_r, false);
                }
                await Task.Delay(1);
            }
        }

        public Aimbot_iOSDev()
        {
            InitializeComponent();
            update();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (toggle == 1)
            {
                serverModuleAddress = getModuleAddress("server.dll");
                engineModuleAddress = getModuleAddress("engine.dll");
                started = true;
                injectButton.Text = "Stop";
                label3.Text = "Injected successfully";
            }
            else
            {
                started = false;
                injectButton.Text = "Start";
            }
            toggle *= -1;
        }

        private void previousButton_Click(object sender, EventArgs e)
        {
            if (enemyBaseAddress >= playerBaseAddress + 0x24)
            {
                enemyBaseAddress -= 0x24;            
            }
        }

        private void nextButton_Click(object sender, EventArgs e)
        {
            if (enemyBaseAddress <= playerBaseAddress + 0x24 * 32)
            {
                enemyBaseAddress += 0x24;
            }
        }
    }
}
