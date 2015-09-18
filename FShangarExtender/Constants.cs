using UnityEngine;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

namespace FShangarExtender
{
	static class Constants
	{
		public const string debugMarker = "[FSHangarExtender]";
		public const string debugVersion = "Version 3.4.4";

		public static string[] baseSceneNodeNames = { "vabscenery", "sphscenery", "vablvl1", "vablvl2", "vablvl3", "vabmodern", "sphlvl1", "sphlvl2", "sphlvl3", "sphmodern" };
		public static string[] nonScalingNodeNames = { "vabcrew", "sphcrew" };

		public static readonly string settingRuntimeDirectory = Assembly.GetExecutingAssembly().Location.Replace(new FileInfo(Assembly.GetExecutingAssembly().Location).Name, "");
		public const string configFileName = "settings.txt";
		public static string CompletePathAndFileName = string.Concat(settingRuntimeDirectory, configFileName);

		public const string defaultHotKey = "[*]";
		public const float defaultScaleFactor = 10f;
		public const string defaultTempParentName = "FSHangarExtender_Temp_Parent";
	}
}
