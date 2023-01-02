namespace OculusGraphQLApiLib.Results
{
    public class TargetObject<T>
    {
        public string id { get; set; } = "";
        public string trace_id { get; set; } = "";
        public T target_object { get; set; } = default;
    }
}