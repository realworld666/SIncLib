using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Console = DevConsole.Console;

namespace SIncLib
{
    internal class PortingBehaviour : ModBehaviour
    {
        public static PortingBehaviour Instance;

        public List<PortingJob> PortingJobQueue = new List<PortingJob>();


        public HashSet<string> PortingTeams = new HashSet<string>();
        public SimulatedCompany OutsourcedPorting = null;
        public int SupportForMonths = 12;
        public int MinimumUserbase = 1000;
        public int ConcurrentJobs = 1;
        public bool Enabled = false;


        private void Awake()
        {
            Instance = this;

            // load from player prefs
            string teamsString = PlayerPrefs.GetString("PortingTeams", "");
            if (!string.IsNullOrEmpty(teamsString))
            {
                PortingTeams = new HashSet<string>(teamsString.Split(','));
            }
            var outsourcedPortingId = PlayerPrefs.GetInt("OutsourcedPorting", -1);
            if (outsourcedPortingId >= 0)
            {
                try
                {
                    var company = GameSettings.Instance.simulation.Companies.First(x => x.Key == outsourcedPortingId);
                    OutsourcedPorting = company.Value;
                }
                catch (Exception e)
                {
                    Console.LogError("Failed to load outsourced porting company " + outsourcedPortingId + " " + e.Message);
                }

            }
            SupportForMonths = PlayerPrefs.GetInt("SupportForMonths", 12);
            MinimumUserbase = PlayerPrefs.GetInt("MinimumUserbase", 1000);
            ConcurrentJobs = PlayerPrefs.GetInt("ConcurrentJobs", 1);
            Enabled = PlayerPrefs.GetInt("PortingEnabled", 0) == 1;
        }

        private void Start()
        {
            if (!SIncLibMod.ModActive || !isActiveAndEnabled)
            {
                return;
            }
            TimeOfDayOnOnDayPassed(null, null);
            TimeOfDay.OnDayPassed += TimeOfDayOnOnDayPassed;
        }

        public override void OnActivate()
        {
        }

        public override void OnDeactivate()
        {
        }

        public void TimeOfDayOnOnDayPassed(object sender, EventArgs e)
        {
            if (!Enabled)
            {
                return;
            }

            PortingJobQueue.Clear();

            // get all software in date range
            List<SoftwareProduct> products = GameSettings.Instance.MyCompany.Products;
            // get current date
            var currentDate = TimeOfDay.Instance.GetDate();
            // subtract the number of months from the current date
            var cutoffDate = currentDate - SupportForMonths;
            // filter list and remove anything that can't be ported
            products = products.Where(x => x.GetReleaseDate() > cutoffDate && x.Type.OSSpecific && !PublisherDeal.HasDeal(x, "OSExclusivity")).OrderByDescending(x => x.Userbase).ToList();

            // add existing porting jobs to the job queue
            foreach (Team team in PortingTeams.SelectNotNull(new Func<string, Team>(GameSettings.GetTeam)))
            {
                foreach (var workItem in team.WorkItems.OfType<SoftwarePort>())
                {
                    PortingJobQueue.Add(new PortingJob()
                    {
                        Product = workItem.Product,
                        TargetProduct = workItem.OSs.FirstOrDefault().Product,
                        Team = team,
                        WorkItem = workItem,
                        IsPaused = false
                    });
                }
            }

            foreach (var product in products)
            {
                var supportedOSs = FilterAvailableTargets(product).OrderByDescending(p => p.Userbase);

                foreach (var os in supportedOSs)
                {
                    // if not already in the queue with same product and same target
                    if (PortingJobQueue.Any(x => x.Product == product && x.TargetProduct == os))
                    {
                        continue;
                    }
                    PortingJobQueue.Add(new PortingJob()
                    {
                        Product = product,
                        TargetProduct = os,
                        Team = null,
                        WorkItem = null,
                        IsPaused = true
                    });
                }
            }


        }

        private List<SoftwareProduct> FilterAvailableTargets(SoftwareProduct p)
        {
            List<SoftwareProduct> allProducts = GameSettings.Instance.simulation.GetAllProducts(true).ToList();

            var companyFilter = p.Publishing != null ? p.Publishing.Publisher.ID : 0U;
            var catFilter = p.Type.HasOSLimits() ? p.Type.GetOSLimits().ToHashSet() : null;
            var ignoreCat = catFilter == null;
            //Console.Log("Filtering for " + p.Name + " company " + companyFilter + ((!ignoreCat) ? " categories " + catFilter.GetListAbbrev("Category") : ""));
            return allProducts.Where(targetProd =>
            {
                if (!targetProd.Type.Name.Equals("Operating System"))
                {
                    return false;
                }

                if (targetProd.Userbase < MinimumUserbase)
                {
                    //Console.Log("Filtering out " + targetProd.Name + " user base " + targetProd.Userbase);
                    return false;
                }

                if (companyFilter > 0U && (int)targetProd.DevCompany.ID != (int)companyFilter)
                {
                    //Console.Log("Filtering out " + targetProd.Name + " company " + targetProd.DevCompany.Name);
                    return false;
                }

                if (!ignoreCat && !catFilter.Contains(targetProd.Category.Name))
                {
                    // Console.Log("Filtering out " + targetProd.Name + " category " + targetProd.Category.Name);
                    return false;
                }
                if (p.Features != null && !SoftwareType.OSDependenciesMet(targetProd, p.Features))
                {
                    //Console.Log(p.Name + " requires " + p.Features.GetListAbbrev("Feature") + " but " + targetProd.Name + " does not have them");
                    return false;
                }

                if (p.OSs.Contains(targetProd) || !SoftwareType.OSDependenciesMet(targetProd, p.Features))
                {
                    //Console.Log("2 Filtering out " + targetProd.Name);
                    return false;
                }
                SoftwarePort softwarePort = GameSettings.Instance.MyCompany.WorkItems.OfType<SoftwarePort>().FirstOrDefault((z => z.Product == p));

                if (softwarePort != null && softwarePort.OSs.Any((z => z.Product == targetProd || z.Product.MockSucceeded == targetProd)))
                {
                    //Console.Log("3 Filtering out " + targetProd.Name);
                    return false;
                }

                //Console.Log("Keeping " + targetProd.Name);
                return true;
            }).ToList();
        }

        public void Update()
        {
            if (!SIncLibMod.ModActive || !isActiveAndEnabled)
            {
                return;
            }

            // check if any jobs are complete
            foreach (var job in PortingJobQueue.Where(x => x.WorkItem != null && x.WorkItem.GetActualProgress() >= 1))
            {
                Console.Log("Job " + job.Product.Name + " finished");
                // remove the job from the queue
                PortingJobQueue.Remove(job);
            }

            // check if any of the porting teams have less than ConcurrentJobs porting jobs running
            foreach (var team in PortingTeams.SelectNotNull(new Func<string, Team>(GameSettings.GetTeam)))
            {
                if (team.WorkItems.OfType<SoftwarePort>().Count() < ConcurrentJobs)
                {
                    // get the first job in the queue and start a porting job
                    var nextJob = PortingJobQueue.FirstOrDefault(job => job.WorkItem == null);
                    if (nextJob != null)
                    {
                        Console.Log("Starting next job " + nextJob.Product.Name);
                        StartPortingJob(team, nextJob);
                    }
                }
            }
        }

        private void StartPortingJob(Team team, PortingJob nextJob)
        {
            // create a new porting job
            var portingJob = new SoftwarePort(nextJob.Product, new SoftwareProduct[] { nextJob.TargetProduct });
            if (OutsourcedPorting != null)
            {
                portingJob.CompanyWorker = OutsourcedPorting;
            }
            else
            {
                // add the job to the team
                portingJob.AddDevTeam(team);
            }
            nextJob.IsPaused = false;
            nextJob.WorkItem = portingJob;
            nextJob.Team = team;

            return;

        }

    }
}
