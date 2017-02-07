using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public class ShowPlayerPrefs : EditorWindow, IHasCustomMenu
{
	public enum PrefTypes { UNDEFINED = 0, INT = 1, FLOAT = 2, STRING = 3 };

	#region GUIConstans
	private const float PrefTypeWidth = 45f;
	private const float DefaultRegionsSpace = 15f;
	private const float MinusButtonWidth = 15f;
	#endregion GUIConstans

	private List<string> prefTypesList = new List<string> { PrefTypes.INT.ToString().ToLower(), PrefTypes.FLOAT.ToString().ToLower(), PrefTypes.STRING.ToString().ToLower() };

	private static Dictionary<string, object> currentPrefs = new Dictionary<string, object>();
	private static bool debug = false;

	private string SetPrefKey = "key";
	private PrefTypes SetPrefType = PrefTypes.INT;
	private string SetPrefValue = "value";

	private float lastTimeLoadPrefs = 0f;
	private float updatePrefsRate = 1.5f;

	private void OnEnable()
	{
		ReloadPrefs();
	}

	private void OnGUI()
	{
		if (lastTimeLoadPrefs < (Time.unscaledTime - updatePrefsRate))
		{
			ReloadPrefs();
			lastTimeLoadPrefs = Time.unscaledTime;
		}
		DrawSetPlayerPref();
		DrawExistingPrefs();
	}

	#region DrawGui methods
	private void DrawExistingPrefs()
	{
		KeyValuePair<string, object>[] array = currentPrefs.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			KeyValuePair<string, object> item = array[i];

			GUILayout.BeginHorizontal();
			GUILayout.Label(item.Key, GUILayout.Width(position.width * 0.4f));
			GUILayout.Label(ConvertTypeToEnum(item.Value.GetType()).ToString().ToLower(), GUILayout.Width(PrefTypeWidth));
			GUILayout.Label(item.Value.ToString());
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("-", GUILayout.Width(MinusButtonWidth)))
			{
				DebugLog("Delete pref with key: " + item.Key);
				PlayerPrefs.DeleteKey(item.Key);
				currentPrefs.Remove(item.Key);
				PlayerPrefs.Save();
				ReloadPrefs();
				return;
			}
			GUILayout.EndHorizontal();
		}

		GUILayout.Space(DefaultRegionsSpace);
	}

	private void DrawSetPlayerPref()
	{
		GUILayout.Space(5f);
		GUI.backgroundColor = new Color(0.85f, 0.85f, 0.85f);
		GUI.Box(new Rect(1, 1, position.width - 2, EditorGUIUtility.singleLineHeight * 4 + 10f), "");

		GUI.skin.label.fontStyle = FontStyle.Bold;
		GUILayout.Label("Set PlayerPref");
		GUI.skin.label.fontStyle = FontStyle.Normal;

		GUILayout.BeginHorizontal();
		GUILayout.Label("PlayerPref key:");
		SetPrefKey = GUILayout.TextField(SetPrefKey);
		GUILayout.EndHorizontal();

		GUILayout.Space(5f);

		GUILayout.BeginHorizontal();
		GUILayout.Label("Value:");
		SetPrefValue = GUILayout.TextField(SetPrefValue);

		SetPrefType = (PrefTypes)(EditorGUILayout.Popup((int)SetPrefType - 1, prefTypesList.ToArray(), GUILayout.Width(PrefTypeWidth)) + 1);

		GUI.color = new Color(0.6f, 1f, 0.6f);
		if (GUILayout.Button("Set", GUILayout.Width(50)))
		{
			SetPlayerPref(SetPrefType, SetPrefKey, SetPrefValue);
		}
		GUILayout.EndHorizontal();

		GUILayout.Space(DefaultRegionsSpace);
		GUI.backgroundColor = Color.white;
		GUI.color = Color.white;
	}
	#endregion DrawGui methods

	private void SetPlayerPref(PrefTypes type, string key, string value)
	{
		if (string.IsNullOrEmpty(key))
		{
			DebugErroLog("Empty key for Pref");
			return;
		}

		if (type == PrefTypes.STRING)
		{
			DebugLog("PlayerPrefs.SetString " + key + " " + value);
			PlayerPrefs.SetString(key, value);
		}
		else if (type == PrefTypes.INT)
		{
			int val = 0;
			if (int.TryParse(value, out val))
			{
				DebugLog("PlayerPrefs.SetInt " + key + " " + val);
				PlayerPrefs.SetInt(key, val);
			}
			else
			{
				DebugErroLog("Cannot cast " + value + " to int");
			}
		}
		else if (type == PrefTypes.FLOAT)
		{
			float val = 0;
			if (float.TryParse(value, out val))
			{
				DebugLog("PlayerPrefs.SetFloat " + key + " " + val);
				PlayerPrefs.SetFloat(key, val);
			}
			else
			{
				DebugErroLog("Cannot cast " + value + " to float");
			}
		}
		else if (type == PrefTypes.UNDEFINED)
		{
			DebugErroLog("Type is undefined. Change it");
		}
		PlayerPrefs.Save();
		ReloadPrefs();
	}

	private PrefTypes ConvertTypeToEnum(Type type)
	{
		if (type == typeof(Int32))
		{
			return PrefTypes.INT;
		}
		else if(type == typeof(Double))
		{
			return PrefTypes.FLOAT;
		}
		else if (type == typeof(String))
		{
			return PrefTypes.STRING;
		}
		return PrefTypes.UNDEFINED;
	}

	#region LoadPrefs
	private void ReloadPrefs()
	{
		currentPrefs = GetMacPrefs();
		List<string> deleteList = currentPrefs.Keys.Where(key => key.StartsWith("unity.")).ToList();
		for (int i = 0; i < deleteList.Count; i++)
		{
			currentPrefs.Remove(deleteList[i]);
		}
	}

	private static Dictionary<string, object> GetMacPrefs()
	{
		string prefsFileName = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal)
			+ "/Library/Preferences/unity."
			+ PlayerSettings.companyName + "."
			+ PlayerSettings.productName + ".plist";

		Dictionary<string, object> plist;
		try
		{
			plist = Plist.createDictionaryFromBinaryFile(prefsFileName);
		}
		catch (System.Exception eouter)
		{
			Debug.Log("Couldn't read binary prefs: " + eouter.ToString());
			try
			{
				plist = Plist.createDictionaryFromXmlFile(prefsFileName);
			}
			catch (System.Exception einner)
			{
				Debug.Log("Couldn't read xml prefs: " + einner.ToString());
				return null;
			}
		}
		return plist;
	}
	#endregion LoadPrefs

	#region Menus
	[MenuItem("Window/PlayerPrefs")]
	private static void ShowWindow()
	{
		var window = EditorWindow.GetWindow(typeof(ShowPlayerPrefs));
		window.minSize = new Vector2(250f, 300f);
		window.maxSize = new Vector2(400f, 600f);
	}

	[ContextMenu("Switch Debug Mode")]
	private static void SwitchDebugMode()
	{
		debug = !debug;
	}
	#endregion

	#region Log
	private static void DebugLog(object message)
	{
		if (debug)
		{
			Debug.Log(message);
		}
	}

	private static void DebugErroLog(object message)
	{
		if (debug)
		{
			Debug.LogError(message);
		}
	}
	#endregion Log

	public void AddItemsToMenu(GenericMenu menu)
	{
		menu.AddItem(new GUIContent("Debug mode"), debug, SwitchDebugMode);
	}
}
