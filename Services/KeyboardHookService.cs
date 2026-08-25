using System.Diagnostics;
using System.Runtime.InteropServices;
using Prayer.Interop;

namespace Prayer.Services;

public class KeyboardHookService : IDisposable
{
    private IntPtr _hookId = IntPtr.Zero;
    private Win32Native.LowLevelKeyboardProc? _proc;
    private bool _isHooked = false;

    public bool IsHooked => _isHooked;
    public bool AllowTestEscape { get; set; } = false;

    public void InstallHook()
    {
        if (_isHooked) return;

        _proc = HookCallback;
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;
        
        var moduleHandle = curModule != null 
            ? Win32Native.GetModuleHandle(curModule.ModuleName) 
            : IntPtr.Zero;

        _hookId = Win32Native.SetWindowsHookEx(Win32Native.WH_KEYBOARD_LL, _proc, moduleHandle, 0);
        _isHooked = _hookId != IntPtr.Zero;
    }

    public void UninstallHook()
    {
        if (!_isHooked) return;

        if (_hookId != IntPtr.Zero)
        {
            Win32Native.UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }

        _proc = null;
        _isHooked = false;
        AllowTestEscape = false;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var hookStruct = Marshal.PtrToStructure<Win32Native.KBDLLHOOKSTRUCT>(lParam);
            uint vkCode = hookStruct.vkCode;
            int msg = wParam.ToInt32();

            bool isKeyDown = msg == Win32Native.WM_KEYDOWN || msg == Win32Native.WM_SYSKEYDOWN;
            bool isAltPressed = (hookStruct.flags & 0x20) != 0 || (Win32Native.GetAsyncKeyState(Win32Native.VK_MENU) & 0x8000) != 0;
            bool isCtrlPressed = (Win32Native.GetAsyncKeyState(Win32Native.VK_CONTROL) & 0x8000) != 0;

            // In test mode, allow Escape and Alt+F4 to exit preview
            if (AllowTestEscape)
            {
                if (vkCode == Win32Native.VK_ESCAPE || (vkCode == Win32Native.VK_F4 && isAltPressed))
                {
                    return Win32Native.CallNextHookEx(_hookId, nCode, wParam, lParam);
                }
            }

            // 1. Block Windows Keys (Left and Right)
            if (vkCode == Win32Native.VK_LWIN || vkCode == Win32Native.VK_RWIN)
            {
                return (IntPtr)1;
            }

            // 2. Block Alt+Tab
            if (vkCode == Win32Native.VK_TAB && isAltPressed)
            {
                return (IntPtr)1;
            }

            // 3. Block Alt+F4
            if (vkCode == Win32Native.VK_F4 && isAltPressed)
            {
                return (IntPtr)1;
            }

            // 4. Block Alt+Esc or Ctrl+Esc
            if (vkCode == Win32Native.VK_ESCAPE && (isAltPressed || isCtrlPressed))
            {
                return (IntPtr)1;
            }

            // 5. Block Ctrl+Shift+Esc (Task Manager shortcut attempt)
            if (vkCode == Win32Native.VK_ESCAPE && isCtrlPressed)
            {
                return (IntPtr)1;
            }
        }

        return Win32Native.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        UninstallHook();
        GC.SuppressFinalize(this);
    }
}
