using PremierAuto.Models;
using System.Collections.Generic;

namespace PremierAuto.ViewModels
{
    public class MechanicProfileViewModel
    {
        public Mechanic Mechanic { get; set; }
        public List<Review> Reviews { get; set; } = new List<Review>();
    }
}