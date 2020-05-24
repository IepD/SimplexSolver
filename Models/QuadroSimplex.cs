using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace SimplexSolver.Models
{
    [Serializable]
    public class QuadroSimplex
    {
        public string[] Elementos;
        public decimal[,] Matriz;
        public decimal[] FuncaoObjetiva;

        public QuadroSimplex(string[] elementosQuadro, decimal[,] quadroNumerico, decimal[] funcaoObjetiva)
        {
            this.Elementos = elementosQuadro;
            this.Matriz = quadroNumerico;
            this.FuncaoObjetiva = funcaoObjetiva;
        }

        public QuadroSimplex()
        {
        }

        public QuadroSimplex Copy(string[] elementosQuadro, decimal[,] quadroNumerico, decimal[] funcaoObjetiva)
        {
            return new QuadroSimplex(elementosQuadro, quadroNumerico, funcaoObjetiva);
        }

        public QuadroSimplex Clone(QuadroSimplex simplex)
        {
            return simplex.DibriClone();
        }
    }
}
