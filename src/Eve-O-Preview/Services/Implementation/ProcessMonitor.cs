﻿using EveOPreview.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;

namespace EveOPreview.Services.Implementation
{
	sealed class ProcessMonitor : IProcessMonitor
	{

		#region Private fields
		private readonly IDictionary<IntPtr, string> _processCache;
		private IProcessInfo _currentProcessInfo;
        private readonly IThumbnailConfiguration _configuration;
        #endregion

        public ProcessMonitor(IThumbnailConfiguration configuration)
		{
			_configuration = configuration;

            this._processCache = new Dictionary<IntPtr, string>(512);
			
			// This field cannot be initialized properly in constructor
			// At the moment this code is executed the main application window is not yet initialized
			this._currentProcessInfo = new ProcessInfo(IntPtr.Zero, "", null);
		}

		private bool IsMonitoredProcess(string processName)
		{
			// This is a possible extension point
			if (_configuration.ProcessName == "*")
			{
				return true;
			}
			return String.Equals(processName, _configuration.ProcessName, StringComparison.OrdinalIgnoreCase);
		}

		private IProcessInfo GetCurrentProcessInfo()
		{
			var currentProcess = Process.GetCurrentProcess();
			var index = GetIndex(currentProcess.MainWindowHandle, currentProcess.MainWindowTitle);
			return new ProcessInfo(currentProcess.MainWindowHandle, currentProcess.MainWindowTitle, index);
		}

		public IProcessInfo GetMainProcess()
		{
			if (this._currentProcessInfo.Handle == IntPtr.Zero)
			{
				var processInfo = this.GetCurrentProcessInfo();

				// Are we initialized yet?
				if (processInfo.Title != "")
				{
					this._currentProcessInfo = processInfo;
				}
			}

			return this._currentProcessInfo;
		}

		public ICollection<IProcessInfo> GetAllProcesses()
		{
			ICollection<IProcessInfo> result = new List<IProcessInfo>(this._processCache.Count);

			// TODO Lock list here just in case
			foreach (KeyValuePair<IntPtr, string> entry in this._processCache)
			{
				var index = GetIndex(entry.Key, entry.Value);
				result.Add(new ProcessInfo(entry.Key, entry.Value, index));
			}

			return result;
		}

		public void GetUpdatedProcesses(out ICollection<IProcessInfo> addedProcesses, out ICollection<IProcessInfo> updatedProcesses, out ICollection<IProcessInfo> removedProcesses)
		{
			addedProcesses = new List<IProcessInfo>(16);
			updatedProcesses = new List<IProcessInfo>(16);
			removedProcesses = new List<IProcessInfo>(16);

			IList<IntPtr> knownProcesses = new List<IntPtr>(this._processCache.Keys);
			foreach (Process process in Process.GetProcesses())
			{
				string processName = process.ProcessName;

				if (!this.IsMonitoredProcess(processName))
				{
					continue;
				}

				IntPtr mainWindowHandle = process.MainWindowHandle;
				if (mainWindowHandle == IntPtr.Zero)
				{
					continue; // No need to monitor non-visual processes
				}

				string mainWindowTitle = process.MainWindowTitle;
				this._processCache.TryGetValue(mainWindowHandle, out string cachedTitle);

				if (cachedTitle == null)
				{
					// This is a new process in the list
					this._processCache.Add(mainWindowHandle, mainWindowTitle);
					var index = GetIndex(mainWindowHandle, mainWindowTitle);
					addedProcesses.Add(new ProcessInfo(mainWindowHandle, mainWindowTitle, index));
				}
				else
				{
					// This is an already known process
					if (cachedTitle != mainWindowTitle)
					{
						this._processCache[mainWindowHandle] = mainWindowTitle;
						var index = UpdateIndex(mainWindowHandle, mainWindowTitle, cachedTitle);
						updatedProcesses.Add(new ProcessInfo(mainWindowHandle, mainWindowTitle, index));
					}

					knownProcesses.Remove(mainWindowHandle);
				}
			}

			foreach (IntPtr handle in knownProcesses)
			{
				string title = this._processCache[handle];
				var index = RemoveIndex(handle, title);
				removedProcesses.Add(new ProcessInfo(handle, title, index));
				this._processCache.Remove(handle);
			}
		}


		#region Indexes

		private static Dictionary<string, HashSet<int>> indexesByName = new Dictionary<string, HashSet<int>>();
		private static Dictionary<IntPtr, int> indexByHandle = new Dictionary<IntPtr, int>();

		private int GetIndex(IntPtr handle, string title)
		{
			if (indexByHandle.ContainsKey(handle))
			{
				return indexByHandle[handle];
			}
			if (!indexesByName.ContainsKey(title))
			{
				indexesByName[title] = new HashSet<int>();
			}

			var newIndex = 1;
			var reservedIndexes = indexesByName[title];
			while (reservedIndexes.Contains(newIndex))
			{
				newIndex++;
			}
			reservedIndexes.Add(newIndex);
			indexByHandle[handle] = newIndex;
			return newIndex;
		}
		private int RemoveIndex(IntPtr handle, string title)
		{
			var oldIndex = indexByHandle[handle];
			indexesByName[title].Remove(oldIndex);
			indexByHandle.Remove(handle);
			return oldIndex;
		}

		private int UpdateIndex(IntPtr handle, string newTitle, string cachedTitle)
		{
			RemoveIndex(handle, cachedTitle);
			return GetIndex(handle, newTitle);
		}

		#endregion
	}
}
