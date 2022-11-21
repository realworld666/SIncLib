using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SIncLib
{
    internal class SIncLibAddon : ModBehaviour
    {
        private static GUIWindow Window;
        private static string title = "Addon Sales";
        public static bool shown;
        private GUIListView _softwareList;
        private AddOnProduct _selectedProduct;

        public static SIncLibAddon Instance { get; set; }

        private void Awake()
        {
            Instance = this;
        }

        public override void OnDeactivate()
        {
            if (Window != null)
                Destroy(Window.gameObject);
        }

        public override void OnActivate()
        {
        }

        public static void ShowWindow()
        {
            if (shown)
            {
                Window.Close();
                shown = false;
                return;
            }

            Instance.CreateWindow();
            shown = true;
        }

        public void CreateWindow()
        {
            Window = WindowManager.SpawnWindow();
            Window.InitialTitle = Window.TitleText.text = Window.NonLocTitle = title;
            Window.MinSize.x = 670;
            Window.MinSize.y = 800;
            Window.name = "SIncLibAddon";
            Window.MainPanel.name = "SIncLibAddonPanel";

            if (Window.name == "SIncLibAddon")
            {
                Window.GetComponentsInChildren<Button>()
                    .SingleOrDefault(x => x.name == "CloseButton")
                    .onClick.AddListener(() => shown = false);
            }

            _softwareList = WindowManager.SpawnList();
            SetupSoftwareColumnDefinition(_softwareList);
            _softwareList.Items.Clear();
            _softwareList.Items.AddRange(GameSettings.Instance.MyCompany.AddOns);
            _softwareList.Initialize();

            WindowManager.AddElementToWindow(_softwareList.gameObject, Window, new Rect(10, 40, -20, 200),
                new Rect(0, 0, 1, 0));
        }

        private void SetupSoftwareColumnDefinition(GUIListView listView)
        {
            listView.AddColumn("Name", content =>
            {
                AddOnProduct addOnProduct = content as AddOnProduct;
                Debug.Assert(addOnProduct != null);
                return addOnProduct.GetName();
            }, (obj1, obj2) =>
            {
                AddOnProduct addOnProduct1 = obj1 as AddOnProduct;
                AddOnProduct addOnProduct2 = obj2 as AddOnProduct;
                Debug.Assert(addOnProduct1 != null && addOnProduct2 != null);
                return string.CompareOrdinal(addOnProduct1.GetName(), addOnProduct2.GetName());
            }, false);

            listView.AddColumn("Parent Product", content =>
            {
                AddOnProduct addOnProduct = content as AddOnProduct;
                Debug.Assert(addOnProduct != null);
                return addOnProduct.Parent.GetName();
            }, (obj1, obj2) =>
            {
                AddOnProduct addOnProduct1 = obj1 as AddOnProduct;
                AddOnProduct addOnProduct2 = obj2 as AddOnProduct;
                Debug.Assert(addOnProduct1 != null && addOnProduct2 != null);
                return string.CompareOrdinal(addOnProduct1.Parent.GetName(), addOnProduct2.Parent.GetName());
            }, false);

            listView.AddColumn("Release", content =>
            {
                AddOnProduct addOnProduct = content as AddOnProduct;
                Debug.Assert(addOnProduct != null);
                return addOnProduct.Release.ToCompactString();
            }, (obj1, obj2) =>
            {
                AddOnProduct addOnProduct1 = obj1 as AddOnProduct;
                AddOnProduct addOnProduct2 = obj2 as AddOnProduct;
                Debug.Assert(addOnProduct1 != null && addOnProduct2 != null);
                return addOnProduct1.Release.CompareTo(addOnProduct2.Release);
            }, false);

            listView.AddActionColumn("Sales", action =>
            {
                AddOnProduct item = action as AddOnProduct;

                _selectedProduct = item;

            }, false);
        }
    }
}
