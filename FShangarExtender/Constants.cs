using UnityEngine;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

namespace FShangarExtender
{
	static class Constants
	{
		public const string debugMarker = "[FSHangarExtender]";
		public const string debugVersion = "Version 3.4.6";

		public static string[] baseSceneNames = { "vabscenery", "sphscenery" };
		public static string[] baseHangarNames = { "vablvl1", "vablvl2", "vablvl3", "vabmodern", "sphlvl1", "sphlvl2", "sphlvl3", "sphmodern" };
		public static string[] baseHangarVisibleNames = { "model_vab_exterior_ground_v46n", "model_vab_interior_ground_v20", "Component_780_1", "model_vab_interior_floor_cover_v20", "ShadowPlane", "model_vab_interior_lights_flood_v16", "model_vab_interior_occluder_v16", "model_sph_exterior_ground_v20", "Component_1_1", "Component_777_1", "ksp_runway", "ksp_runway_fbx" };
		public static string[] nonScalingNodeNames = { "vabcrew", "sphcrew" };

		public static readonly string settingRuntimeDirectory = Assembly.GetExecutingAssembly().Location.Replace(new FileInfo(Assembly.GetExecutingAssembly().Location).Name, "");
		public const string configFileName = "settings.txt";
		public static string CompletePathAndFileName = string.Concat(settingRuntimeDirectory, configFileName);
		public const string extentIconFileName = "FSHangarExtender/FShangarExtenderIconExtend";
		public const string shrinkIconFileName = "FSHangarExtender/FShangarExtenderIconShrink";
		public static string completeShrinkIconFileNamePath = string.Concat(settingRuntimeDirectory, extentIconFileName);
		public static string completeExtendIconFileNamePath = string.Concat(settingRuntimeDirectory, shrinkIconFileName);

		public const string defaultHotKey = "[*]";
		public const float defaultScaleFactor = 10f;
		public const string defaultTempParentName = "FSHangarExtender_Temp_Parent";
	}
}
