using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace AA2TranslationLoader
{
	public class Program
	{
		[XmlType(AnonymousType = true)]
		public class ReplacementDictionary
		{
			public class Replacement
			{
				[XmlElement(ElementName = "Japanese")]
				public string TargetString
				{
					get;
					set;
				}

				[XmlElement(ElementName = "Translated")]
				public string ReplacementString
				{
					get;
					set;
				}
			}

			[XmlElement(ElementName = "Language")]
			public string TranslationLanguage
			{
				get;
				set;
			}

			[XmlElement(ElementName = "TargetEncoding")]
			public string EncodingName
			{
				get;
				set;
			}

			[XmlElement(ElementName = "ExecutableName")]
			public string GameExeName
			{
				get;
				set;
			}

			[XmlElement(ElementName = "SchemaVersion")]
			public int SchemaVersion
			{
				get;
				set;
			}

			[XmlElement(ElementName = "UpdateNote")]
			public string UpdateNote
			{
				get;
				set;
			}

			[XmlArrayItem(ElementName = "Replacement")]
			public List<Program.ReplacementDictionary.Replacement> Replacements
			{
				get;
				set;
			}

			public ReplacementDictionary()
			{
				this.Replacements = new List<Program.ReplacementDictionary.Replacement>();
			}
		}

		private delegate bool EnumThreadWindowsDelegate(IntPtr hWnd, IntPtr lParam);

		private delegate bool EnumChildWindowsDelegate(IntPtr hWnd, IntPtr lParam);

		private const uint RDW_INVALIDATE = 1u;

		private const uint SW_HIDE = 0u;

		private const uint SW_SHOW = 5u;

		private const int GWL_STYLE = -16;

		private const int TCS_FORCELABELLEFT = 32;

		private const int DEFAULT_GUI_FONT = 17;

		private const int BS_BITMAP = 128;

		private const uint WM_SETFONT = 48u;

		private static List<IntPtr> _knownHandles = new List<IntPtr>();

		private static Dictionary<string, string> _replacements;

		private static bool _debug;

		private static void Main(string[] args)
		{

            Console.WriteLine("AA2TL - Artificial Academy 2 Translation Loader v1.3");
			Console.WriteLine("======================================================");
			Console.WriteLine("");
			Console.WriteLine("Please remember to use AppLocale or change your system");
			Console.WriteLine("locale and date/time format to Japanese.");
			Console.WriteLine("");
			if (args.Length > 0 && args[0].Equals("-debug"))
			{
				Console.WriteLine("Debug switch enabled.\n");
				Program._debug = true;
			}
			Program.ReplacementDictionary replacementDictionary = new Program.ReplacementDictionary();
			try
			{
				Console.WriteLine("Loading dictionary.xml...");
				if (!File.Exists(string.Format("{0}\\dictionary.xml", AppDomain.CurrentDomain.BaseDirectory)))
				{
					Console.WriteLine("   dictionary.xml was not found.");
					Console.WriteLine("   Place dictionary.xml in the same folder as AA2TL.");
					Program.PressAnyKey();
				}
				Program._replacements = new Dictionary<string, string>();
				string xml = File.ReadAllText(string.Format("{0}\\dictionary.xml", AppDomain.CurrentDomain.BaseDirectory));
				replacementDictionary = (Program.ReplacementDictionary)Program.DeserializeXml(xml);
				foreach (Program.ReplacementDictionary.Replacement current in replacementDictionary.Replacements)
				{
					if (!Program._replacements.ContainsKey(current.TargetString) && !current.TargetString.Equals(current.ReplacementString))
					{
						Program._replacements.Add(current.TargetString, current.ReplacementString);
					}
				}
				Console.WriteLine("   {0}", replacementDictionary.UpdateNote);
				Console.WriteLine("   Target: {0}.exe ({1})", replacementDictionary.GameExeName, replacementDictionary.EncodingName);
				Console.WriteLine("   Loaded {0} translations ({1}).", Program._replacements.Count, replacementDictionary.TranslationLanguage);
			}
			catch (Exception ex)
			{
				Console.WriteLine("   {0}", ex.GetType());
				Console.WriteLine("   {0}", ex.Message);
				Console.WriteLine("   Try redownloading the dictionary.xml file.");
				Program.PressAnyKey();
			}
			Console.WriteLine("");
			Process[] processesByName = Process.GetProcessesByName(replacementDictionary.GameExeName);
			Process process;
			if (processesByName.Length > 0)
			{
				Console.Write("Attaching to existing {0}.exe instance (PID {1})...", replacementDictionary.GameExeName, processesByName[0].Id);
				process = processesByName[0];
			}
			else
			{
				Console.Write("Launching {0}.exe...", replacementDictionary.GameExeName);
				if (!File.Exists(string.Format("{0}\\{1}.exe", AppDomain.CurrentDomain.BaseDirectory, replacementDictionary.GameExeName)))
				{
					Console.WriteLine("");
					Console.WriteLine("   {0}.exe was not found.", replacementDictionary.GameExeName);
					Console.WriteLine("   Place AA2TL in your AA2 Maker folder.");
					Program.PressAnyKey();
				}
				process = Process.Start(string.Format("{0}\\{1}.exe", AppDomain.CurrentDomain.BaseDirectory, replacementDictionary.GameExeName));
			}
			Thread.Sleep(1000);
			Console.WriteLine(" ready!");
			if (!Program._debug)
			{
				Program.ShowWindow(Program.GetConsoleWindow(), 0u);
			}
			try
			{
				while (!process.HasExited)
				{
					Stopwatch stopwatch = Stopwatch.StartNew();
					IntPtr[] array = Program.EnumerateProcessWindows(process.Id);
					if (array != null && array.Length > 0)
					{
						IntPtr[] array2 = array;
						for (int i = 0; i < array2.Length; i++)
						{
							IntPtr intPtr = array2[i];
							string text = Program.GetControlText(intPtr);
							string controlClassName = Program.GetControlClassName(intPtr);
							if (text.Contains("\r\n"))
							{
								text = text.Replace("\r\n", "\n");
							}
							if (!Program._knownHandles.Contains(intPtr))
							{
								Program._knownHandles.Add(intPtr);
								Program.SetControlFont(intPtr);
								if (!text.Equals("左目") && !text.Equals("右目") && !text.Equals("両方") && controlClassName.Contains("Button") && !text.Equals(string.Empty))
								{
									Program.ClearControlImage(intPtr);
								}
							}
							if (controlClassName.Equals("SysTabControl32"))
							{
								for (int j = 0; j < TabControlHelper.GetTabCount(intPtr); j++)
								{
									string tabText = TabControlHelper.GetTabText(intPtr, process.Id, j, replacementDictionary.EncodingName);
									bool flag = Program._replacements.ContainsKey(tabText);
									if (j == 0 && !flag)
									{
										break;
									}
									Program.SetTabControlLeftAlignment(intPtr);
									if (flag)
									{
										TabControlHelper.SetTabText(intPtr, process.Id, j, Program._replacements[tabText], replacementDictionary.EncodingName);
									}
								}
							}
							else if (controlClassName.Equals("SysHeader32"))
							{
								for (int j = 0; j < ListViewHeaderHelper.GetColumnCount(intPtr); j++)
								{
									string columnHeaderText = ListViewHeaderHelper.GetColumnHeaderText(intPtr, process.Id, j, replacementDictionary.EncodingName);
									bool flag = Program._replacements.ContainsKey(columnHeaderText);
									if (j == 0 && !flag)
									{
										break;
									}
									if (flag)
									{
										ListViewHeaderHelper.SetColumnHeaderText(intPtr, process.Id, j, Program._replacements[columnHeaderText], replacementDictionary.EncodingName);
									}
								}
							}
							else if (!controlClassName.Equals("ComboBox"))
							{
								if (Program._replacements.ContainsKey(text) && !string.IsNullOrEmpty(text))
								{
									Program.SetControlCaption(intPtr, Program._replacements[text]);
								}
							}
						}
						stopwatch.Stop();
						if (Program._debug)
						{
							Console.Write("\rReplacement loop took {0}ms.   ", stopwatch.ElapsedMilliseconds);
						}
						Thread.Sleep(400);
					}
				}
			}
			catch (Exception ex)
			{
				Program.ShowWindow(Program.GetConsoleWindow(), 5u);
				Console.Write("\n\n");
				Console.WriteLine("=====================================================");
				Console.WriteLine("AA2TL has crashed.");
				Console.WriteLine("{0} will continue running, but will not be translated.", replacementDictionary.GameExeName);
				Console.WriteLine(ex.ToString());
				Program.PressAnyKey();
			}
			Console.WriteLine("\n{0} has been closed.", replacementDictionary.GameExeName);
		}

		private static object DeserializeXml(string xml)
		{
			XmlSerializer xmlSerializer = new XmlSerializer(typeof(Program.ReplacementDictionary), new XmlRootAttribute("ReplacementDictionary"));
			StringReader textReader = new StringReader(xml);
			return xmlSerializer.Deserialize(textReader);
		}

		private static void PressAnyKey()
		{
			Console.WriteLine("\nPress any key to exit.");
			Console.ReadKey();
			Environment.Exit(1);
		}

		private static string GetControlClassName(IntPtr hWnd)
		{
			StringBuilder stringBuilder = new StringBuilder(256);
			Program.GetClassName(hWnd, stringBuilder, stringBuilder.Capacity);
			return stringBuilder.ToString();
		}

		[DllImport("user32.dll")]
		private static extern bool EnumThreadWindows(int dwThreadId, Program.EnumThreadWindowsDelegate callback, IntPtr lParam);

		[DllImport("user32.dll")]
		private static extern bool EnumChildWindows(IntPtr window, Program.EnumChildWindowsDelegate callback, IntPtr lParam);

		[DllImport("user32.dll")]
		private static extern IntPtr GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

		[DllImport("user32.dll")]
		private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

		[DllImport("user32.dll")]
		private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

		[DllImport("user32.dll")]
		private static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, uint flags);

		[DllImport("kernel32.dll")]
		private static extern IntPtr GetConsoleWindow();

		[DllImport("user32.dll")]
		private static extern bool ShowWindow(IntPtr hWnd, uint nCmdShow);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern bool SetWindowText(IntPtr hwnd, string lpString);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern int GetWindowTextLength(IntPtr hWnd);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool PostMessage(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam);

		[DllImport("gdi32.dll")]
		private static extern IntPtr GetStockObject(int fnObject);

		private static IntPtr[] EnumerateProcessWindows(int processId)
		{
			List<IntPtr> parentWindowHandles = new List<IntPtr>();
			List<IntPtr> allWindowHandles = new List<IntPtr>();
			foreach (ProcessThread processThread in Process.GetProcessById(processId).Threads)
			{
				Program.EnumThreadWindows(processThread.Id, delegate(IntPtr hWnd, IntPtr lParam)
				{
					parentWindowHandles.Add(hWnd);
					allWindowHandles.Add(hWnd);
					return true;
				}, IntPtr.Zero);
			}
			foreach (IntPtr current in parentWindowHandles)
			{
				Program.EnumChildWindows(current, delegate(IntPtr hWnd, IntPtr lParam)
				{
					allWindowHandles.Add(hWnd);
					return true;
				}, IntPtr.Zero);
			}
			return allWindowHandles.ToArray();
		}

		private static void ClearControlImage(IntPtr hWnd)
		{
			int num = Program.GetWindowLong(hWnd, -16);
			num &= -129;
			Program.SetWindowLong(hWnd, -16, num);
		}

		private static string GetControlText(IntPtr hWnd)
		{
			int windowTextLength = Program.GetWindowTextLength(hWnd);
			StringBuilder stringBuilder = new StringBuilder(windowTextLength + 1);
			Program.GetWindowText(hWnd, stringBuilder, stringBuilder.Capacity);
			return stringBuilder.ToString();
		}

		private static void SetControlCaption(IntPtr hWnd, string caption)
		{
			Program.SetWindowText(hWnd, caption);
			Program.RedrawWindow(hWnd, IntPtr.Zero, IntPtr.Zero, 1u);
		}

		private static void SetTabControlLeftAlignment(IntPtr hWnd)
		{
			int num = Program.GetWindowLong(hWnd, -16);
			num |= 32;
			Program.SetWindowLong(hWnd, -16, num);
		}

        private static void SetControlFont(IntPtr hWnd)
		{
            //IntPtr stockObject = Program.GetStockObject(17);
            //Program.PostMessage(hWnd, WM_SETFONT, myFont, (IntPtr)1);
		}
	}
}
