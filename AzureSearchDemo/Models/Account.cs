using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureSearchDemo.Models
{
    [SerializePropertyNamesAsCamelCase]
    public class Account
    {
        [System.ComponentModel.DataAnnotations.Key]
        [IsFilterable]
        public string Account_Number { get; set; }

        [IsFilterable, IsSortable, IsFacetable]
        public double? Balance { get; set; }
        [IsSearchable, IsFilterable, IsSortable]
        public string FirstName { get; set; }
        [IsSearchable, IsFilterable, IsSortable]
        public string LastName { get; set; }
        [IsFacetable, IsFilterable, IsSortable]
        public int? Age { get; set; }
        [IsSearchable, IsFilterable]
        public string Address { get; set; }
        [IsFilterable, IsFacetable, IsSortable]
        public string Employer { get; set; }
        [IsFilterable]
        public string Email { get; set; }
        [IsSortable, IsSearchable, IsFilterable]
        public string City { get; set; }
        [IsSortable, IsSearchable, IsFilterable]
        public string State { get; set; }
    }
}
