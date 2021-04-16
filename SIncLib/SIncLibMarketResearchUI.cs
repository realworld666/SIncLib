using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SIncLib
{
    internal class SIncLibMarketResearchUI : ModBehaviour
    {
        private static GUIWindow   Window;
        private static string      title = "Market Researcha";
        public static  bool        shown;
        private        GUIListView _softwareList;
        private SoftwareProduct _selectedProduct;

        public static SIncLibMarketResearchUI Instance { get; set; }
        
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
            Window                = WindowManager.SpawnWindow();
            Window.InitialTitle   = Window.TitleText.text = Window.NonLocTitle = title;
            Window.MinSize.x      = 670;
            Window.MinSize.y      = 800;
            Window.name           = "SIncLibMarketResearch";
            Window.MainPanel.name = "SIncLibAMarketResearchPanel";

            if (Window.name == "SIncLibMarketResearch")
            {
                Window.GetComponentsInChildren<Button>()
                    .SingleOrDefault(x => x.name == "CloseButton")
                    .onClick.AddListener(() => shown = false);
            }
            
            _softwareList = WindowManager.SpawnList();
            SetupSoftwareColumnDefinition(_softwareList);
            _softwareList.Items.Clear();
            _softwareList.Items.AddRange( GameSettings.Instance.MyCompany.Products );
            _softwareList.Initialize();
            
            WindowManager.AddElementToWindow(_softwareList.gameObject, Window, new Rect(10, 40, -20, 200),
                new Rect(0, 0, 1, 0));
        }
        
        private void SetupSoftwareColumnDefinition(GUIListView listView)
        {
            listView.AddColumn("Name", content =>
            {
                SoftwareProduct softwareProduct = content as SoftwareProduct;
                Debug.Assert(softwareProduct != null);
                return softwareProduct.Name;
            }, (obj1, obj2) =>
            {
                SoftwareProduct softwareProduct1 = obj1 as SoftwareProduct;
                SoftwareProduct softwareProduct2 = obj2 as SoftwareProduct;
                Debug.Assert(softwareProduct1 != null && softwareProduct2 != null);
                return string.CompareOrdinal(softwareProduct1.Name, softwareProduct2.Name);
            }, false);
            
            listView.AddColumn("Release", content =>
            {
                SoftwareProduct softwareProduct = content as SoftwareProduct;
                Debug.Assert(softwareProduct != null);
                return softwareProduct.Release.ToCompactString();
            }, (obj1, obj2) =>
            {
                SoftwareProduct softwareProduct1 = obj1 as SoftwareProduct;
                SoftwareProduct softwareProduct2 = obj2 as SoftwareProduct;
                Debug.Assert(softwareProduct1 != null && softwareProduct2 != null);
                return softwareProduct1.Release.CompareTo(softwareProduct2.Release);
            }, false);
            
            listView.AddActionColumn("Inspect", action =>
            {
                SoftwareProduct item = action as SoftwareProduct;

                _selectedProduct = item;
                
            }, false);
        }
    }
}