using JLibrary.Win32;
using System;
using System.Text;

namespace InjectionLibrary
{
	internal class CRTInjection : StandardInjectionMethod
	{
		public override IntPtr Inject(string dllPath, IntPtr hProcess)
		{
			this.ClearErrors();
			if (hProcess.IsNull() || hProcess.Compare(-1L))
			{
				throw new ArgumentOutOfRangeException("hProcess", "Invalid process handle specified.");
			}
			IntPtr result;
			try
			{
				IntPtr intPtr = IntPtr.Zero;
				IntPtr procAddress = WinAPI.GetProcAddress(WinAPI.GetModuleHandleA("kernel32.dll"), "LoadLibraryW");
				if (procAddress.IsNull())
				{
					throw new Exception("Unable to locate the LoadLibraryW entry point");
				}
				IntPtr intPtr2 = WinAPI.CreateRemotePointer(hProcess, Encoding.Unicode.GetBytes(dllPath + "\0"), 4);
				if (intPtr2.IsNull())
				{
					throw new InvalidOperationException("Failed to allocate memory in the remote process");
				}
				try
				{
					uint num = WinAPI.RunThread(hProcess, procAddress, (uint)intPtr2.ToInt32(), 10000);
					if (num == 4294967295u)
					{
						throw new Exception("Error occurred when calling function in the remote process");
					}
					if (num == 0u)
					{
						throw new Exception("Failed to load module into remote process. Error code: " + WinAPI.GetLastErrorEx(hProcess).ToString());
					}
					intPtr = Win32Ptr.Create((long)((ulong)num));
				}
				finally
				{
					WinAPI.VirtualFreeEx(hProcess, intPtr2, 0, 32768);
				}
				result = intPtr;
			}
			catch (Exception lastError)
			{
				this.SetLastError(lastError);
				result = IntPtr.Zero;
			}
			return result;
		}

		public override IntPtr[] InjectAll(string[] dllPaths, IntPtr hProcess)
		{
			this.ClearErrors();
			if (hProcess.IsNull() || hProcess.Compare(-1L))
			{
				throw new ArgumentOutOfRangeException("hProcess", "Invalid process handle specified.");
			}
			IntPtr[] result;
			try
			{
				IntPtr zero = IntPtr.Zero;
				IntPtr intPtr = this.CreateMultiLoadStub(dllPaths, hProcess, out zero, 0u);
				IntPtr[] array = null;
				if (!intPtr.IsNull())
				{
					try
					{
						uint num = WinAPI.RunThread(hProcess, intPtr, 0u, 10000);
						if (num == 4294967295u)
						{
							throw new Exception("Error occurred while executing remote thread.");
						}
						byte[] array2 = WinAPI.ReadRemoteMemory(hProcess, zero, (uint)((uint)dllPaths.Length << 2));
						if (array2 == null)
						{
							throw new InvalidOperationException("Unable to read from the remote process.");
						}
						array = new IntPtr[dllPaths.Length];
						for (int i = 0; i < array.Length; i++)
						{
							array[i] = new IntPtr(BitConverter.ToInt32(array2, i << 2));
						}
					}
					finally
					{
						WinAPI.VirtualFreeEx(hProcess, zero, 0, 32768);
						WinAPI.VirtualFreeEx(hProcess, intPtr, 0, 32768);
					}
				}
				result = array;
			}
			catch (Exception lastError)
			{
				this.SetLastError(lastError);
				result = null;
			}
			return result;
		}
	}
}
