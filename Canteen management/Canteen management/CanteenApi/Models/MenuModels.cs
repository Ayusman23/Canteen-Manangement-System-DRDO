using System.Text.Json.Serialization;

namespace CanteenApi.Models
{
    public class MenuDay
    {
        public string Day { get; set; }
        public Meal Normal { get; set; }
        public Meal Special { get; set; }
        public Item Item { get; set; }
    }

    public class Meal
    {
        [JsonPropertyName("meal")]
        public string MealName { get; set; }
        public int Price { get; set; }
    }

    public class Item
    {
        public string Name { get; set; }
        public int Price { get; set; }
    }
}