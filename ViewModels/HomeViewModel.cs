using System.Collections.Generic;
using PremierAuto.Models;

namespace PremierAuto.ViewModels
{
    public class HomeViewModel
    {
        public List<Service> Services { get; set; } = new List<Service>();
        public List<Mechanic> Mechanics { get; set; } = new List<Mechanic>();
    }
}