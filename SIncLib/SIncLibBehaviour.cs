using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Console = DevConsole.Console;

namespace SIncLib
{
    public class SIncLibBehaviour : ModBehaviour
    {
        public static SIncLibBehaviour Instance;

        private int _adjustDepartment = 0;

        /// <summary>
        /// For some reason unknown to me, the dynamic compiler does not like this when its an enum
        /// </summary>
        public struct AdjustHRFlags
        {
            public const int Code   = 1;
            public const int Art    = 2;
            public const int Design = 4;
        };

        public int AdjustDepartment
        {
            get { return _adjustDepartment; }
            set { _adjustDepartment = value; }
        }

        public bool IdleOnly { get; set; }
        public bool AdjustHR { get; set; }

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

            SceneManager.sceneLoaded += OnLevelFinishedLoading;
        }

        private void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == null)
                return;

            //Other scenes include MainScene and Customization
            if (scene.name.Equals("MainMenu") && SIncLibUI.btn != null)
            {
                Destroy(SIncLibUI.btn.gameObject);
            }
            else if (scene.name.Equals("MainScene") && isActiveAndEnabled)
            {
                SIncLibUI.SpawnButton();
            }
        }

        public override void OnDeactivate()
        {
            SIncLibMod.ModActive = false;
            if (!SIncLibMod.ModActive && GameSettings.Instance != null && HUD.Instance != null)
            {
                HUD.Instance.AddPopupMessage("SIncLibUI has been deactivated!", "Cogs", PopupManager.PopUpAction.None,
                                             0, PopupManager.NotificationSound.Neutral, Color.black, 0f,
                                             PopupManager.PopupIDs.None);
            }
        }

        public override void OnActivate()
        {
            SIncLibMod.ModActive = true;
            if (SIncLibMod.ModActive && GameSettings.Instance != null && HUD.Instance != null)
            {
                HUD.Instance.AddPopupMessage("SIncLibUI has been activated!", "Cogs", PopupManager.PopUpAction.None,
                                             0, PopupManager.NotificationSound.Neutral, Color.black, 0f,
                                             PopupManager.PopupIDs.None);
            }
        }

        private int EmployeeIndex(Employee.EmployeeRole role)
        {
            return (int) role - 1;
        }

        public bool TransferBestAvailableStaff(Team team, Team[] sourceTeams, out string error)
        {
            IEnumerable<SoftwareWorkItem> software =
                team.WorkItems.Where(wi => wi is SoftwareWorkItem).Cast<SoftwareWorkItem>();
            if (!software.Any())
            {
                error = "Team has no projects";
                return false;
            }

            // Setup HR so that the correct number of employees are hired, should anyone leave
            if (AdjustDepartment != 0)
            {
                int MaxArt    = 0;
                int MaxCode   = 0;
                int MaxDesign = 0;
                foreach (SoftwareWorkItem workItem in software)
                {
                    Console.Log("Category 2: " + workItem._category);
                    SoftwareProduct softwareProduct = !workItem.SequelTo.HasValue
                        ? (SoftwareProduct) null
                        : GameSettings.Instance.simulation.GetProduct(workItem.SequelTo.Value, false);
                    SoftwareProduct sequelTo = softwareProduct == null || !softwareProduct.Traded
                        ? softwareProduct
                        : (SoftwareProduct) null;

                    float devTime = workItem.Type.DevTime(workItem.Features, workItem._category, sequelTo);
                    if (workItem.Type.OSSpecific)
                    {
                        devTime += Mathf.Max(workItem.OSs.Length - 1, 0);
                    }

                    int[] employeeRatio = workItem.Type.GetOptimalEmployeeCount(devTime, workItem.CodeArtRatio);

                    if ((AdjustDepartment & AdjustHRFlags.Art) != 0)
                        MaxArt = Mathf.Max(MaxArt, Mathf.CeilToInt(employeeRatio[1] * (1f - workItem.CodeArtRatio)));
                    if ((AdjustDepartment & AdjustHRFlags.Code) != 0)
                        MaxCode = Mathf.Max(MaxCode, Mathf.CeilToInt(employeeRatio[1] * workItem.CodeArtRatio));
                    if ((AdjustDepartment & AdjustHRFlags.Design) != 0)
                        MaxDesign = Mathf.Max(MaxDesign, employeeRatio[0]);
                }

                if (AdjustHR)
                {
                    if ((AdjustDepartment & AdjustHRFlags.Art) != 0)
                        team.HR.MaxEmployees[EmployeeIndex(Employee.EmployeeRole.Artist)] = MaxArt;
                    if ((AdjustDepartment & AdjustHRFlags.Code) != 0)
                        team.HR.MaxEmployees[EmployeeIndex(Employee.EmployeeRole.Programmer)] = MaxCode;
                    if ((AdjustDepartment & AdjustHRFlags.Design) != 0)
                        team.HR.MaxEmployees[EmployeeIndex(Employee.EmployeeRole.Designer)] = MaxDesign;
                }

                Console.Log(string.Format("Total staff required: C: {0} D: {1} A: {2}",
                                          team.HR.MaxEmployees[EmployeeIndex(Employee.EmployeeRole.Programmer)],
                                          team.HR.MaxEmployees[EmployeeIndex(Employee.EmployeeRole.Designer)],
                                          team.HR.MaxEmployees[EmployeeIndex(Employee.EmployeeRole.Artist)]));
            }

            // Switch team members around so that the team has the correct number of staff with the correct skills
            Dictionary<string, float> specializationMonths = null;
            foreach (SoftwareWorkItem workItem in software)
            {
                SoftwareProduct softwareProduct = !workItem.SequelTo.HasValue
                    ? (SoftwareProduct) null
                    : GameSettings.Instance.simulation.GetProduct(workItem.SequelTo.Value, false);
                SoftwareProduct sequelTo = softwareProduct == null || !softwareProduct.Traded
                    ? softwareProduct
                    : (SoftwareProduct) null;


                var months = workItem.Type.GetSpecializationMonths(workItem.Features, workItem.Category(),
                                                                   workItem.OSs, sequelTo);
                if (specializationMonths == null)
                {
                    specializationMonths = months;
                }
                else
                {
                    // Merge dictionaries
                    foreach (KeyValuePair<string, float> valuePair in months)
                    {
                        if (specializationMonths.ContainsKey(valuePair.Key))
                        {
                            specializationMonths[valuePair.Key] =
                                Mathf.Max(specializationMonths[valuePair.Key], valuePair.Value);
                        }
                        else
                        {
                            specializationMonths.Add(valuePair.Key, valuePair.Value);
                        }
                    }
                }
            }

            float totalMonths = specializationMonths.Sum(s => s.Value);

            List<Actor> chosenEmployees = new List<Actor>();
            IEnumerable<Actor> actors =
                GameSettings.Instance.sActorManager.Actors.Where(a => (!IdleOnly || a.IsIdle) &&
                                                                      (sourceTeams.Any(t => t == a.GetTeam()) ||
                                                                       a.GetTeam() == team || a.GetTeam() == null));

            Console.Log("Available employees : " + actors.Count());
            foreach (KeyValuePair<string, float> specialization in specializationMonths)
            {
                int numCodersRequired =
                    Mathf.CeilToInt((specialization.Value / totalMonths) *
                                    team.HR.MaxEmployees[EmployeeIndex(Employee.EmployeeRole.Programmer)]);
                int numDesignersRequired =
                    Mathf.CeilToInt((specialization.Value / totalMonths) *
                                    team.HR.MaxEmployees[EmployeeIndex(Employee.EmployeeRole.Designer)]);
                int numArtistsRequired =
                    Mathf.CeilToInt((specialization.Value / totalMonths) *
                                    team.HR.MaxEmployees[EmployeeIndex(Employee.EmployeeRole.Artist)]);

                Console.Log(
                            string.Format("Num {0} staff required: C: {1} D: {2} A: {3}", specialization.Key,
                                          numCodersRequired, numDesignersRequired, numArtistsRequired));

                // Coders
                if ((AdjustDepartment & AdjustHRFlags.Code) != 0)
                {
                    chosenEmployees.AddRange(actors
                                             .OrderByDescending(a =>
                                                                    a.employee != null
                                                                        ? a.employee
                                                                           .GetSpecialization(Employee.EmployeeRole.Programmer,
                                                                                              specialization.Key, true)
                                                                        : (float?) null)
                                             .Take(numCodersRequired));
                }

                // Designers
                if ((AdjustDepartment & AdjustHRFlags.Design) != 0)
                {
                    chosenEmployees.AddRange(actors
                                             .OrderByDescending(a =>
                                                                    a.employee != null
                                                                        ? a.employee
                                                                           .GetSpecialization(Employee.EmployeeRole.Designer,
                                                                                              specialization.Key, true)
                                                                        : (float?) null)
                                             .Take(numDesignersRequired));
                }

                // Artists
                if ((AdjustDepartment & AdjustHRFlags.Art) != 0)
                {
                    chosenEmployees.AddRange(actors
                                             .OrderByDescending(a =>
                                                                    a.employee != null
                                                                        ? a.employee
                                                                           .GetSpecialization(Employee.EmployeeRole.Artist,
                                                                                              specialization.Key, true)
                                                                        : (float?) null)
                                             .Take(numArtistsRequired));
                }
            }

            chosenEmployees = chosenEmployees.Distinct().ToList();

            // Remove old team members
            foreach (Actor actor in GameSettings.Instance.sActorManager.Actors.Where(a => a.GetTeam() == team))
            {
                // Don't kick out the lead
                if (actor.employee.IsRole(Employee.RoleBit.Lead))
                {
                    continue;
                }

                if ((AdjustDepartment & AdjustHRFlags.Art) == 0 && actor.employee.IsRole(Employee.RoleBit.Artist))
                {
                    continue;
                }

                if ((AdjustDepartment & AdjustHRFlags.Code) == 0 && actor.employee.IsRole(Employee.RoleBit.Programmer))
                {
                    continue;
                }

                if ((AdjustDepartment & AdjustHRFlags.Design) == 0 && actor.employee.IsRole(Employee.RoleBit.Designer))
                {
                    continue;
                }

                actor.Team = null;
            }

            // Make sure team has enough staff
            var unattachedStaff = GameSettings
                                  .Instance.sActorManager.Actors.Where(a => a.GetTeam() == null);

            if (chosenEmployees.Count(a => a.employee.IsRole(Employee.EmployeeRole.Programmer)) <
                team.HR.MaxEmployees[EmployeeIndex(Employee.EmployeeRole.Programmer)])
            {
                chosenEmployees.AddRange(unattachedStaff.OrderByDescending(a => a.employee != null
                                                                               ? a.employee.GetSkill(Employee
                                                                                                     .EmployeeRole
                                                                                                     .Programmer)
                                                                               : (float?) null)
                                                        .Take(team.HR.MaxEmployees
                                                                  [EmployeeIndex(Employee.EmployeeRole.Programmer)]));
            }

            if (chosenEmployees.Count(a => a.employee.IsRole(Employee.EmployeeRole.Designer)) <
                team.HR.MaxEmployees[EmployeeIndex(Employee.EmployeeRole.Designer)])
            {
                chosenEmployees.AddRange(unattachedStaff.OrderByDescending(a =>
                                                                               a.employee != null
                                                                                   ? a.employee.GetSkill(Employee
                                                                                                         .EmployeeRole
                                                                                                         .Designer)
                                                                                   : (float?) null)
                                                        .Take(team.HR.MaxEmployees
                                                                  [EmployeeIndex(Employee.EmployeeRole.Designer)]));
            }

            if (chosenEmployees.Count(a => a.employee.IsRole(Employee.EmployeeRole.Artist)) <
                team.HR.MaxEmployees[EmployeeIndex(Employee.EmployeeRole.Artist)])
            {
                chosenEmployees.AddRange(unattachedStaff.OrderByDescending(a => a.employee != null
                                                                               ? a.employee.GetSkill(Employee
                                                                                                     .EmployeeRole
                                                                                                     .Artist)
                                                                               : (float?) null)
                                                        .Take(team.HR.MaxEmployees
                                                                  [EmployeeIndex(Employee.EmployeeRole.Artist)]));
            }
            
            chosenEmployees = chosenEmployees.Distinct().ToList();
            
            // Find how many staff we have stolen from each team
            var teamCounts = chosenEmployees.Where(e=>e.GetTeam() != team).GroupBy(e=>e.Team)
                                            .Select(group=>new
                                                           {
                                                               Key   = group.Key,
                                                               Count = group.Count()
                                                           });

            // Add new team members
            foreach (Actor employee in chosenEmployees)
            {
                employee.Team = team.Name;
            }
            
            // Which staff are still unattached
            foreach (var teamCount in teamCounts)
            {
                unattachedStaff = unattachedStaff.Where(e=>e.GetTeam()==null);
                Console.Log(string.Format("After reassignment, {0} staff are unassigned.", unattachedStaff.Count()));
                
                Console.Log(string.Format("Team {0} missing {1} staff.", teamCount.Key, teamCount.Count));
                IEnumerable<Actor> takenStaff = unattachedStaff.Take(Mathf.Min(unattachedStaff.Count(), teamCount.Count));
                foreach (Actor employee in takenStaff)
                {
                    employee.Team = teamCount.Key;
                }
            }
            
            unattachedStaff = unattachedStaff.Where(e=>e.GetTeam()==null);
            Console.Log(string.Format("After reassignment, {0} staff are unassigned.", unattachedStaff.Count()));
            if ( unattachedStaff.Any() )
            {
                HUD.Instance.AddPopupMessage(unattachedStaff.Count() + " staff are without a team following reassignemnt", "Cogs", PopupManager.PopUpAction.None,
                                             0, PopupManager.NotificationSound.Issue, Color.black, 0f,
                                             PopupManager.PopupIDs.None);
            }

            error = null;
            return true;
        }
    }
}