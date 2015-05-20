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
		private static JsonSerializer _serializer;
		private const int MAX_BACKUP_COUNT = 10;

		private static JsonSerializer Serializer
		{
			get
			{
				if (_serializer == null)
					_serializer = new JsonSerializer();
				return _serializer;
			}
		}

		public static void WriteTo(this object toSerialize, FileInfo filepath, bool backupPrevious = false)
		{
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
			WriteJsonToFile(toSerialize, filepath);
		}

		public static void WriteJsonToFile(object toSerialize, FileInfo filepath)
		{
			using (var writer = new StreamWriter(filepath.FullName))
			{
				using (var jwriter = new JsonTextWriter(writer))
				{
					var serializer = Serializer;
					serializer.Serialize(jwriter, toSerialize);
				}
			}
		}

		public static TObject ReadJsonFromFile<TObject>(FileInfo filepath)
		{
			using (var reader = new StreamReader(filepath.FullName))
			{
				using (var jreader = new JsonTextReader(reader))
				{
					var serializer = new JsonSerializer();
					return serializer.Deserialize<TObject>(jreader);
				}
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