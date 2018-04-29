using System;
using System.Collections.Generic;
using UnityEngine;
using BedrockFramework.Utilities;

namespace BedrockFramework.DevTools
{
	public class DebugMenu : MonoBehaviour {
		class DebugItem
		{
			string title;
			Action action;

			public DebugItem(string title, Action action)
			{
				this.title = title;
				this.action = action;
			}

			public bool OnGUI_DrawItem()
			{
				if (action == null)
				{
					GUILayout.Label(title, GUILayout.MaxHeight(_DebugMainHeight), GUILayout.MaxWidth(DebugCategory._DebugCategoryMaxWidth));
					return false;
				}

				if (GUILayout.Button(title, GUILayout.MaxHeight(_DebugMainHeight), GUILayout.MaxWidth(DebugCategory._DebugCategoryMaxWidth)))
				{
					action();
					return true;
				}

				return false;
			}
		}

		class DebugCategory
		{
			public const int _DebugCategoryMaxWidth = 128;

			string title;
			Color colour;
			Rect buttonRect;
			int menuItemHeight;

			List<DebugItem> debugItems = new List<DebugItem>();

			public string Title { get { return title; } }

			public DebugCategory(string title, Color colour)
			{
				this.title = title;
				this.colour = colour;
			}

			public void AddDebugItem(DebugItem item)
			{
				debugItems.Add(item);
				menuItemHeight = _DebugMainHeight + (debugItems.Count * _DebugMainHeight);
			}

			public bool OnGUI_DrawCategory()
			{
				GUI.backgroundColor = colour;
				Rect buttonArea = GUILayoutUtility.GetRect(new GUIContent(title), GUI.skin.button, GUILayout.MaxWidth(_DebugCategoryMaxWidth));
				bool clicked = GUI.Button(buttonArea, title);

				if (clicked)
					buttonRect = buttonArea;

				return clicked;
			}

			public bool OnGUI_DrawDropDownMenu()
			{
				Rect menuItemArea = new Rect(buttonRect.position, new Vector2(buttonRect.width, menuItemHeight));

				if (Input.GetMouseButtonDown(0))
				{
					if (!menuItemArea.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)))
					{
						return false;
					}
				}

				GUI.backgroundColor = Color.white;
				GUILayout.BeginArea(menuItemArea, GUI.skin.box);
				GUILayout.BeginVertical();
				GUILayoutUtility.GetRect(_DebugCategoryMaxWidth, _DebugMainHeight);
				foreach (DebugItem debugItem in debugItems)
				{
					if (debugItem.OnGUI_DrawItem())
						return false;
				}
				GUILayout.EndVertical();
				GUILayout.EndArea();

				return true;
			}
		}

		const int _DebugMainHeight = 16;

		[SerializeField]
		GUISkin menuSkin;

		[SerializeField]
		Color[] categoryColours;

		static DebugMenu instance;
		List<DebugCategory> debugCategories = new List<DebugCategory>();
		DebugCategory activeCategory;

		static DebugMenu Instance
		{
			get
			{
				if (instance == null)
				{
					instance = FindObjectOfType<DebugMenu>();
				}

				return instance;
			}
		}

		public static void AddDebugItem(string categoryTitle, string name, Action action = null)
		{
            if (Instance == null)
                return;

			DebugCategory category = Instance.GetCategory(categoryTitle);
			if (category == null)
				category = Instance.CreateCategory(categoryTitle);

			category.AddDebugItem(new DebugItem(name, action));
		}

		#region Categories

		DebugCategory GetCategory(string categoryTitle)
		{
			for (int i = 0; i < debugCategories.Count; i++)
			{
				if (debugCategories[i].Title == categoryTitle)
					return debugCategories[i];
			}
			return null;
		}

		DebugCategory CreateCategory(string categoryTitle)
		{
			DebugCategory category = new DebugCategory(categoryTitle, categoryColours[debugCategories.Count.Wrap(0, categoryColours.Length)]);
			debugCategories.Add(category);
			return category;
		}

		#endregion

        void Awake()
        {
            if (!Debug.isDebugBuild)
                DestroyImmediate(this);
        }

		void OnGUI()
		{
			GUI.skin = menuSkin;
			DrawCategories();

			if (activeCategory != null)
			{
				if (!activeCategory.OnGUI_DrawDropDownMenu())
					activeCategory = null;
			}
		}

		void DrawCategories()
		{
			GUILayout.BeginArea(new Rect(0, 0, Screen.width, _DebugMainHeight), menuSkin.box);
			GUILayout.BeginHorizontal();
			foreach (DebugCategory category in debugCategories)
			{
				if (category.OnGUI_DrawCategory())
					activeCategory = category;
			}
			GUILayout.EndHorizontal();
			GUILayout.EndArea();
		}
	}
}
