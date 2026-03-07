using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages the 9 major allergens used in the Phase 3 allergen challenge.
/// Provides display names, descriptions, and random selection utilities.
/// </summary>
public static class AllergenManager
{
    // The 9 major allergens (reuses AllergenProductData.AllergenType)
    public static readonly AllergenProductData.AllergenType[] MajorAllergens =
    {
        AllergenProductData.AllergenType.Milk,
        AllergenProductData.AllergenType.Eggs,
        AllergenProductData.AllergenType.Fish,
        AllergenProductData.AllergenType.Shellfish,
        AllergenProductData.AllergenType.TreeNuts,
        AllergenProductData.AllergenType.Peanuts,
        AllergenProductData.AllergenType.Wheat,
        AllergenProductData.AllergenType.Soy,
        AllergenProductData.AllergenType.Sesame
    };

    private static readonly Dictionary<AllergenProductData.AllergenType, string> AllergenDisplayNames =
        new Dictionary<AllergenProductData.AllergenType, string>
        {
            { AllergenProductData.AllergenType.Milk,      "Milk"      },
            { AllergenProductData.AllergenType.Eggs,      "Eggs"      },
            { AllergenProductData.AllergenType.Fish,      "Fish"      },
            { AllergenProductData.AllergenType.Shellfish, "Shellfish" },
            { AllergenProductData.AllergenType.TreeNuts,  "Tree Nuts" },
            { AllergenProductData.AllergenType.Peanuts,   "Peanuts"   },
            { AllergenProductData.AllergenType.Wheat,     "Wheat"     },
            { AllergenProductData.AllergenType.Soy,       "Soy"       },
            { AllergenProductData.AllergenType.Sesame,    "Sesame"    }
        };

    private static readonly Dictionary<AllergenProductData.AllergenType, string> AllergenDescriptions =
        new Dictionary<AllergenProductData.AllergenType, string>
        {
            { AllergenProductData.AllergenType.Milk,      "Found in dairy products like cheese, butter, and yogurt."  },
            { AllergenProductData.AllergenType.Eggs,      "Found in baked goods, mayonnaise, and pasta."              },
            { AllergenProductData.AllergenType.Fish,      "Found in tuna, salmon, cod, and fish sauce."               },
            { AllergenProductData.AllergenType.Shellfish, "Found in shrimp, crab, lobster, and oysters."              },
            { AllergenProductData.AllergenType.TreeNuts,  "Found in almonds, walnuts, cashews, and pistachios."       },
            { AllergenProductData.AllergenType.Peanuts,   "Found in peanut butter, mixed nuts, and some sauces."      },
            { AllergenProductData.AllergenType.Wheat,     "Found in bread, pasta, cereals, and baked goods."          },
            { AllergenProductData.AllergenType.Soy,       "Found in tofu, soy sauce, edamame, and miso."              },
            { AllergenProductData.AllergenType.Sesame,    "Found in tahini, hummus, sesame oil, and bread toppings."  }
        };

    /// <summary>Returns a randomly selected allergen from the 9 major allergens.</summary>
    public static AllergenProductData.AllergenType GetRandomAllergen()
    {
        return MajorAllergens[Random.Range(0, MajorAllergens.Length)];
    }

    /// <summary>Returns the human-readable display name for an allergen.</summary>
    public static string GetDisplayName(AllergenProductData.AllergenType allergen)
    {
        return AllergenDisplayNames.TryGetValue(allergen, out string name) ? name : allergen.ToString();
    }

    /// <summary>Returns an educational description for an allergen.</summary>
    public static string GetDescription(AllergenProductData.AllergenType allergen)
    {
        return AllergenDescriptions.TryGetValue(allergen, out string desc) ? desc : string.Empty;
    }

    /// <summary>
    /// Returns a list of <paramref name="count"/> safe allergens that are different from
    /// the given <paramref name="dangerous"/> allergen. The result order is randomised.
    /// </summary>
    public static List<AllergenProductData.AllergenType> GetSafeAllergens(
        AllergenProductData.AllergenType dangerous, int count = 2)
    {
        List<AllergenProductData.AllergenType> pool = new List<AllergenProductData.AllergenType>(MajorAllergens);
        pool.Remove(dangerous);

        // Fisher-Yates shuffle
        for (int i = 0; i < pool.Count; i++)
        {
            int r = Random.Range(i, pool.Count);
            AllergenProductData.AllergenType temp = pool[i];
            pool[i] = pool[r];
            pool[r] = temp;
        }

        return pool.GetRange(0, Mathf.Min(count, pool.Count));
    }
}

