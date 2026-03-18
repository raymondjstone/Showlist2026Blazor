using System;
using System.Collections.Generic;
using Showlist2026.Entities;

namespace Showlist2026.Models
{
    public class HomePageStats
    {

        public int shows=0;
        public int episodes=0;
        public int backlogpages=0;
        public int showsNeedingUpdate=0;
        public List<Show> recentshows = new List<Show>();

        public HomePageStats()
        {

        }



    }
}
