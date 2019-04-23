using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DevConsole;
using UnityEngine;
using UnityEngine.UI;
using Console = DevConsole.Console;
using Debug = UnityEngine.Debug;

namespace SIncLib
{
    public class SIncLibAutoDevUI : ModBehaviour
    {
        private static GUIWindow   Window;
        private static string      title = "Project Management Task Manager";
        public static  bool        shown = false;
        private        GUIListView _devList;
        private        GUIListView _supportList;
        private        GUIListView _marketList;
        private        long         _lastHash;

        public static SIncLibAutoDevUI Instance { get; set; }

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

        private void Update()
        {
            long hash = 0;
            if (!shown) return;
            hash += GameSettings.Instance.MyCompany.WorkItems.Where(wi => wi.AutoDev)
                                .Sum(item => (long)((float)item.GetHashCode() * 0.0001f));

            if (hash == _lastHash) return;
            Console.Log(string.Format("Refreshing list {0}/{1}",hash,_lastHash));
            _lastHash = hash;
            AssignDevItems();
            AssignSupportItems();
            AssignMarketingItems();
        }

        public void CreateWindow()
        {
            Window                = WindowManager.SpawnWindow();
            Window.InitialTitle   = Window.TitleText.text = Window.NonLocTitle = title;
            Window.MinSize.x      = 670;
            Window.MinSize.y      = 800;
            Window.name           = "SIncLibAutoDev";
            Window.MainPanel.name = "SIncLibAutoDevPanel";

            if (Window.name == "SIncLibAutoDev")
            {
                Window.GetComponentsInChildren<Button>()
                      .SingleOrDefault(x => x.name == "CloseButton")
                      .onClick.AddListener(() => shown = false);
            }

            Utils.AddLabel("Dev Tasks", new Rect(10, 10, 250, 35), Window);

            _devList = WindowManager.SpawnList();
            SetupDevColumnDefinition(_devList);
            AssignDevItems();

            WindowManager.AddElementToWindow(_devList.gameObject, Window, new Rect(10, 40, -20, 200),
                                             new Rect(0, 0, 1, 0));

            Utils.AddLabel("Support Tasks", new Rect(10, 250, 250, 35), Window);

            _supportList = WindowManager.SpawnList();
            SetupSupportColumnDefinition(_supportList);
            AssignSupportItems();

            WindowManager.AddElementToWindow(_supportList.gameObject, Window, new Rect(10, 280, -20, 200),
                                             new Rect(0, 0, 1, 0));

            Utils.AddLabel("Marketing Tasks", new Rect(10, 500, 250, 35), Window);

            _marketList = WindowManager.SpawnList();
            SetupMarketColumnDefinition(_marketList);
            AssignMarketingItems();

            WindowManager.AddElementToWindow(_marketList.gameObject, Window, new Rect(10, 530, -20, 200),
                                             new Rect(0, 0, 1, 0));
        }

        private void AssignMarketingItems()
        {
            IEnumerable<WorkItem> marketItems =
                GameSettings.Instance.MyCompany.WorkItems.Where(wi => wi.AutoDev && wi is MarketingPlan);
            _marketList.Items = new EventList<object>(marketItems.Cast<object>().ToList());
            _marketList.Initialize();
        }

        private void AssignSupportItems()
        {
            IEnumerable<WorkItem> supportItems =
                GameSettings.Instance.MyCompany.WorkItems.Where(wi => wi.AutoDev && wi is SupportWork);
            _supportList.Items = new EventList<object>(supportItems.Cast<object>().ToList());
            _supportList.Initialize();
        }

        private void AssignDevItems()
        {
            IEnumerable<WorkItem> devItems =
                GameSettings.Instance.MyCompany.WorkItems.Where(wi => wi.AutoDev && wi is SoftwareWorkItem);
            _devList.Items = new EventList<object>(devItems.Cast<object>().ToList());
            _devList.Initialize();
        }

        private void SetupMarketColumnDefinition(GUIListView marketList)
        {
            NameColumn(marketList);
            marketList.AddColumn("Type", o =>
            {
                MarketingPlan item = o as MarketingPlan;
                Debug.Assert(item != null);
                return item.Type;
            }, null, false);
            marketList.AddColumn("Progress", o =>
            {
                MarketingPlan item = o as MarketingPlan;
                Debug.Assert(item != null);
                return string.Format("{0:00}%", item.GetProgress() * 100f);
            }, (o, o1) =>
            {
                MarketingPlan item  = o as MarketingPlan;
                MarketingPlan item1 = o1 as MarketingPlan;
                Debug.Assert(item != null);
                Debug.Assert(item1 != null);
                return (int) (item.GetProgress() - item1.GetProgress());
            }, true);
            marketList.AddColumn("Budget", o =>
            {
                MarketingPlan item = o as MarketingPlan;
                Debug.Assert(item != null);
                return item.MaxBudget.Currency(false);
            }, (o, o1) =>
            {
                MarketingPlan item  = o as MarketingPlan;
                MarketingPlan item1 = o1 as MarketingPlan;
                Debug.Assert(item != null);
                Debug.Assert(item1 != null);
                return (int) (item.MaxBudget - item1.MaxBudget);
            }, true);
            marketList.AddColumn("Cost", o =>
            {
                MarketingPlan item = o as MarketingPlan;
                Debug.Assert(item != null);
                return item.Spent.Currency(true);
            }, (o, o1) =>
            {
                MarketingPlan item  = o as MarketingPlan;
                MarketingPlan item1 = o1 as MarketingPlan;
                Debug.Assert(item != null);
                Debug.Assert(item1 != null);
                return (int) (item.Spent - item1.Spent);
            }, true);
            marketList.AddActionColumn("Takeover", o =>
            {
                MarketingPlan item = o as MarketingPlan;
                Debug.Assert(item != null);
                item.AutoDev = false;
                item.Hidden  = false;
                var autoDevTasks = GameSettings.Instance.MyCompany.WorkItems.Where(wi => wi is AutoDevWorkItem);
                foreach (var workItem in autoDevTasks)
                {
                    var autoDev = (AutoDevWorkItem) workItem;
                    autoDev.MarketingItems.Remove(item);
                }
            }, false);
        }

        private void SetupSupportColumnDefinition(GUIListView supportList)
        {
            NameColumn(supportList);
            supportList.AddColumn("Active users", o =>
            {
                SupportWork item = o as SupportWork;
                Debug.Assert(item != null);
                return Mathf.RoundToInt(item.TargetProduct.Userbase).ToString("N0");
            }, (o, o1) =>
            {
                SupportWork item  = o as SupportWork;
                SupportWork item1 = o1 as SupportWork;
                Debug.Assert(item != null);
                Debug.Assert(item1 != null);
                return (int) (item.TargetProduct.Userbase - item1.TargetProduct.Userbase);
            }, true);
            supportList.AddColumn("BugsFixed", o =>
            {
                SupportWork item = o as SupportWork;
                Debug.Assert(item != null);
                return Mathf.RoundToInt(item.StartBugs - item.TargetProduct.Bugs).ToString("N0");
            }, (o, o1) =>
            {
                SupportWork item  = o as SupportWork;
                SupportWork item1 = o1 as SupportWork;
                Debug.Assert(item != null);
                Debug.Assert(item1 != null);
                return (int) ((item.StartBugs - item.TargetProduct.Bugs) -
                              (item1.StartBugs - item1.TargetProduct.Bugs));
            }, true);
            supportList.AddColumn("BugsVerified", o =>
            {
                SupportWork item = o as SupportWork;
                Debug.Assert(item != null);
                return Mathf.RoundToInt(item.Verified).ToString("N0");
            }, (o, o1) =>
            {
                SupportWork item  = o as SupportWork;
                SupportWork item1 = o1 as SupportWork;
                Debug.Assert(item != null);
                Debug.Assert(item1 != null);
                return (int) ((item.Verified) - (item1.Verified));
            }, true);
            supportList.AddColumn("TicketsQueued", o =>
            {
                SupportWork item = o as SupportWork;
                Debug.Assert(item != null);
                return Mathf.RoundToInt(item.Tickets.Count).ToString("N0");
            }, (o, o1) =>
            {
                SupportWork item  = o as SupportWork;
                SupportWork item1 = o1 as SupportWork;
                Debug.Assert(item != null);
                Debug.Assert(item1 != null);
                return (int) ((item.Tickets.Count) - (item1.Tickets.Count));
            }, true);
            supportList.AddColumn("TicketsMissed", o =>
            {
                SupportWork item = o as SupportWork;
                Debug.Assert(item != null);
                return Mathf.RoundToInt(item.Missed).ToString("N0");
            }, (o, o1) =>
            {
                SupportWork item  = o as SupportWork;
                SupportWork item1 = o1 as SupportWork;
                Debug.Assert(item != null);
                Debug.Assert(item1 != null);
                return (int) ((item.Missed) - (item1.Missed));
            }, true);
            supportList.AddActionColumn("Assign", o =>
            {
                SupportWork item = o as SupportWork;
                Debug.Assert(item != null);
                item.guiItem.Assign("Support", null);
            }, false);
            supportList.AddActionColumn("Cancel", o =>
            {
                SupportWork item = o as SupportWork;
                Debug.Assert(item != null);
                item.CancelSupport();
            }, false);
        }

        private void SetupDevColumnDefinition(GUIListView listView)
        {
            NameColumn(listView);
            listView.AddColumn("Status", o =>
            {
                WorkItem item = o as WorkItem;
                Debug.Assert(item != null);
                return item.Category();
            }, null, true);
            listView.AddColumn("Phase", o =>
            {
                if (o is SoftwareAlpha)
                {
                    var sa = o as SoftwareAlpha;
                    if (sa.InBeta)
                        return "Beta";
                    if (sa.InDelay)
                        return "Delay";
                    return "Alpha";
                }
                else
                {
                    return o.GetType().ToString();
                }
            }, (o, o1) =>
            {
                string phase1 = "";
                string phase2 = "";
                
                if (o is SoftwareAlpha)
                {
                    var sa = o as SoftwareAlpha;
                    if (sa.InBeta)
                        phase1 = "Beta";
                    if (sa.InDelay)
                        phase1 = "Delay";
                    phase1 = "Alpha";
                }
                
                if (o1 is SoftwareAlpha)
                {
                    var sa = o as SoftwareAlpha;
                    if (sa.InBeta)
                        phase2 = "Beta";
                    if (sa.InDelay)
                        phase2 = "Delay";
                    phase2 = "Alpha";
                }
                
                return String.CompareOrdinal(phase1, phase2);
            }, true);
            listView.AddColumn("Followers", o =>
            {
                SoftwareWorkItem item = o as SoftwareWorkItem;
                Debug.Assert(item != null);
                return Mathf.RoundToInt(item.Followers).ToString("N0");
            }, (o, o1) =>
            {
                SoftwareWorkItem item  = o as SoftwareWorkItem;
                SoftwareWorkItem item1 = o1 as SoftwareWorkItem;
                Debug.Assert(item != null);
                Debug.Assert(item1 != null);
                return (int) (item.Followers - item1.Followers);
            }, true);
            listView.AddColumn("Code", o =>
            {
                if (o is SoftwareAlpha)
                {
                    SoftwareAlpha item = o as SoftwareAlpha;
                    return string.Format("{0:0.##}/{1:0.##}", (item.CodeProgress * item.CodeDevTime),
                                         item.CodeDevTime);
                }
                else
                {
                    return "n/a";
                }
            }, (o, o1) =>
            {
                SoftwareAlpha item  = o as SoftwareAlpha;
                SoftwareAlpha item1 = o1 as SoftwareAlpha;
                float         val1  = item != null ? item.CodeProgress : float.MaxValue;
                float         val2  = item1 != null ? item1.CodeProgress : float.MaxValue;
                return (int) (val1 - val2);
            }, true);
            listView.AddColumn("Art", o =>
            {
                if (o is SoftwareAlpha)
                {
                    SoftwareAlpha item = o as SoftwareAlpha;
                    return string.Format("{0:0.##}/{1:0.##}", (item.ArtProgress * item.ArtDevTime),
                                         item.ArtDevTime);
                }
                else
                {
                    return "n/a";
                }
            }, (o, o1) =>
            {
                SoftwareAlpha item  = o as SoftwareAlpha;
                SoftwareAlpha item1 = o1 as SoftwareAlpha;
                float         val1  = item != null ? item.ArtProgress : float.MaxValue;
                float         val2  = item1 != null ? item1.ArtProgress : float.MaxValue;
                return (int) (val1 - val2);
            }, true);
            listView.AddColumn("StillBugs", o =>
            {
                if (o is SoftwareAlpha)
                {
                    SoftwareAlpha item = o as SoftwareAlpha;
                    if (item.InBeta)
                        return (int) item.FixedBugs;
                }

                return "n/a";
            }, (o, o1) =>
            {
                SoftwareAlpha item  = o as SoftwareAlpha;
                SoftwareAlpha item1 = o1 as SoftwareAlpha;
                float         val1  = item != null && item.InBeta ? item.FixedBugs : float.MaxValue;
                float         val2  = item1 != null && item.InBeta ? item1.FixedBugs : float.MaxValue;
                return (int) (val1 - val2);
            }, true);
            listView.AddActionColumn("Promote", o =>
            {
                SoftwareWorkItem item = o as SoftwareWorkItem;
                Debug.Assert(item != null);
                item.PromoteAction();
            }, false);
        }

        private static void NameColumn(GUIListView listView)
        {
            listView.AddColumn("Name", o =>
            {
                WorkItem item = o as WorkItem;
                Debug.Assert(item != null);
                return item.Name;
            }, (o, o1) =>
            {
                WorkItem item  = o as WorkItem;
                WorkItem item1 = o1 as WorkItem;
                Debug.Assert(item != null && item1 != null);
                return String.CompareOrdinal(item.Name, item1.Name);
            }, false);
        }
    }
}