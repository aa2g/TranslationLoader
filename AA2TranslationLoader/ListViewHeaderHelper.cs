using System;
using System.Runtime.InteropServices;
using System.Text;

namespace AA2TranslationLoader
{
	internal class ListViewHeaderHelper
	{
		public struct HDITEM
		{
			public uint mask;

			public int cxy;

			public IntPtr pszText;

			public IntPtr hbm;

			public int cchTextMax;

			public int fmt;

			public IntPtr lParam;

			public int iImage;

			public int iOrder;

			public uint type;

			public IntPtr pvFilter;

			public uint state;
		}

		private const int HDM_FIRST = 4608;

		private const int HDM_GETITEM = 4611;

		private const int HDM_SETITEM = 4612;

		private const int HDM_GETITEMCOUNT = 4608;

		private const uint HDI_TEXT = 2u;

		private const uint PROCESS_ALL_ACCESS = 2035711u;

		private const uint MEM_COMMIT = 4096u;

		private const uint MEM_RELEASE = 32768u;

		private const uint PAGE_READWRITE = 4u;

		[DllImport("user32.dll")]
		public static extern IntPtr SendMessage(IntPtr hWnd, int message, int wParam, IntPtr lParam);

		[DllImport("user32.dll")]
		public static extern bool SendMessage(IntPtr hWnd, int message, IntPtr wParam, IntPtr lParam);

		[DllImport("kernel32")]
		private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

		[DllImport("kernel32")]
		private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, uint flAllocationType, uint flProtect);

		[DllImport("kernel32")]
		private static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, uint dwFreeType);

		[DllImport("kernel32")]
		private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, ref ListViewHeaderHelper.HDITEM lvColumnBuffer, int dwSize, IntPtr lpNumberOfBytesWritten);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, IntPtr lpNumberOfBytesWritten);

		[DllImport("kernel32")]
		private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, int dwSize, IntPtr lpNumberOfBytesRead);

		[DllImport("kernel32")]
		private static extern bool CloseHandle(IntPtr hObject);

		public static int GetColumnCount(IntPtr hWnd)
		{
			return (int)ListViewHeaderHelper.SendMessage(hWnd, 4608, 0, IntPtr.Zero);
		}

		public static void SetColumnHeaderText(IntPtr hWnd, int processId, int tabIndex, string newText, string targetEncoding)
		{
			if (!string.IsNullOrEmpty(newText))
			{
				IntPtr intPtr = IntPtr.Zero;
				IntPtr intPtr2 = IntPtr.Zero;
				IntPtr zero = IntPtr.Zero;
				int num = Marshal.SizeOf(typeof(ListViewHeaderHelper.HDITEM));
				try
				{
					intPtr = ListViewHeaderHelper.OpenProcess(2035711u, false, processId);
					if (intPtr == IntPtr.Zero)
					{
						throw new Exception("OpenProcess failed");
					}
					intPtr2 = ListViewHeaderHelper.VirtualAllocEx(intPtr, IntPtr.Zero, 1024, 4096u, 4u);
					if (intPtr2 == IntPtr.Zero)
					{
						throw new Exception("VirtualAllocEx failed");
					}
					ListViewHeaderHelper.HDITEM hDITEM = default(ListViewHeaderHelper.HDITEM);
					hDITEM.mask = 2u;
					hDITEM.pszText = (IntPtr)(intPtr2.ToInt32() + num);
					hDITEM.cchTextMax = 255;
					if (!ListViewHeaderHelper.WriteProcessMemory(intPtr, intPtr2, ref hDITEM, num, IntPtr.Zero))
					{
						throw new Exception(string.Format("WriteProcessMemory failed (struct copy, text={0})", newText));
					}
					byte[] bytes = Encoding.GetEncoding(targetEncoding).GetBytes(newText);
					if (!ListViewHeaderHelper.WriteProcessMemory(intPtr, hDITEM.pszText, bytes, num, IntPtr.Zero))
					{
						throw new Exception(string.Format("WriteProcessMemory failed (string buffer copy, text={0})", newText));
					}
					ListViewHeaderHelper.SendMessage(hWnd, 4612, new IntPtr(tabIndex), intPtr2);
				}
				finally
				{
					if (zero != IntPtr.Zero)
					{
						Marshal.FreeHGlobal(zero);
					}
					if (intPtr2 != IntPtr.Zero)
					{
						ListViewHeaderHelper.VirtualFreeEx(intPtr, intPtr2, 0, 32768u);
					}
					if (intPtr != IntPtr.Zero)
					{
						ListViewHeaderHelper.CloseHandle(intPtr);
					}
				}
			}
		}

		public static string GetColumnHeaderText(IntPtr hWnd, int processId, int colIndex, string targetEncoding)
		{
			IntPtr intPtr = IntPtr.Zero;
			IntPtr intPtr2 = IntPtr.Zero;
			IntPtr intPtr3 = IntPtr.Zero;
			int num = Marshal.SizeOf(typeof(ListViewHeaderHelper.HDITEM));
			string result = "";
			try
			{
				intPtr = ListViewHeaderHelper.OpenProcess(2035711u, false, processId);
				if (intPtr == IntPtr.Zero)
				{
					throw new Exception("OpenProcess failed");
				}
				intPtr3 = Marshal.AllocHGlobal(1024);
				intPtr2 = ListViewHeaderHelper.VirtualAllocEx(intPtr, IntPtr.Zero, 1024, 4096u, 4u);
				if (intPtr2 == IntPtr.Zero)
				{
					throw new Exception("VirtualAllocEx failed");
				}
				ListViewHeaderHelper.HDITEM hDITEM = default(ListViewHeaderHelper.HDITEM);
				hDITEM.mask = 2u;
				hDITEM.pszText = (IntPtr)(intPtr2.ToInt32() + num);
				hDITEM.cchTextMax = 255;
				if (!ListViewHeaderHelper.WriteProcessMemory(intPtr, intPtr2, ref hDITEM, num, IntPtr.Zero))
				{
					throw new Exception("WriteProcessMemory failed");
				}
				ListViewHeaderHelper.SendMessage(hWnd, 4611, new IntPtr(colIndex), intPtr2);
				if (!ListViewHeaderHelper.ReadProcessMemory(intPtr, intPtr2, intPtr3, 1024, IntPtr.Zero))
				{
					throw new Exception("ReadProcessMemory failed");
				}
				byte[] array = new byte[255];
				Marshal.Copy((IntPtr)(intPtr3.ToInt32() + num), array, 0, 255);
				result = ListViewHeaderHelper.DecodeString(array, targetEncoding);
			}
			finally
			{
				if (intPtr3 != IntPtr.Zero)
				{
					Marshal.FreeHGlobal(intPtr3);
				}
				if (intPtr2 != IntPtr.Zero)
				{
					ListViewHeaderHelper.VirtualFreeEx(intPtr, intPtr2, 0, 32768u);
				}
				if (intPtr != IntPtr.Zero)
				{
					ListViewHeaderHelper.CloseHandle(intPtr);
				}
			}
			return result;
		}

		private static string DecodeString(byte[] buffer, string targetEncoding)
		{
			int num = Array.IndexOf<byte>(buffer, 0, 0);
			if (num < 0)
			{
				num = buffer.Length;
			}
			return Encoding.GetEncoding(targetEncoding).GetString(buffer, 0, num);
		}

		public static string ByteArrayToHexRepresentation(byte[] ba)
		{
			string text = BitConverter.ToString(ba);
			return text.Replace("-", "");
		}
	}
}
