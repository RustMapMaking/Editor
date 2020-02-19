﻿using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class AssetManager
{
	public static string BundlePath { get; private set; }

	public static GameManifest Manifest { get; private set; }
	private const string ManifestPath = "assets/manifest.asset";

	public static AssetBundleManifest AssetManifest { get; private set; }

	private static Dictionary<uint, string> IDLookup = new Dictionary<uint, string>();
	private static Dictionary<string, uint> PathLookup = new Dictionary<string, uint>();

	public static Dictionary<string, AssetBundle> Bundles = new Dictionary<string, AssetBundle>(System.StringComparer.OrdinalIgnoreCase);
	public static Dictionary<string, AssetBundle> AssetPaths = new Dictionary<string, AssetBundle>(System.StringComparer.OrdinalIgnoreCase);
	public static Dictionary<string, Object> Cache = new Dictionary<string, Object>();

	public static bool IsInitialised { get; private set; }

	/// <summary>Loads the prefabs from the Rust prefab bundle.</summary>
	/// <param name="bundlesRoot">The file path to the Rust bundles file.</param>
	public static void Initialise(string bundlesRoot)
	{
		if (!IsInitialised)
		{
			MapManager.ProgressBar("Loading Bundles", "Loading Root Bundle", 0.1f);
			BundlePath = bundlesRoot;
			var rootBundle = AssetBundle.LoadFromFile(bundlesRoot);
			if (rootBundle == null)
			{
				Debug.LogError("Couldn't load root AssetBundle - " + bundlesRoot);
				return;
			}

			var manifestList = rootBundle.LoadAllAssets<AssetBundleManifest>();
			if (manifestList.Length != 1)
			{
				Debug.LogError("Couldn't find AssetBundleManifest - " + manifestList.Length);
				return;
			}
			AssetManifest = manifestList[0];

			var bundles = AssetManifest.GetAllAssetBundles();
			for (int i = 0; i < bundles.Length; i++)
			{
				MapManager.progressValue += 0.9f / bundles.Length;
				MapManager.ProgressBar("Loading Bundles", "Loading: " + bundles[i], MapManager.progressValue);
				var bundlePath = Path.GetDirectoryName(BundlePath) + Path.DirectorySeparatorChar + bundles[i];
				var asset = AssetBundle.LoadFromFile(bundlePath);
				if (asset == null)
				{
					Debug.LogError("Couldn't load AssetBundle - " + bundlePath);
					return;
				}

				foreach (var filename in asset.GetAllAssetNames())
					AssetPaths.Add(filename, asset);
			}

			Manifest = GetAsset<GameManifest>(ManifestPath);
			if (Manifest == null)
			{
				Debug.LogError("Couldn't load GameManifest.");
				Dispose();
				return;
			}

			for (uint index = 0; (long)index < (long)Manifest.pooledStrings.Length; ++index)
			{
				IDLookup.Add(Manifest.pooledStrings[index].hash, Manifest.pooledStrings[index].str);
				PathLookup.Add(Manifest.pooledStrings[index].str, Manifest.pooledStrings[index].hash);
			}

			AssetDump();
			IsInitialised = true;
			MapManager.ClearProgressBar();
		}
		else
		{
			Debug.Log("Bundle already loaded.");
		}
	}

	public static T GetAsset<T>(string filePath) where T : Object
	{
		AssetBundle bundle = null;

		if (!AssetPaths.TryGetValue(filePath, out bundle))
			return null;

		return bundle.LoadAsset<T>(filePath);
	}

	public static T LoadAsset<T>(string filePath) where T : Object
	{
		var asset = default(T);

		if (Cache.ContainsKey(filePath))
			asset = Cache[filePath] as T;
		else
		{
			asset = GetAsset<T>(filePath);
			if (asset != null)
				Cache.Add(filePath, asset);
		}
		return asset;
	}

	public static GameObject LoadPrefab(string filePath)
	{
		if (Cache.ContainsKey(filePath))
			return Cache[filePath] as GameObject;

		else
		{
			GameObject val = GetAsset<GameObject>(filePath);
			if (val != null)
			{
				PrefabManager.Process(val, filePath);
				Cache.Add(filePath, val);
				return val;
			}
			Debug.LogWarning("Prefab not loaded from bundle: " + filePath);
			return PrefabManager.DefaultPrefab;
		}
	}

	public static void Dispose()
	{
		AssetPaths.Clear();
		Bundles.Clear();
		Cache.Clear();
	}

	public static List<string> GetManifestStrings()
	{
		if (Manifest == null)
			return null;

		List<string> manifestStrings = new List<string>();
		foreach (var item in Manifest.pooledStrings)
			manifestStrings.Add(item.str);

		return manifestStrings;
	}

	/// <summary>Dumps every asset found in the Rust content bundle to a text file.</summary>
	public static void AssetDump()
	{
		using (StreamWriter streamWriter = new StreamWriter("AssetDump.txt", false))
			foreach (var item in AssetPaths.Keys)
				streamWriter.WriteLine(item + " : " + ToID(item));
	}

	public static string ToPath(uint i)
	{
		if ((int)i == 0)
			return string.Empty;
		string str;
		if (IDLookup.TryGetValue(i, out str))
			return str;
		return string.Empty;
	}

	public static uint ToID(string str)
	{
		if (string.IsNullOrEmpty(str))
			return 0;
		uint num;
		if (PathLookup.TryGetValue(str, out num))
			return num;
		return 0;
	}
}