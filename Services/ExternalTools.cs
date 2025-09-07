using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace HGEngineGUI.Services
{
	public static class ExternalTools
	{
		public static string ToolsRoot => Path.Combine(AppContext.BaseDirectory, "Tools");
		public static string ResolvedRnbEditorRoot { get; private set; } = string.Empty;
		public static bool HasRnbEditor { get; private set; }
		public static bool HasPython { get; private set; }
		public static string PythonCommand { get; private set; } = string.Empty;

		public static void Refresh()
		{
			try
			{
				// Detection precedence:
				// 1) Next to EXE:  [App]/Tools/rnbstyle-hge-editor
				// 2) Inside selected project root:  [Project]/rnbstyle-hge-editor
				// 3) Inside selected project root Tools:  [Project]/Tools/rnbstyle-hge-editor
				string? projectRoot = ProjectContext.RootPath;
				string exeCandidate = Path.Combine(ToolsRoot, "rnbstyle-hge-editor");
				string projCandidate1 = projectRoot == null ? string.Empty : Path.Combine(projectRoot, "rnbstyle-hge-editor");
				string projCandidate2 = projectRoot == null ? string.Empty : Path.Combine(projectRoot, "Tools", "rnbstyle-hge-editor");
				string projSibling = string.Empty;
				try { var parent = projectRoot == null ? null : Directory.GetParent(projectRoot); projSibling = parent == null ? string.Empty : Path.Combine(parent.FullName, "rnbstyle-hge-editor"); }
				catch { projSibling = string.Empty; }

				string pick = string.Empty;
				if (IsValidRnbEditorRoot(exeCandidate)) pick = exeCandidate;
				else if (!string.IsNullOrEmpty(projSibling) && IsValidRnbEditorRoot(projSibling)) pick = projSibling;
				else if (!string.IsNullOrEmpty(projCandidate1) && IsValidRnbEditorRoot(projCandidate1)) pick = projCandidate1;
				else if (!string.IsNullOrEmpty(projCandidate2) && IsValidRnbEditorRoot(projCandidate2)) pick = projCandidate2;

				ResolvedRnbEditorRoot = pick;
				HasRnbEditor = !string.IsNullOrEmpty(pick);
			}
			catch { HasRnbEditor = false; }

			try
			{
				// Try py launcher first for Windows, then python
				if (TryCheckPython("py", out var cmd) || TryCheckPython("python", out cmd) || TryCheckPython("python3", out cmd))
				{
					HasPython = true; PythonCommand = cmd;
				}
				else
				{
					HasPython = false; PythonCommand = string.Empty;
				}
			}
			catch { HasPython = false; PythonCommand = string.Empty; }
		}

		private static bool IsValidRnbEditorRoot(string path)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) return false;
				if (File.Exists(Path.Combine(path, "tocsv.py"))) return true;
				if (File.Exists(Path.Combine(path, "manifest.json"))) return true;
			}
			catch { }
			return false;
		}

		private static bool TryCheckPython(string candidate, out string resolved)
		{
			resolved = string.Empty;
			try
			{
				var psi = new ProcessStartInfo
				{
					FileName = candidate,
					Arguments = "--version",
					CreateNoWindow = true,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true
				};
				using var p = Process.Start(psi);
				if (p == null) return false;
				p.WaitForExit(3000);
				if (p.ExitCode == 0 || p.ExitCode == 1)
				{
					resolved = candidate; return true;
				}
			}
			catch { }
			return false;
		}

		public static async Task<(int ExitCode, string StdOut, string StdErr)> RunPythonAsync(string args, string? workingDir = null)
		{
			if (!HasPython || string.IsNullOrEmpty(PythonCommand)) return (-1, string.Empty, "Python not available");
			var psi = new ProcessStartInfo
			{
				FileName = PythonCommand,
				Arguments = args,
				WorkingDirectory = string.IsNullOrWhiteSpace(workingDir) ? (string.IsNullOrEmpty(ResolvedRnbEditorRoot) ? AppContext.BaseDirectory : ResolvedRnbEditorRoot) : workingDir,
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true
			};
			try
			{
				using var proc = Process.Start(psi);
				if (proc == null) return (-1, string.Empty, "Failed to start python process");
				string stdout = await proc.StandardOutput.ReadToEndAsync();
				string stderr = await proc.StandardError.ReadToEndAsync();
				await Task.Run(() => proc.WaitForExit(120000));
				return (proc.HasExited ? proc.ExitCode : -1, stdout, stderr);
			}
			catch (Exception ex)
			{
				return (-1, string.Empty, ex.Message);
			}
		}
	}
}


