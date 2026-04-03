using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace bgT5Launcher;

internal class KeyHook
{
	public delegate int keyboardHookProc(int code, int wParam, ref keyboardHookStruct lParam);

	public struct keyboardHookStruct
	{
		public int vkCode;

		public int scanCode;

		public int flags;

		public int time;

		public int dwExtraInfo;
	}

	private const int WH_KEYBOARD_LL = 13;

	private const int WM_KEYDOWN = 256;

	private const int WM_KEYUP = 257;

	private const int WM_SYSKEYDOWN = 260;

	private const int WM_SYSKEYUP = 261;

	public List<Keys> HookedKeys = new List<Keys>();

	private IntPtr hhook = IntPtr.Zero;

	private keyboardHookProc _hookProc;

	public event KeyEventHandler KeyDown;

	public event KeyEventHandler KeyUp;

	public KeyHook()
	{
		hook();
	}

	~KeyHook()
	{
		unhook();
	}

	public void hook()
	{
		_hookProc = hookProc;
		IntPtr hInstance = LoadLibrary("User32");
		hhook = SetWindowsHookEx(13, _hookProc, hInstance, 0u);
	}

	public void unhook()
	{
		UnhookWindowsHookEx(hhook);
	}

	public int hookProc(int code, int wParam, ref keyboardHookStruct lParam)
	{
		if (code >= 0)
		{
			Keys vkCode = (Keys)lParam.vkCode;
			if (HookedKeys.Contains(vkCode))
			{
				KeyEventArgs keyEventArgs = new KeyEventArgs(vkCode);
				if ((wParam == 256 || wParam == 260) && this.KeyDown != null)
				{
					this.KeyDown(this, keyEventArgs);
				}
				else if ((wParam == 257 || wParam == 261) && this.KeyUp != null)
				{
					this.KeyUp(this, keyEventArgs);
				}
				if (keyEventArgs.Handled)
				{
					return 1;
				}
			}
		}
		return CallNextHookEx(hhook, code, wParam, ref lParam);
	}

	[DllImport("user32.dll")]
	private static extern IntPtr SetWindowsHookEx(int idHook, keyboardHookProc callback, IntPtr hInstance, uint threadId);

	[DllImport("user32.dll")]
	private static extern bool UnhookWindowsHookEx(IntPtr hInstance);

	[DllImport("user32.dll")]
	private static extern int CallNextHookEx(IntPtr idHook, int nCode, int wParam, ref keyboardHookStruct lParam);

	[DllImport("kernel32.dll")]
	private static extern IntPtr LoadLibrary(string lpFileName);
}
