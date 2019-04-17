using System.Collections.Generic;
using System.Linq;
using DevConsole;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SIncLib
{
    public class SIncLibBehaviour : ModBehaviour
    {
	    public static SIncLibBehaviour Instance;

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
            if ( scene == null )
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

        public bool TransferBestAvailableStaff(Team team)
		{
			IEnumerable<SoftwareWorkItem> software = team.WorkItems.Where(wi=>wi is SoftwareWorkItem).Cast<SoftwareWorkItem>();
			if ( !software.Any() )
				return false;
			
			software.First(s=>s.)
			
			Console.Log("Category 2: " + _activeDesign._category);
			float devTime = SelectedType.DevTime(_activeDesign.Features, _activeDesign._category, SequelTo);
			if (SelectedType.OSSpecific)
			{
				devTime += Mathf.Max(_activeDesign.OSs.Length - 1, 0);
			}

			int[] employeeRatio = SelectedType.GetOptimalEmployeeCount(devTime, _activeDesign.CodeArtRatio);

			// Setup HR so that the correct number of employees are hired, should anyone leave
			team.HR.MaxEmployees[EmployeeIndex(Employee.EmployeeRole.Artist)] =
				Mathf.CeilToInt(employeeRatio[1] * (1f - _activeDesign.CodeArtRatio));
			team.HR.MaxEmployees[EmployeeIndex(Employee.EmployeeRole.Programmer)] =
				Mathf.CeilToInt(employeeRatio[1] * _activeDesign.CodeArtRatio);
			team.HR.MaxEmployees[EmployeeIndex(Employee.EmployeeRole.Designer)] = employeeRatio[0];

			Console.Log(string.Format("Total staff required: P: {0} D: {1} A: {2}",
				team.HR.MaxEmployees[EmployeeIndex(Employee.EmployeeRole.Programmer)],
				team.HR.MaxEmployees[EmployeeIndex(Employee.EmployeeRole.Designer)],
				team.HR.MaxEmployees[EmployeeIndex(Employee.EmployeeRole.Artist)]));

			// Switch team members around so that the team has the correct number of staff with the correct skills
			Dictionary<string, float> specializationMonths =
				this.SelectedType.GetSpecializationMonths(_activeDesign.Features, _activeDesign.Category(), _activeDesign.OSs,
					null);
			float totalMonths = specializationMonths.Sum(s => s.Value);

			List<Actor> chosenEmployees = new List<Actor>();

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
					$"Num {specialization.Key} staff required: P: {numCodersRequired} D: {numDesignersRequired} A: {numArtistsRequired}");

				// Coders
				chosenEmployees.AddRange(GameSettings.Instance.sActorManager.Actors.Where(a => a.IsIdle || a.GetTeam() == team)
					.OrderByDescending(a => a.employee?.GetSpecialization(Employee.EmployeeRole.Programmer, specialization.Key, true))
					.Take(numCodersRequired));
				// Designers
				chosenEmployees.AddRange(GameSettings.Instance.sActorManager.Actors.Where(a => a.IsIdle || a.GetTeam() == team)
					.OrderByDescending(a => a.employee?.GetSpecialization(Employee.EmployeeRole.Designer, specialization.Key, true))
					.Take(numDesignersRequired));
				// Artists
				chosenEmployees.AddRange(GameSettings.Instance.sActorManager.Actors.Where(a => a.IsIdle || a.GetTeam() == team)
					.OrderByDescending(a => a.employee?.GetSpecialization(Employee.EmployeeRole.Artist, specialization.Key, true))
					.Take(numArtistsRequired));
			}

			chosenEmployees = chosenEmployees.Distinct().ToList();
			if (chosenEmployees.Count(a => a.employee.IsRole(Employee.EmployeeRole.Programmer)) <
				team.HR.MaxEmployees[EmployeeIndex(Employee.EmployeeRole.Programmer)])
			{
				chosenEmployees.AddRange(GameSettings.Instance.sActorManager.Actors.Where(a => a.IsIdle || a.GetTeam() == team)
					.OrderByDescending(a => a.employee?.GetSkill(Employee.EmployeeRole.Programmer))
					.Take(team.HR.MaxEmployees[EmployeeIndex(Employee.EmployeeRole.Programmer)]));
			}

			if (chosenEmployees.Count(a => a.employee.IsRole(Employee.EmployeeRole.Designer)) <
				team.HR.MaxEmployees[EmployeeIndex(Employee.EmployeeRole.Designer)])
			{
				chosenEmployees.AddRange(GameSettings.Instance.sActorManager.Actors.Where(a => a.IsIdle || a.GetTeam() == team)
					.OrderByDescending(a => a.employee?.GetSkill(Employee.EmployeeRole.Designer))
					.Take(team.HR.MaxEmployees[EmployeeIndex(Employee.EmployeeRole.Designer)]));
			}

			if (chosenEmployees.Count(a => a.employee.IsRole(Employee.EmployeeRole.Artist)) <
				team.HR.MaxEmployees[EmployeeIndex(Employee.EmployeeRole.Artist)])
			{
				chosenEmployees.AddRange(GameSettings.Instance.sActorManager.Actors.Where(a => a.IsIdle || a.GetTeam() == team)
					.OrderByDescending(a => a.employee?.GetSkill(Employee.EmployeeRole.Artist))
					.Take(team.HR.MaxEmployees[EmployeeIndex(Employee.EmployeeRole.Artist)]));
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

				actor.Team = null;
			}

			// Add new team members
			foreach (Actor employee in chosenEmployees)
			{
				employee.Team = team.Name;
			}
		}
    }
}