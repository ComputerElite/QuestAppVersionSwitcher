using System.Collections.Generic;

namespace OculusGraphQLApiLib.Results
{
    public class ContextualSearch
    {
        public AllCategoryResults contextual_search { get; set; } = new AllCategoryResults();
    }

    public class AllCategoryResults
    {
        public List<CategorySearchResult> all_category_results { get; set; } = new List<CategorySearchResult>();
    }

    public class CategorySearchResult
    {
        public string name { get; set; } = "";
        public Nodes<TargetObject<EdgesPrimaryBinaryApplication>> search_results { get; set; } = new Nodes<TargetObject<EdgesPrimaryBinaryApplication>>();
        public string display_name { get; set; } = "";
    }
}