#region file header
// ////////////////////////////////////////////////////////////////////
// ///
// ///  
// /// 17.05.2015
// ///
// ///
// ////////////////////////////////////////////////////////////////////
#endregion

using System;
using System.IO;
using Newtonsoft.Json;

namespace RegulatedNoise.Core.Helpers
{
	public static class SerializationHelpers
	{
		private const int MAX_BACKUP_COUNT = 10;

		public static void WriteTo(this object toSerialize, FileInfo filepath, bool backupPrevious = false)
		{
			if (toSerialize == null)
			{
				throw new ArgumentNullException("toSerialize");
			}
			if (filepath == null)
			{
				throw new ArgumentNullException("filepath");
			}
			if (backupPrevious && filepath.Exists)
			{
				var destFileName = GetBackupPath(filepath);
				if (File.Exists(destFileName))
				{
					File.Delete(destFileName);
				}
				File.Move(filepath.FullName, destFileName);
			}
			using (var writer = new StreamWriter(filepath.FullName))
			using (var jwriter = new JsonTextWriter(writer))
			{
				var serializer = new JsonSerializer();
				serializer.Serialize(jwriter, toSerialize);
			}
		}

		private static string GetBackupPath(FileInfo filepath, int index = MAX_BACKUP_COUNT)
		{
			if (index == 0)
			{
				return BuildBackupPath(filepath, MAX_BACKUP_COUNT);
			}
			else
			{
				var backupPath = BuildBackupPath(filepath, index);
				if (!File.Exists(backupPath))
				{
					return backupPath;
				}
				else
				{
					return GetBackupPath(filepath, --index);
				}
			}
		}

		private static string BuildBackupPath(FileInfo filepath, int index)
		{
			var directoryName = filepath.DirectoryName;
			string backupPath;
			if (directoryName != null)
			{
				backupPath = Path.Combine(directoryName,
					Path.GetFileNameWithoutExtension(filepath.FullName),
					"." + index.ToString("00"),
					Path.GetExtension(filepath.FullName));
			}
			else
			{
				backupPath = Path.Combine(Path.GetFileNameWithoutExtension(filepath.FullName),
					"." + index.ToString("00"),
					Path.GetExtension(filepath.FullName));
			}
			return backupPath;
		}
	}
}