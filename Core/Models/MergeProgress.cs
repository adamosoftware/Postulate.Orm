namespace Postulate.Orm.ModelMerge
{
    public class MergeProgress
    {
        public string Description { get; set; }
        public int PercentComplete { get; set; }

        public override string ToString()
        {
            return $"{Description} - {PercentComplete}% complete";
        }
    }
}