using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplexSolver.Models
{
    public class Simplex
    {
        public decimal[] objectiveVector { get; set; }
        public decimal[] Matriz { get; set; }
        [Display(Name = "Variaveis")]
        public int? Variaveis { get; set; }
        [Display(Name = "Restricoes")]
        public int? Restricoes { get; set; }

        public bool Minimizar { get; set; }

        public bool ExibirPassoAPasso { get; set; }
    }
}
