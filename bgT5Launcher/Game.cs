using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace bgT5Launcher;

internal class Game
{
	private enum ProcessAccessFlags : uint
	{
		All = 2035711u,
		Terminate = 1u,
		CreateThread = 2u,
		VMOperation = 8u,
		VMRead = 16u,
		VMWrite = 32u,
		DupHandle = 64u,
		SetInformation = 512u,
		QueryInformation = 1024u,
		Synchronize = 1048576u
	}

	[DllImport("kernel32.dll")]
	private static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out int lpNumberOfBytesWritten);

	[DllImport("kernel32.dll")]
	public static extern int CloseHandle(IntPtr hProcess);

	private static void WriteMem(Process p, int address, long v)
	{
		IntPtr hProcess = OpenProcess(ProcessAccessFlags.All, bInheritHandle: false, p.Id);
		byte[] array = new byte[1] { (byte)v };
		int lpNumberOfBytesWritten = 0;
		WriteProcessMemory(hProcess, new IntPtr(address), array, (uint)array.LongLength, out lpNumberOfBytesWritten);
		CloseHandle(hProcess);
	}

	public static void Start(Process p)
	{
		WriteMem(p, 94130980, 1L);
		WriteMem(p, 94131056, 1L);
	}
}
