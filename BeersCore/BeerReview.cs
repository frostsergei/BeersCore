using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeersCore
{
    class BeerReview
    {
        public string brewery_id { get; set; }
        public string brewery_name { get; set; }
        public string review_time { get; set; }
        public string review_overall { get; set; }
        public string review_aroma { get; set; }
        public string review_appearance { get; set; }
        public string review_profilename { get; set; }
        public string beer_style { get; set; }
        public string review_palate { get; set; }
        public string review_taste { get; set; }
        public string beer_name { get; set; }
        public string beer_abv { get; set; }
        public string beer_beerid { get; set; }

        public bool valid()
        {
            if (beer_beerid == string.Empty) return false;
            return true;
        }
    }
}
