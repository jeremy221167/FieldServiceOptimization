using ML.Services.Models;

namespace BlazorDemo.Services
{
    public interface IDemoDataService
    {
        List<Technician> GetSampleTechnicians();
        List<string> GetServiceTypes();
        List<string> GetEquipmentTypes();
        List<string> GetPriorityLevels();
        Dictionary<string, List<string>> GetSkillsData();
        JobRequest CreateSampleJob();
        JobRequest CreateEmergencyJob();
        List<JobRequest> GetActiveJobs();
    }

    public class DemoDataService : IDemoDataService
    {
        private readonly List<Technician> _sampleTechnicians;
        private readonly List<string> _serviceTypes;
        private readonly List<string> _equipmentTypes;
        private readonly List<string> _priorities;
        private readonly Dictionary<string, List<string>> _skillsData;

        public DemoDataService()
        {
            _serviceTypes = new List<string>
            {
                "HVAC Repair", "Plumbing", "Electrical", "Appliance Repair",
                "Maintenance", "Installation", "Emergency Service"
            };

            _equipmentTypes = new List<string>
            {
                "Air Conditioning Unit", "Heating System", "Water Heater",
                "Dishwasher", "Washing Machine", "Refrigerator", "Electrical Panel"
            };

            _priorities = new List<string> { "Low", "Normal", "High", "Urgent" };

            _skillsData = new Dictionary<string, List<string>>
            {
                ["HVAC"] = new() { "1", "2", "3", "4", "5" },
                ["Electrical"] = new() { "1", "2", "3", "4", "5" },
                ["Plumbing"] = new() { "1", "2", "3", "4", "5" },
                ["Appliance"] = new() { "1", "2", "3", "4", "5" },
                ["Carpentry"] = new() { "1", "2", "3", "4", "5" }
            };

            _sampleTechnicians = InitializeSampleTechnicians();
        }

        public List<Technician> GetSampleTechnicians() => _sampleTechnicians.ToList();
        public List<string> GetServiceTypes() => _serviceTypes.ToList();
        public List<string> GetEquipmentTypes() => _equipmentTypes.ToList();
        public List<string> GetPriorityLevels() => _priorities.ToList();
        public Dictionary<string, List<string>> GetSkillsData() => _skillsData.ToDictionary(x => x.Key, x => x.Value.ToList());

        public JobRequest CreateSampleJob()
        {
            var random = new Random();
            var serviceType = _serviceTypes[random.Next(_serviceTypes.Count)];
            var equipment = _equipmentTypes[random.Next(_equipmentTypes.Count)];
            var priority = _priorities[random.Next(_priorities.Count)];

            var locations = new[]
            {
                ("Downtown Office", 40.7128, -74.0060),
                ("Uptown Apartment", 40.7831, -73.9712),
                ("Midtown Store", 40.7549, -73.9840),
                ("Brooklyn Office", 40.6782, -73.9442),
                ("Queens Warehouse", 40.7282, -73.7949)
            };

            var location = locations[random.Next(locations.Length)];

            return new JobRequest
            {
                JobId = $"JOB{DateTime.Now:yyyyMMddHHmmss}",
                ServiceType = serviceType,
                Equipment = equipment,
                Location = location.Item1,
                Latitude = location.Item2,
                Longitude = location.Item3,
                ScheduledDate = DateTime.Now.AddHours(random.Next(1, 48)),
                SlaHours = random.Next(4, 24),
                Priority = priority,
                RequiredSkills = GetRandomSkills()
            };
        }

        private Dictionary<string, string> GetRandomSkills()
        {
            var random = new Random();
            var skills = new Dictionary<string, string>();
            var skillTypes = _skillsData.Keys.Take(random.Next(1, 4));

            foreach (var skill in skillTypes)
            {
                var level = random.Next(1, 6).ToString();
                skills[skill] = level;
            }

            return skills;
        }

        private List<Technician> InitializeSampleTechnicians()
        {
            var random = new Random();
            var names = new[]
            {
                "Alice Smith", "Bob Johnson", "Carol Davis", "David Brown",
                "Emma Wilson", "Frank Miller", "Grace Lee", "Henry Taylor",
                "Isabella Garcia", "Jack Martinez", "Kate Anderson", "Liam Thomas"
            };

            var locations = new[]
            {
                (40.7580, -73.9855), (40.6892, -74.0445), (40.7282, -73.7949),
                (40.7831, -73.9712), (40.6782, -73.9442), (40.7549, -73.9840),
                (40.7128, -74.0060), (40.7369, -73.9901), (40.7505, -73.9934),
                (40.7614, -73.9776), (40.7489, -73.9680), (40.7061, -74.0087)
            };

            return names.Select((name, index) =>
            {
                var location = locations[index % locations.Length];
                var skills = new Dictionary<string, int>();

                foreach (var skillType in _skillsData.Keys.Take(random.Next(2, 5)))
                {
                    skills[skillType] = random.Next(1, 6);
                }

                var isAvailable = random.NextDouble() > 0.3;
                var status = CreateSampleStatus(random, isAvailable);

                return new Technician
                {
                    TechnicianId = $"TECH{(index + 1):D3}",
                    Name = name,
                    Latitude = location.Item1,
                    Longitude = location.Item2,
                    Skills = skills,
                    IsAvailable = isAvailable,
                    CurrentWorkload = random.Next(0, 5),
                    HistoricalSlaSuccessRate = 0.7 + (random.NextDouble() * 0.3),
                    AvailableFrom = DateTime.Now.AddHours(-random.Next(0, 12)),
                    AvailableUntil = DateTime.Now.AddHours(random.Next(8, 24)),
                    CoverageArea = CreateSampleCoverageArea(index, random),
                    CurrentStatus = status,
                    CurrentJobId = status.Status != "Available" ? $"JOB{DateTime.Now:yyyyMMdd}{random.Next(100, 999)}" : string.Empty,
                    CanBeInterrupted = random.NextDouble() > 0.3,
                    Phone = $"+1555{random.Next(100, 999)}{random.Next(1000, 9999)}"
                };
            }).ToList();
        }

        private GeographicCoverage CreateSampleCoverageArea(int index, Random random)
        {
            var cities = new[]
            {
                "Manhattan", "Brooklyn", "Queens", "Bronx", "Staten Island",
                "Jersey City", "Newark", "Hoboken", "Long Island City"
            };

            var postalCodes = new[]
            {
                "10001", "10002", "10003", "10004", "10005", "10006", "10007",
                "11201", "11202", "11203", "11204", "11205", "11206"
            };

            var primaryCities = cities.OrderBy(x => random.Next()).Take(random.Next(1, 3)).ToList();
            var secondaryCities = cities.Except(primaryCities).OrderBy(x => random.Next()).Take(random.Next(1, 3)).ToList();
            var coveragePostalCodes = postalCodes.OrderBy(x => random.Next()).Take(random.Next(3, 8)).ToList();

            var preferredRegions = new List<GeographicRegion>();

            if (random.NextDouble() > 0.3)
            {
                preferredRegions.Add(new GeographicRegion
                {
                    RegionName = $"Metro Area {index + 1}",
                    RegionType = "Custom",
                    CenterLatitude = 40.7128 + (random.NextDouble() - 0.5) * 0.2,
                    CenterLongitude = -74.0060 + (random.NextDouble() - 0.5) * 0.3,
                    RadiusKm = 25 + random.Next(-10, 15),
                    Priority = random.Next(1, 4)
                });
            }

            return new GeographicCoverage
            {
                ServiceRadiusKm = 30 + random.Next(-10, 40),
                PrimaryCities = primaryCities,
                SecondaryCities = secondaryCities,
                PostalCodes = coveragePostalCodes,
                PreferredRegions = preferredRegions,
                IsWillingToTravel = random.NextDouble() > 0.1,
                MaxTravelDistanceKm = 80 + random.Next(-30, 120)
            };
        }

        private TechnicianStatus CreateSampleStatus(Random random, bool isAvailable)
        {
            var statuses = new[] { "Available", "EnRoute", "OnSite", "Busy" };
            var status = isAvailable ? "Available" : statuses[random.Next(1, statuses.Length)];

            var baseLocation = (40.7128 + (random.NextDouble() - 0.5) * 0.2,
                              -74.0060 + (random.NextDouble() - 0.5) * 0.3);

            return new TechnicianStatus
            {
                Status = status,
                CurrentLatitude = status != "Available" ? baseLocation.Item1 : null,
                CurrentLongitude = status != "Available" ? baseLocation.Item2 : null,
                LastLocationUpdate = DateTime.UtcNow.AddMinutes(-random.Next(0, 30)),
                EstimatedArrivalMinutes = status == "EnRoute" ? random.Next(5, 45) : 0,
                CurrentJobLocation = status != "Available" ? "Sample Location" : string.Empty,
                IsTracking = status == "EnRoute"
            };
        }

        public JobRequest CreateEmergencyJob()
        {
            var job = CreateSampleJob();
            job.IsEmergency = true;
            job.Priority = "Urgent";
            job.CustomerName = "Emergency Customer";
            job.CustomerPhone = "+15551234567";
            job.ServiceType = "Emergency Repair";
            job.SlaHours = 1;
            job.JobId = $"EMERGENCY{DateTime.Now:yyyyMMddHHmmss}";
            return job;
        }

        public List<JobRequest> GetActiveJobs()
        {
            var jobs = new List<JobRequest>();
            var random = new Random(42); // Fixed seed for consistent results

            var jobLocations = new[]
            {
                ("Midtown Plaza", 40.7549, -73.9840, "HVAC Repair", "High"),
                ("Brooklyn Heights", 40.6962, -73.9969, "Electrical", "Normal"),
                ("Queens Mall", 40.7282, -73.7949, "Plumbing", "Urgent"),
                ("Upper West Side", 40.7831, -73.9712, "Appliance Repair", "Normal"),
                ("Financial District", 40.7074, -74.0113, "Emergency Service", "Urgent"),
                ("Chelsea Office", 40.7505, -73.9934, "Installation", "High"),
                ("SoHo Store", 40.7230, -74.0030, "Maintenance", "Low"),
                ("Williamsburg", 40.7081, -73.9571, "HVAC Repair", "Normal"),
                ("Long Island City", 40.7505, -73.9369, "Electrical", "High")
            };

            for (int i = 0; i < jobLocations.Length; i++)
            {
                var location = jobLocations[i];
                var job = new JobRequest
                {
                    JobId = $"JOB{DateTime.Now:yyyyMMdd}{(i + 1):D3}",
                    ServiceType = location.Item4,
                    Equipment = _equipmentTypes[random.Next(_equipmentTypes.Count)],
                    Location = location.Item1,
                    Latitude = location.Item2,
                    Longitude = location.Item3,
                    Priority = location.Item5,
                    ScheduledDate = DateTime.Now.AddHours(random.Next(-2, 8)),
                    SlaHours = location.Item5 == "Urgent" ? 2 : location.Item5 == "High" ? 4 : 8,
                    RequiredSkills = GetRandomSkills(),
                    CustomerName = $"Customer {i + 1}",
                    CustomerPhone = $"+1555{random.Next(100, 999)}{random.Next(1000, 9999)}",
                    IsEmergency = location.Item5 == "Urgent"
                };

                jobs.Add(job);
            }

            return jobs;
        }
    }
}