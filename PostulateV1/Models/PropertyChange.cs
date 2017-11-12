namespace Postulate.Orm.Models
{
    public class PropertyChange
    {
        public string PropertyName { get; set; }
        public object OldValue { get; set; }
        public object NewValue { get; set; }

        public bool IsChanged()
        {
            if (OldValue == null && NewValue == null) return false;
            if (OldValue == null ^ NewValue == null) return true;
            return !OldValue.Equals(NewValue);
        }
    }
}