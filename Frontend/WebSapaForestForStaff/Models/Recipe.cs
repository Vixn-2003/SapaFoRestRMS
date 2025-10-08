using System;
using System.Collections.Generic;

namespace WebSapaForestForStaff.Models;

public partial class Recipe
{
    public int RecipeId { get; set; }

    public int MenuItemId { get; set; }

    public int IngredientId { get; set; }

    public decimal QuantityNeeded { get; set; }

    public virtual Ingredient Ingredient { get; set; } = null!;

    public virtual MenuItem MenuItem { get; set; } = null!;
}
