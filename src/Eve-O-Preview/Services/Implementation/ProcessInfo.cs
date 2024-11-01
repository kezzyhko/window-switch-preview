using System;

namespace EveOPreview.Services.Implementation
{
	sealed class ProcessInfo : IProcessInfo
	{
		public ProcessInfo(IntPtr handle, string title, int? index)
		{
			this.Handle = handle;
			this.Title = index == null ? title : $"{title} ({index})";
		}

		public IntPtr Handle { get; }
		public string Title { get; }
	}
}