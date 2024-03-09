using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SIncLib
{
    internal class PortingManagerUI : ModBehaviour
    {
        private GUIWindow Window;
        private GUIListView ActiveJobs;
        private readonly string title = "Porting Manager";
        private bool shown;
        private Text TeamText;

        public static PortingManagerUI Instance;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (!SIncLibMod.ModActive || !isActiveAndEnabled)
            {
                return;
            }

            SceneManager.sceneLoaded -= OnLevelFinishedLoading;
            SceneManager.sceneLoaded += OnLevelFinishedLoading;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnLevelFinishedLoading;
        }

        public override void OnDeactivate()
        {
            shown = false;
            if (Window != null)
            {
                Destroy(Window.gameObject);
            }
        }

        private void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
        {
            if (scene == null || scene.name == null)
            {
                return;
            }

            //Other scenes include MainScene and Customization
            if (scene.name.Equals("MainMenu") && Window != null && Window.gameObject != null)
            {
                Destroy(Window.gameObject);
            }
        }

        public override void OnActivate()
        {
        }

        public static void Show()
        {
            if (Instance.shown)
            {
                Instance.Window.Close();
                Instance.shown = false;
                return;
            }

            Instance.Init();
            Instance.shown = true;
        }

        private void Init()
        {
            Window = WindowManager.SpawnWindow();
            Window.InitialTitle = Window.TitleText.text = Window.NonLocTitle = title;
            Window.MinSize.x = 800;
            Window.MinSize.y = 630;
            Window.name = "SIncLibPortingManager";
            Window.MainPanel.name = "SIncLibPortingManagerPanel";

            if (Window.name == "SIncLibPortingManager")
            {
                Window.GetComponentsInChildren<Button>()
                    .SingleOrDefault(x => x.name == "CloseButton")
                    .onClick.AddListener(() => shown = false);
            }

            Utils.AddLabel("Enabled", new Rect(10, 20, 250, 32), Window);
            Utils.AddToggle("", new Rect(270, 20, 250, 32), PortingBehaviour.Instance.Enabled, b =>
            {
                PortingBehaviour.Instance.Enabled = b;
                PlayerPrefs.SetInt("PortingEnabled", b ? 1 : 0);
                PlayerPrefs.Save();
            }, Window);


            //Utils.AddButton("Test Porting", new Rect(500, 50, 250, 32), TestPorting, Window);

            Utils.AddLabel("Porting Teams: ", new Rect(10, 60, 250, 32), Window);
            Button teamButton = Utils.AddButton("SELECT TEAM", new Rect(270, 50, 250, 32), ShowTeamWindow, Window);
            TeamText = teamButton.GetComponentInChildren<Text>();
            UpdateTeamText();

            Utils.AddLabel("Support products for (months): ", new Rect(10, 100, 250, 32), Window);
            Utils.AddIntField(PortingBehaviour.Instance.SupportForMonths, new Rect(270, 90, 250, 32), i =>
            {
                PortingBehaviour.Instance.SupportForMonths = i;
                PlayerPrefs.SetInt("SupportForMonths", i);
                PlayerPrefs.Save();
            }, Window);

            Utils.AddLabel("Port to OS with more than x active users: ", new Rect(10, 140, 250, 32), Window);
            Utils.AddIntField(PortingBehaviour.Instance.MinimumUserbase, new Rect(270, 130, 250, 32), i =>
            {
                PortingBehaviour.Instance.MinimumUserbase = i;
                PlayerPrefs.SetInt("MinimumUserbase", i);
                PlayerPrefs.Save();
            }, Window);

            Utils.AddLabel("Parallel Jobs: ", new Rect(10, 180, 250, 32), Window);
            Utils.AddIntField(PortingBehaviour.Instance.ConcurrentJobs, new Rect(270, 170, 250, 32), i =>
            {
                PortingBehaviour.Instance.ConcurrentJobs = i;
                PlayerPrefs.SetInt("ConcurrentJobs", i);
                PlayerPrefs.Save();
            }, Window);

            Utils.AddLabel("Always port to inhouse OS", new Rect(10, 220, 250, 32), Window);
            Utils.AddToggle("", new Rect(270, 220, 250, 32), PortingBehaviour.Instance.AlwaysPortToInhouseOS, b =>
            {
                PortingBehaviour.Instance.AlwaysPortToInhouseOS = b;
                PlayerPrefs.SetInt("AlwaysPortToInhouseOS", b ? 1 : 0);
                PlayerPrefs.Save();
            }, Window);

            RenderJobWindow();

        }

        private void TestPorting()
        {
            PortingBehaviour.Instance.TimeOfDayOnOnDayPassed(null, null);

            UpdateListView();
        }

        private void RenderJobWindow()
        {
            ActiveJobs = Utils.AddListView(new Rect(10, 250, -20, -200 - 30), Window);

            var nameColumn = (GUIListView.ColumnDef)new GUIListView.ColumnDefinition<PortingJob>("Name", x => x.Product.Name, false, new float?(220f), false, true);
            var osColumn = (GUIListView.ColumnDef)new GUIListView.ColumnDefinition<PortingJob>("OS", x => x.TargetProduct.Name, false, new float?(220f), false, true);
            var teamColumn = (GUIListView.ColumnDef)new GUIListView.ColumnDefinition<PortingJob>("Team", x => x.Team == null ? "None" : x.Team.Name, false, new float?(150f), false, true);
            var progressColumn = (GUIListView.ColumnDef)new GUIListView.ColumnDefinition<PortingJob>("Progress", x => GetProgress(x), false, new float?(100f), false, true);
            var pauseColumn = (GUIListView.ColumnDef)new GUIListView.ColumnDefinition<PortingJob>("Status", x => x.IsPaused ? "Paused" : "Running", false, new float?(100f), false, true);

            ActiveJobs.AddColumn(nameColumn);
            ActiveJobs.AddColumn(osColumn);
            ActiveJobs.AddColumn(teamColumn);
            ActiveJobs.AddColumn(progressColumn);
            ActiveJobs.AddColumn(pauseColumn);

            UpdateListView();
        }

        private static string GetProgress(PortingJob x)
        {
            if (x.WorkItem == null)
            {
                return "Not started";
            }
            if (x.WorkItem.GetCurrentStage().Equals("MockOSPortWait".Loc()))
            {
                return "MockOSPortWait".Loc();
            }
            return Mathf.RoundToInt(x.WorkItem.GetActualProgress() * 100f) + "%";
        }

        private void UpdateListView()
        {
            ActiveJobs.Items = (EventList<object>)PortingBehaviour.Instance.PortingJobQueue.Cast<object>().ToList();
        }

        private void ShowTeamWindow()
        {
            HUD.Instance.TeamSelectWindow.Show(GameSettings.Instance.GetDefaultTeams("Porting"), (SimulatedCompany)null,
                (Action<string[], SimulatedCompany>)((ts, c) =>
                {
                    PortingBehaviour.Instance.PortingTeams.Clear();
                    if (c != null)
                    {
                        PortingBehaviour.Instance.OutsourcedPorting = c;
                        PlayerPrefs.SetInt("OutsourcedPorting", (int)c.ID);
                        PlayerPrefs.DeleteKey("PortingTeams");
                        PlayerPrefs.Save();
                    }
                    else
                    {
                        string teamsString = string.Join(",", ts);
                        foreach (var team in ts)
                        {
                            PortingBehaviour.Instance.PortingTeams.Add(team);
                        }
                        // convert to string and save to player prefs
                        PlayerPrefs.SetString("PortingTeams", teamsString);
                        PlayerPrefs.SetInt("OutsourcedPorting", -1);
                        PlayerPrefs.Save();
                    }
                    UpdateTeamText();
                }), "Porting", "Porting", "SoftwarePort");
        }

        private void UpdateTeamText()
        {
            if (PortingBehaviour.Instance.OutsourcedPorting != null)
            {
                TeamText.text = PortingBehaviour.Instance.OutsourcedPorting.Name;
            }
            else
            {
                PortingBehaviour.Instance.PortingTeams = PortingBehaviour.Instance.PortingTeams
                    .Where(x =>
                        GameSettings.Instance.sActorManager.Teams.ContainsKey(x))
                    .ToHashSet();
                TeamText.text = PortingBehaviour.Instance.PortingTeams.GetListAbbrev("Team");
            }
        }

        private void Update()
        {
            if (!SIncLibMod.ModActive || !isActiveAndEnabled)
            {
                return;
            }
            if (ActiveJobs != null)
            {
                ActiveJobs.UpdateActiveList(true);
                ActiveJobs.UpdateElements();
                ActiveJobs.UpdateInUI();
                UpdateListView();
            }
        }
    }
}