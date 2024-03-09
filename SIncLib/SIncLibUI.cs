using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SIncLib
{
    public class SIncLibUI : ModBehaviour
    {
        public static Button btn;

        public static GUIWindow Window;
        private static readonly string title = "SIncLib by Otters Pocket - v" + SIncLibMod.Version;
        public static bool shown;
        private static HashSet<string> DevTeams = new HashSet<string>();
        public static HashSet<string> SerialTeams = new HashSet<string>();
        private static Text TeamText;
        private static Text SourceText;
        private static Text SerialText;
        private static RectTransform Panel;

        public static void SpawnButton()
        {
            btn = WindowManager.SpawnButton();
            btn.GetComponentInChildren<Text>().text = "SIncLib";
            btn.onClick.AddListener(Show);
            btn.name = "SIncLibButton";

            WindowManager.AddElementToElement(btn.gameObject,
                WindowManager.FindElementPath("MainPanel/Holder/FanPanel").gameObject,
                new Rect(264, 0, 100, 32),
                new Rect(0, 0, 0, 0));
        }

        private void Start()
        {
            SceneManager.sceneLoaded -= OnLevelFinishedLoading;
            SceneManager.sceneLoaded += OnLevelFinishedLoading;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnLevelFinishedLoading;
        }

        private void OnLevelFinishedLoading(Scene scene, LoadSceneMode arg1)
        {
            if (scene == null || scene.name == null)
            {
                return;
            }

            if (SceneManager.GetActiveScene().name.Equals("MainScene"))
            {
                SpawnButton();
            }

            //Other scenes include MainScene and Customization
            if (scene.name.Equals("MainMenu") && Window != null && Window.gameObject != null)
            {
                OnDeactivate();
            }
        }

        public override void OnDeactivate()
        {
            if (btn != null)
            {
                Destroy(btn.gameObject);
            }

            if (Window != null)
            {
                Destroy(Window.gameObject);
            }

            shown = false;
        }

        public override void OnActivate()
        {
            if (SceneManager.GetActiveScene().name.Equals("MainScene"))
            {
                SpawnButton();
            }
        }

        public static void Show()
        {
            if (shown)
            {
                Window.Close();
                shown = false;
                return;
            }

            Init();
            shown = true;
        }

        private static void Init()
        {
            DevTeams.Clear();
            DevTeams.AddRange(GameSettings.Instance.GetDefaultTeams("Design"));

            Window = WindowManager.SpawnWindow();
            Window.InitialTitle = Window.TitleText.text = Window.NonLocTitle = title;
            Window.MinSize.x = 700;
            Window.MinSize.y = 400;
            Window.name = "SIncLibOptions";
            Window.MainPanel.name = "SIncLibOptionsPanel";

            if (Window.name == "SIncLibOptions")
            {
                Window.GetComponentsInChildren<Button>()
                    .SingleOrDefault(x => x.name == "CloseButton")
                    .onClick.AddListener(() => shown = false);
            }

            Utils.AddButton("Game Mods", new Rect(10, 10, 250, 32), () => ShowGameMods(), Window);
            Utils.AddButton("Team Manager", new Rect(10, 50, 250, 32), () => ShowTeamManager(), Window);
            Utils.AddButton("Porting Manager", new Rect(10, 90, 250, 32), () => PortingManagerUI.Show(), Window);


            ShowGameMods();
        }

        private static void ShowGameMods()
        {
            if (Panel != null)
            {
                Destroy(Panel.gameObject);
            }
            Panel = Utils.AddPanel(new Rect(270, 10, 250, -1));

            Utils.AddToggle("Out of stock notifications for acquired software",
                new Rect(10, 10, 600, 32),
                SIncLibBehaviour.Instance.StockNotifications,
                state =>
                {
                    SIncLibBehaviour.Instance.StockNotifications = state;
                    PlayerPrefs.SetInt("SIncLib_StockNotifications", state ? 1 : 0);
                    PlayerPrefs.Save();
                }, Panel.gameObject);

            Utils.AddLabel("Auto stock levels at % of last months sales", new Rect(10, 50, 600, 32), Panel.gameObject);
            Utils.AddIntField(Mathf.CeilToInt(SIncLibBehaviour.Instance.ManageStock * 100),
                new Rect(290, 40, 130, 32),
                value =>
                {
                    SIncLibBehaviour.Instance.ManageStock = value / 100f;
                    PlayerPrefs.SetFloat("SIncLib_ManageStock", SIncLibBehaviour.Instance.ManageStock);
                    PlayerPrefs.Save();
                }, Panel.gameObject);

            Utils.AddToggle("Show Notifications When Stock Bought?",
                new Rect(10, 90, 600, 32),
                SIncLibBehaviour.Instance.ManageStockNotifications,
                state =>
                {
                    SIncLibBehaviour.Instance.ManageStockNotifications = state;
                    PlayerPrefs.SetInt("SIncLib_ManageStockNotifications", state ? 1 : 0);
                    PlayerPrefs.Save();
                }, Panel.gameObject);

            Utils.AddToggle("Hide Printing Complete Notifications",
                               new Rect(10, 130, 600, 32),
                                              SIncLibBehaviour.Instance.HidePrintingNotifications,
                                                             state =>
                                                             {
                                                                 SIncLibBehaviour.Instance.HidePrintingNotifications = state;
                                                                 PlayerPrefs.SetInt("SIncLib_HidePrintingNotifications", state ? 1 : 0);
                                                                 PlayerPrefs.Save();
                                                             }, Panel.gameObject);
        }

        private static void ShowTeamManager()
        {
            if (Panel != null)
            {
                Destroy(Panel.gameObject);
            }

            Panel = Utils.AddPanel(new Rect(270, 10, 250, -1));

            Utils.AddLabel("Team:", new Rect(10, 10, 250, 32), Panel.gameObject);

            Button teamButton = Utils.AddButton("SELECT TEAM", new Rect(150, 5, 250, 32), ShowTeamWindow, Panel.gameObject);
            TeamText = teamButton.GetComponentInChildren<Text>();

            Utils.AddLabel("Source From:", new Rect(10, 50, 250, 32), Panel.gameObject);

            Button sourceButton = Utils.AddButton("SOURCE", new Rect(150, 45, 250, 32), ShowMultiTeamWindow, Panel.gameObject);
            SourceText = sourceButton.GetComponentInChildren<Text>();
            UpdateTeamText();

            Utils.AddLabel("Adjust only:", new Rect(10, 90, 250, 32), Panel.gameObject);

            Utils.AddToggle("Code",
                new Rect(150, 90, 250, 32),
                (SIncLibBehaviour.Instance.AdjustDepartment & SIncLibBehaviour.AdjustHRFlags.Code) != 0,
                state =>
                {
                    if (state)
                    {
                        SIncLibBehaviour.Instance.AdjustDepartment |= SIncLibBehaviour.AdjustHRFlags.Code;
                    }
                    else
                    {
                        SIncLibBehaviour.Instance.AdjustDepartment &= ~SIncLibBehaviour.AdjustHRFlags.Code;
                    }
                }, Panel.gameObject);
            Utils.AddToggle("Art",
                new Rect(250, 90, 250, 32),
                (SIncLibBehaviour.Instance.AdjustDepartment & SIncLibBehaviour.AdjustHRFlags.Art) != 0,
                state =>
                {
                    if (state)
                    {
                        SIncLibBehaviour.Instance.AdjustDepartment |= SIncLibBehaviour.AdjustHRFlags.Art;
                    }
                    else
                    {
                        SIncLibBehaviour.Instance.AdjustDepartment &= ~SIncLibBehaviour.AdjustHRFlags.Art;
                    }
                }, Panel.gameObject);
            Utils.AddToggle("Design",
                new Rect(350, 90, 250, 32),
                (SIncLibBehaviour.Instance.AdjustDepartment & SIncLibBehaviour.AdjustHRFlags.Design) != 0,
                state =>
                {
                    if (state)
                    {
                        SIncLibBehaviour.Instance.AdjustDepartment |= SIncLibBehaviour.AdjustHRFlags.Design;
                    }
                    else
                    {
                        SIncLibBehaviour.Instance.AdjustDepartment &=
                            ~SIncLibBehaviour.AdjustHRFlags.Design;
                    }
                }, Panel.gameObject);

            Utils.AddLabel("Transfer Idle Only", new Rect(10, 130, 250, 32), Panel.gameObject);
            Utils.AddToggle("",
                new Rect(150, 125, 250, 32),
                SIncLibBehaviour.Instance.IdleOnly,
                state => { SIncLibBehaviour.Instance.IdleOnly = state; }, Panel.gameObject);

            Utils.AddLabel("Adjust HR rules:", new Rect(10, 170, 250, 32), Panel.gameObject);
            Utils.AddToggle("",
                new Rect(150, 165, 250, 32),
                SIncLibBehaviour.Instance.AdjustHR,
                state => { SIncLibBehaviour.Instance.AdjustHR = state; }, Panel.gameObject);

            Utils.AddButton("Optimise Team", new Rect(10, 220, 400, 32), () => OptimiseTeam(), Panel.gameObject);
        }

        private static void OptimiseTeam()
        {
            if (TeamText.text.Equals("SELECT TEAM"))
            {
                return;
            }

            if (!GameSettings.Instance.sActorManager.Teams.ContainsKey(TeamText.text))
            {
                HUD.Instance.AddPopupMessage("Could not find team with name " + TeamText.text,
                    "Exclamation",
                    PopupManager.PopUpAction.None,
                    0U,
                    PopupManager.NotificationSound.Issue,
                    2f);
                return;
            }

            if (!DevTeams.Any())
            {
                HUD.Instance
                    .AddPopupMessage(
                        "You need to select at least one team to pull team members from excluding the target team",
                        "Exclamation",
                        PopupManager.PopUpAction.None,
                        0U,
                        PopupManager.NotificationSound.Issue,
                        2f);
                return;
            }

            Team team = GameSettings.Instance.sActorManager.Teams[TeamText.text];
            string error = "";
            if (!SIncLibBehaviour.Instance.TransferBestAvailableStaff(team,
                    DevTeams.Select(t => GameSettings
                        .Instance.sActorManager
                        .Teams[t]).ToArray(),
                    out error))
            {
                HUD.Instance.AddPopupMessage("Could not optimise team: " + error,
                    "Exclamation",
                    PopupManager.PopUpAction.None,
                    0U,
                    PopupManager.NotificationSound.Issue,
                    2f);
                return;
            }

            HUD.Instance.AddPopupMessage("Team optimised!",
                "Cogs",
                PopupManager.PopUpAction.None,
                0,
                PopupManager.NotificationSound.Neutral,
                0f);
        }

        private static void ShowTeamWindow()
        {
            HUD.Instance.TeamSelectWindow.Show(true, TeamText.text, SetTeamName, null);
        }

        private static void SetTeamName(string[] t)
        {
            TeamText.text = t.First();
        }

        private static void ShowMultiTeamWindow()
        {
            HUD.Instance.TeamSelectWindow.Show(false,
                DevTeams,
                t =>
                {
                    DevTeams.Clear();
                    DevTeams.AddRange(t);
                    UpdateTeamText();
                },
                null);
        }

        private static void ShowSerialTeamWindow()
        {
            HUD.Instance.TeamSelectWindow.Show(false,
                SerialTeams,
                t =>
                {
                    SerialTeams.Clear();
                    SerialTeams.AddRange(t);
                    UpdateSerialTeamText();
                },
                null);
        }

        public static void UpdateTeamText()
        {
            DevTeams = DevTeams
                .Where(x =>
                    GameSettings.Instance.sActorManager.Teams.ContainsKey(x))
                .ToHashSet();
            SourceText.text = DevTeams.GetListAbbrev("Team");
        }

        public static void UpdateSerialTeamText()
        {
            SerialTeams = SerialTeams
                .Where(x =>
                    GameSettings.Instance.sActorManager.Teams.ContainsKey(x))
                .ToHashSet();
            SerialText.text = SerialTeams.GetListAbbrev("Team");
        }
    }
}