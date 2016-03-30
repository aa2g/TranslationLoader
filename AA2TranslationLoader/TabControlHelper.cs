using System;
using System.Runtime.InteropServices;
using System.Text;

namespace AA2TranslationLoader
{
	internal class TabControlHelper
	{
		private struct TCITEM
		{
			public uint mask;

			public int dwState;

			public int dwStateMask;

			public IntPtr pszText;

			public int cchTextMax;

			public int iImage;

			public int lParam;
		}

		private const int TCM_FIRST = 4864;

		private const int TCM_GETITEMCOUNT = 4868;

		private const int TCM_GETITEM = 4869;

		private const int TCM_SETITEM = 4870;

		private const int TCIF_TEXT = 1;

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
		private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, ref TabControlHelper.TCITEM tcitemBuffer, int dwSize, IntPtr lpNumberOfBytesWritten);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, IntPtr lpNumberOfBytesWritten);

		[DllImport("kernel32")]
		private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, int dwSize, IntPtr lpNumberOfBytesRead);

		[DllImport("kernel32")]
		private static extern bool CloseHandle(IntPtr hObject);

		public static int GetTabCount(IntPtr hWnd)
		{
			return (int)TabControlHelper.SendMessage(hWnd, 4868, 0, IntPtr.Zero);
		}

		public static void SetTabText(IntPtr hWnd, int processId, int tabIndex, string newText, string targetEncoding)
		{
			if (!string.IsNullOrEmpty(newText))
			{
				IntPtr intPtr = IntPtr.Zero;
				IntPtr intPtr2 = IntPtr.Zero;
				IntPtr zero = IntPtr.Zero;
				int num = Marshal.SizeOf(typeof(TabControlHelper.TCITEM));
				try
				{
					intPtr = TabControlHelper.OpenProcess(2035711u, false, processId);
					if (intPtr == IntPtr.Zero)
					{
						throw new Exception("OpenProcess failed");
					}
					intPtr2 = TabControlHelper.VirtualAllocEx(intPtr, IntPtr.Zero, 1024, 4096u, 4u);
					if (intPtr2 == IntPtr.Zero)
					{
						throw new Exception("VirtualAllocEx failed");
					}
					TabControlHelper.TCITEM tCITEM = default(TabControlHelper.TCITEM);
					tCITEM.mask = 1u;
					tCITEM.pszText = (IntPtr)(intPtr2.ToInt32() + num);
					tCITEM.cchTextMax = 255;
					if (!TabControlHelper.WriteProcessMemory(intPtr, intPtr2, ref tCITEM, num, IntPtr.Zero))
					{
						throw new Exception(string.Format("WriteProcessMemory failed (struct copy, text={0})", newText));
					}
					byte[] bytes = Encoding.GetEncoding(targetEncoding).GetBytes(newText);
					if (!TabControlHelper.WriteProcessMemory(intPtr, tCITEM.pszText, bytes, num, IntPtr.Zero))
					{
						throw new Exception(string.Format("WriteProcessMemory failed (string buffer copy, text={0})", newText));
					}
					TabControlHelper.SendMessage(hWnd, 4870, new IntPtr(tabIndex), intPtr2);
				}
				finally
				{
					if (zero != IntPtr.Zero)
					{
						Marshal.FreeHGlobal(zero);
					}
					if (intPtr2 != IntPtr.Zero)
					{
						TabControlHelper.VirtualFreeEx(intPtr, intPtr2, 0, 32768u);
					}
					if (intPtr != IntPtr.Zero)
					{
						TabControlHelper.CloseHandle(intPtr);
					}
				}
			}
		}

		public static string GetTabText(IntPtr hWnd, int processId, int tabIndex, string targetEncoding)
		{
			IntPtr intPtr = IntPtr.Zero;
			IntPtr intPtr2 = IntPtr.Zero;
			IntPtr intPtr3 = IntPtr.Zero;
			int num = Marshal.SizeOf(typeof(TabControlHelper.TCITEM));
			string result;
			try
			{
				intPtr = TabControlHelper.OpenProcess(2035711u, false, processId);
				if (intPtr == IntPtr.Zero)
				{
					throw new Exception("OpenProcess failed");
				}
				intPtr3 = Marshal.AllocHGlobal(1024);
				intPtr2 = TabControlHelper.VirtualAllocEx(intPtr, IntPtr.Zero, 1024, 4096u, 4u);
				if (intPtr2 == IntPtr.Zero)
				{
					throw new Exception("VirtualAllocEx failed");
				}
				TabControlHelper.TCITEM tCITEM = default(TabControlHelper.TCITEM);
				tCITEM.mask = 1u;
				tCITEM.pszText = (IntPtr)(intPtr2.ToInt32() + num);
				tCITEM.cchTextMax = 255;
				if (!TabControlHelper.WriteProcessMemory(intPtr, intPtr2, ref tCITEM, num, IntPtr.Zero))
				{
					throw new Exception("WriteProcessMemory failed");
				}
				TabControlHelper.SendMessage(hWnd, 4869, new IntPtr(tabIndex), intPtr2);
				if (!TabControlHelper.ReadProcessMemory(intPtr, intPtr2, intPtr3, 1024, IntPtr.Zero))
				{
					throw new Exception("ReadProcessMemory failed");
				}
				byte[] array = new byte[255];
				Marshal.Copy((IntPtr)(intPtr3.ToInt32() + num), array, 0, 255);
				result = TabControlHelper.DecodeString(array, targetEncoding);
			}
			finally
			{
				if (intPtr3 != IntPtr.Zero)
				{
					Marshal.FreeHGlobal(intPtr3);
				}
				if (intPtr2 != IntPtr.Zero)
				{
					TabControlHelper.VirtualFreeEx(intPtr, intPtr2, 0, 32768u);
				}
				if (intPtr != IntPtr.Zero)
				{
					TabControlHelper.CloseHandle(intPtr);
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
