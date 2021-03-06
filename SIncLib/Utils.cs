using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SIncLib
{
    public class Utils
    {
        public static Button AddButton(string Text, UnityAction Action, ref List<GameObject> Buttons)
        {
            Button x = WindowManager.SpawnButton();
            x.GetComponentInChildren<Text>().text = Text;
            x.onClick.AddListener(Action);
            Buttons.Add(x.gameObject);
            return x;
        }
        
        public static Button AddButton(string Text, Rect Button, UnityAction Action)
        {
            Button x = WindowManager.SpawnButton();
            x.GetComponentInChildren<UnityEngine.UI.Text>().text = Text;
            x.onClick.AddListener(Action);
            WindowManager.AddElementToWindow(x.gameObject, SIncLibUI.Window, Button, new Rect(0, 0, 0, 0));
            return x;
        }

        public static void AddInputBox(string Text, Rect InputBox, UnityAction<string> Action)
        {
            InputField x = WindowManager.SpawnInputbox();
            x.text = Text;
            x.onValueChanged.AddListener(Action);
            WindowManager.AddElementToWindow(x.gameObject, SIncLibUI.Window, InputBox, new Rect(0, 0, 0, 0));
        }

        public static void AddLabel(string Text, Rect Label, GUIWindow window = null)
        {
            if ( window == null )
                window = SIncLibUI.Window;
            
            Text x = WindowManager.SpawnLabel();
            x.text = Text;
            WindowManager.AddElementToWindow(x.gameObject, window, Label, new Rect(0, 0, 0, 0));
        }

        public static void AddToggle(string Text, Rect rect, bool isOn, UnityAction<bool> Action)
        {
            Toggle toggle = WindowManager.SpawnCheckbox();
            toggle.GetComponentInChildren<UnityEngine.UI.Text>().text = Text;
            toggle.isOn = isOn;
            toggle.onValueChanged.AddListener(Action);
            WindowManager.AddElementToWindow(toggle.gameObject, SIncLibUI.Window, rect, new Rect(0, 0, 0, 0));
        }

        public static void DoLoops(GameObject[] Buttons, GameObject[] Col1, GameObject[] Col2, GameObject[] Col3)
        {
            for (int i = 0; i < Buttons.Length; i++)
            {
                GameObject item = Buttons[i];

                WindowManager.AddElementToWindow(item, SIncLibUI.Window, new Rect(1, (i + 7) * 32, 150, 32),
                    new Rect(0, 0, 0, 0));
            }

            for (int i = 0; i < Col1.Length; i++)
            {
                GameObject item = Col1[i];

                WindowManager.AddElementToWindow(item, SIncLibUI.Window, new Rect(161, (i + 7) * 32, 150, 32),
                    new Rect(0, 0, 0, 0));
            }

            for (int i = 0; i < Col2.Length; i++)
            {
                GameObject item = Col2[i];

                WindowManager.AddElementToWindow(item, SIncLibUI.Window, new Rect(322, (i + 7) * 32, 150, 32),
                    new Rect(0, 0, 0, 0));
            }

            for (int i = 0; i < Col3.Length; i++)
            {
                GameObject item = Col3[i];

                WindowManager.AddElementToWindow(item, SIncLibUI.Window, new Rect(483, (i + 7) * 32, 150, 32),
                    new Rect(0, 0, 0, 0));
            }
        }
    }
}