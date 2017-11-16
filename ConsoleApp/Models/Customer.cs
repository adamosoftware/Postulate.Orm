using Postulate.Orm.Abstract;
using Postulate.Orm.Attributes;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsoleApp.Models
{
    public class Customer : Record<int>
    {
        [MaxLength(50)]
        [Required]
        public string LastName { get; set; }

        [MaxLength(50)]
        [Required]
        public string FirstName { get; set; }

        [MaxLength(50)]
        [Column("Email")]
        public string VeryMuchEmail { get; set; }

        [MaxLength(50)]
        public string Phone { get; set; }

        public DateTime? EffectiveDate { get; set; }

        [DecimalPrecision(3, 2)]
        public decimal? DiscountRate { get; set; }

        public override string ToString()
        {
            return $"Last Nane = {LastName}, First Name= {FirstName}, Email = {VeryMuchEmail}, Effective Date = {EffectiveDate}, Discount Rate = {DiscountRate}";
        }
    }
}