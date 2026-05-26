using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace KIM.Dev
{
	[Serializable]
	public struct UIShortcutInfo
	{
		public KeyCode ShortcutKey;
		public Button ShortcutButton;
	}

}