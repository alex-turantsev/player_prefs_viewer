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
	private const float MinusButtonWidth = 17f;
	#endregion GUIConstans

	private List<string> prefTypesList = new List<string> { PrefTypes.INT.ToString().ToLower(), PrefTypes.FLOAT.ToString().ToLower(), PrefTypes.STRING.ToString().ToLower() };

	private static Dictionary<string, object> localPrefs = new Dictionary<string, object>();


	private static Dictionary<string, object> currentPrefs = new Dictionary<string, object>();
	private static bool debug = false;

	private string SetPrefKey = "key";
	private PrefTypes SetPrefType = PrefTypes.INT;
	private string SetPrefValue = "value";

	private double lastTimeLoadPrefs = 0f;
	private double updatePrefsRate = 0.1f;

	private string errorText_ = string.Empty;

	private string errorText
	{
		get { return errorText_; } 
		set { showErroBox = true; errorText_ = value; }
	}

	private bool showErroBox = false;

	private void OnEnable()
	{
		ReloadPrefs();
		localPrefs = currentPrefs;
	}

	private void OnGUI()
	{
		DrawSetPlayerPref();
		if (localPrefs != null)
		{
			DrawExistingPrefs();

		}
	}

	#region DrawGui methods
	private void DrawExistingPrefs()
	{
		Event evt = Event.current;
		KeyValuePair<string, object>[] array = localPrefs.ToArray();
		Array.Sort(array, (x, y) => String.Compare(x.Key, y.Key));
		for (int i = 0; i < array.Length; i++)
		{
			float width = position.width;
			float deltaItem = 6f;
			KeyValuePair<string, object> item = array[i];

			Rect lastRect = GUILayoutUtility.GetLastRect();
			lastRect.x = 2f;
			lastRect.y += EditorGUIUtility.singleLineHeight + 3f;
			lastRect.width = position.width - MinusButtonWidth - 6f;

			if ((evt.type == EventType.MouseDown
				 || evt.type == EventType.mouseDown
				 || evt.type == EventType.mouseUp
				 || evt.type == EventType.MouseUp) && lastRect.Contains(evt.mousePosition))
			{
				SetPrefKey = item.Key;
				SetPrefType = ConvertTypeToEnum(item.Value.GetType());
				SetPrefValue = item.Value.ToString();
				DebugLog(" change values");
				Repaint();
			}

			GUILayout.BeginHorizontal();
			GUILayout.TextField(item.Key, GUILayout.Width(position.width * 0.3f));
			width -= position.width * 0.3f;
			width -= deltaItem;

			GUILayout.Label(ConvertTypeToEnum(item.Value.GetType()).ToString().ToLower(), GUILayout.Width(PrefTypeWidth-10f));
			width -= (PrefTypeWidth - 10f);
			width -= deltaItem;

			GUILayout.TextField(item.Value.ToString(), GUILayout.Width(width - MinusButtonWidth - deltaItem));

			if (GUILayout.Button("X", GUILayout.Width(MinusButtonWidth)))
			{
				DebugLog("Delete pref with key: " + item.Key);
				PlayerPrefs.DeleteKey(item.Key);
				localPrefs.Remove(item.Key);
				PlayerPrefs.Save();
				return;
			}
			GUILayout.EndHorizontal();
		}
		GUILayout.Space(DefaultRegionsSpace);
	}

	private void DrawSetPlayerPref()
	{
		string tempStr = string.Empty;
		PrefTypes tempType = PrefTypes.UNDEFINED;
		GUILayout.Space(5f);
		GUI.backgroundColor = new Color(0.85f, 0.85f, 0.85f);
		float boxHeight = EditorGUIUtility.singleLineHeight * 4 + 2f * 4 + (showErroBox ? 40f : 0f);
		GUI.Box(new Rect(1, 1, position.width - 2, boxHeight), "");

		GUI.skin.label.fontStyle = FontStyle.Bold;
		GUILayout.Label("Set PlayerPref");
		GUI.skin.label.fontStyle = FontStyle.Normal;

		GUILayout.BeginHorizontal();
		GUILayout.Label("PlayerPref key:", GUILayout.Width(85f));
		tempStr = GUILayout.TextField(SetPrefKey, GUILayout.Width(position.width - 85f - 15f));
		if (tempStr != SetPrefKey)
		{
			OnSetPrefValuesChanged();
			Debug.Log("Key wa changed");
		}
		SetPrefKey = tempStr;
		GUILayout.EndHorizontal();

		GUILayout.Space(5f);

		GUILayout.BeginHorizontal();
		GUILayout.Label("Value:", GUILayout.Width(35f));
		tempStr = GUILayout.TextField(SetPrefValue, GUILayout.Width(position.width - 50f - PrefTypeWidth - 35f - 20f));
		if (tempStr != SetPrefValue)
		{
			OnSetPrefValuesChanged();
		}
		SetPrefValue = tempStr;

		tempType = (PrefTypes)(EditorGUILayout.Popup((int)SetPrefType - 1, prefTypesList.ToArray(), GUILayout.Width(PrefTypeWidth)) + 1);
		if (tempType != SetPrefType)
		{
			OnSetPrefValuesChanged();
		}
		SetPrefType = tempType;

		GUI.color = new Color(0.6f, 1f, 0.6f);
		if (GUILayout.Button("Set", GUILayout.Width(50f)))
		{
			SetPlayerPref(SetPrefType, SetPrefKey, SetPrefValue);
		}
		GUILayout.EndHorizontal();

		GUI.backgroundColor = Color.white;
		GUI.color = Color.white;

		if (showErroBox)
		{
			EditorGUILayout.HelpBox(errorText, MessageType.Error, true);
		}

		GUILayout.Space(DefaultRegionsSpace);
	}
	#endregion DrawGui methods

	private void OnSetPrefValuesChanged()
	{
		showErroBox = false;
	}

	private void Update()
	{
		if (lastTimeLoadPrefs < (EditorApplication.timeSinceStartup - updatePrefsRate))
		{
			ReloadPrefs();
			lastTimeLoadPrefs = EditorApplication.timeSinceStartup;
		}
	}

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
			UpdateValue(key, value);
		}
		else if (type == PrefTypes.INT)
		{
			int val = 0;
			if (int.TryParse(value, out val))
			{
				DebugLog("PlayerPrefs.SetInt " + key + " " + val);
				PlayerPrefs.SetInt(key, val);
				UpdateValue(key, val);
			}
			else
			{
				errorText = "Cannot cast " + value + " to int";
				DebugErroLog(errorText);
			}
		}
		else if (type == PrefTypes.FLOAT)
		{
			float val = 0;
			if (float.TryParse(value, out val))
			{
				DebugLog("PlayerPrefs.SetFloat " + key + " " + val);
				PlayerPrefs.SetFloat(key, val);
				UpdateValue(key, val);
			}
			else
			{
				errorText = "Cannot cast " + value + " to float";
				DebugErroLog(errorText);
			}
		}
		else if (type == PrefTypes.UNDEFINED)
		{
			DebugErroLog("Type is undefined. Change it");
		}
		PlayerPrefs.Save();
	}

	private void UpdateValue(string key, object value)
	{
		if (!localPrefs.ContainsKey(key))
		{
			localPrefs.Add(key, value);
		}
		else 
		{
			localPrefs[key] = value;
		}
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
		if (currentPrefs != null)
		{
			List<string> deleteList = currentPrefs.Keys.Where(key => key.StartsWith("unity.")).ToList();
			for (int i = 0; i < deleteList.Count; i++)
			{
				currentPrefs.Remove(deleteList[i]);
			}
		}
		Repaint();
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
